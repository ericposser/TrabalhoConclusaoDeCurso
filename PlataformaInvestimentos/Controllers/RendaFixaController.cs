using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers
{
    [Authorize]
    public class RendaFixaController : UtilController
    {
        private readonly Context _context;

        public RendaFixaController(Context context)
        {
            _context = context;
        }

        // GET: RendaFixa
        public async Task<IActionResult> Index()
        {
            var context = _context.RendaFixa.Include(r => r.Usuario);
            return View(await context.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RendaFixa rendaFixa, [FromServices] IBrapi brapi)
        {
            try
            {
                if (rendaFixa.DataVencimento < rendaFixa.DataCompra)
                {
                    ModelState.AddModelError("DataVencimento",
                        "A data de vencimento não pode ser menor que a data de compra.");
                    return View(rendaFixa);
                }

                var usuarioId = ObterUsuarioId();
                double taxaFinal;

                // Forma (pre/pos)
                var forma = Request.Form.TryGetValue("Forma", out var formaVals)
                    ? formaVals.ToString()
                    : "pre"; // default

                if (forma == "pre")
                {
                    // Pré: usa taxa digitada
                    var taxaStr = Request.Form["Taxa"].ToString().Replace("%", "").Replace(",", ".").Trim();
                    taxaFinal = double.TryParse(taxaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tx)
                        ? tx
                        : 0.0;
                }
                else
                {
                    // Pós: SELIC -> CDI = SELIC - 0,1; aplica percentual informado
                    var selic = await brapi.ObterSelicAtual();
                    if (selic is null)
                    {
                        ModelState.AddModelError("", "Erro ao obter a taxa SELIC.");
                        return View(rendaFixa);
                    }

                    var cdi = (double)(selic.Value - 0.1m);
                    var taxaStr = Request.Form["Taxa"].ToString().Replace("%", "").Replace(",", ".").Trim();
                    var percentual =
                        double.TryParse(taxaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct)
                            ? pct
                            : 100.0;

                    taxaFinal = (percentual / 100.0) * cdi;
                }

                // Toggle de liquidez diária
                var liquidezLigada = Request.Form.TryGetValue("ToggleLiquidez", out var liqVals)
                                     && (liqVals.ToString().Contains("on", StringComparison.OrdinalIgnoreCase)
                                         || liqVals.ToString().Equals("true", StringComparison.OrdinalIgnoreCase));

                if (liquidezLigada)
                {
                    // Esconde/ignora vencimento
                    rendaFixa.DataVencimento = null; // sua model é DateTime?
                }

                rendaFixa.UsuarioId = usuarioId;
                rendaFixa.Taxa = taxaFinal;

                _context.Add(rendaFixa);

                // Lançamento
                var lancamento = new Lancamento
                {
                    Movimentacao = "Compra",
                    Produto = "Renda Fixa - " + rendaFixa.Emissor,
                    Quantidade = 1,
                    ValorTotal = rendaFixa.Valor,
                    Data = rendaFixa.DataCompra,
                    UsuarioId = usuarioId
                };
                _context.Add(lancamento);

                await _context.SaveChangesAsync();
                TempData["Sucesso"] = true;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro ao adicionar a renda fixa. Tente novamente.");
                return View(rendaFixa);
            }
        }

        // GET: RendaFixa/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rendaFixa = await _context.RendaFixa
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rendaFixa == null)
            {
                return NotFound();
            }

            return View(rendaFixa);
        }

        // GET: RendaFixa/Delete/5  (se ainda usa a view dedicada, pode manter)
// Para abrir via modal na Index não precisa alterar este GET.

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string valor)
        {
            var usuarioId = ObterUsuarioId();

            // Busca o registro do usuário
            var rendaFixa = await _context.RendaFixa
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            if (rendaFixa == null)
            {
                TempData["Erro"] = "Renda fixa não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // valor vem com máscara "R$ 1.234,56" -> normaliza para decimal (pt-BR)
            if (string.IsNullOrWhiteSpace(valor))
            {
                TempData["Erro"] = "Informe um valor válido.";
                return RedirectToAction(nameof(Index));
            }

            // Remove "R$", espaços, pontos de milhar e troca vírgula por ponto
            var normalizado = valor.Replace("R$", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .Replace(".", "")
                .Replace(",", ".");
            if (!decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorVenda))
            {
                TempData["Erro"] = "Valor inválido.";
                return RedirectToAction(nameof(Index));
            }

            if (valorVenda <= 0)
            {
                TempData["Erro"] = "O valor deve ser maior que zero.";
                return RedirectToAction(nameof(Index));
            }

            if (valorVenda > rendaFixa.Valor)
            {
                TempData["Erro"] = "Você não pode vender mais do que possui.";
                return RedirectToAction(nameof(Index));
            }

            // Lançamento de venda
            var lancamento = new Lancamento
            {
                Movimentacao = "Venda",
                Produto = rendaFixa.Emissor,
                Quantidade = 1, // para renda fixa estamos registrando como 1 título/unidade
                ValorTotal = valorVenda, // valor resgatado
                Data = DateTime.Now,
                UsuarioId = usuarioId
            };
            _context.Lancamento.Add(lancamento);

            // Atualiza ou remove
            if (valorVenda == rendaFixa.Valor)
            {
                _context.RendaFixa.Remove(rendaFixa);
            }
            else
            {
                rendaFixa.Valor -= valorVenda;
                _context.RendaFixa.Update(rendaFixa);
            }

            await _context.SaveChangesAsync();
            TempData["Removido"] = true;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BuscarSelic([FromServices] IBrapi brapi)
        {
            var taxa = await brapi.ObterSelicAtual();
            return Json(new { taxa });
        }
    }
}