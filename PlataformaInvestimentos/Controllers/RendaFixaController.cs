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
                if (rendaFixa.DataVencimento <= rendaFixa.DataCompra)
                {
                    ModelState.AddModelError("DataVencimento",
                        "A data de vencimento não pode ser menor que a data de compra.");
                    return View(rendaFixa);
                }

                var usuarioId = ObterUsuarioId();
                double taxaFinal;
                
                var forma = Request.Form.TryGetValue("Forma", out var formaVals)
                    ? formaVals.ToString()
                    : "pre"; 

                if (forma == "pre")
                {
                    var taxaStr = Request.Form["Taxa"].ToString().Replace("%", "").Replace(",", ".").Trim();
                    taxaFinal = double.TryParse(taxaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tx)
                        ? tx
                        : 0.0;
                }
                else
                {
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
                
                var liquidezLigada = Request.Form.TryGetValue("ToggleLiquidez", out var liqVals)
                                     && (liqVals.ToString().Contains("on", StringComparison.OrdinalIgnoreCase)
                                         || liqVals.ToString().Equals("true", StringComparison.OrdinalIgnoreCase));

                if (liquidezLigada)
                {
                    rendaFixa.DataVencimento = null; 
                }

                rendaFixa.UsuarioId = usuarioId;
                rendaFixa.Taxa = taxaFinal;

                _context.Add(rendaFixa);
                
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string valor)
        {
            var usuarioId = ObterUsuarioId();
            
            var rendaFixa = await _context.RendaFixa
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            if (rendaFixa == null)
            {
                TempData["Erro"] = "Renda fixa não encontrada.";
                return RedirectToAction(nameof(Index));
            }
            
            if (string.IsNullOrWhiteSpace(valor))
            {
                TempData["Erro"] = "Informe um valor válido.";
                return RedirectToAction(nameof(Index));
            }
            
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
            
            var lancamento = new Lancamento
            {
                Movimentacao = "Venda",
                Produto = rendaFixa.Emissor,
                Quantidade = 1,
                ValorTotal = valorVenda,
                Data = DateTime.Now,
                UsuarioId = usuarioId
            };
            _context.Lancamento.Add(lancamento);
            
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