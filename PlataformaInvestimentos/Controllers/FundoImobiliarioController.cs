using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers
{
    [Authorize]
    public class FundoImobiliarioController : UtilController
    {
        private readonly Context _context;

        public FundoImobiliarioController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sortOrder)
        {
            var usuarioId = ObterUsuarioId();

            ViewBag.TickerSort = String.IsNullOrEmpty(sortOrder) ? "ticker_desc" : "";
            ViewBag.NomeSort = sortOrder == "Nome" ? "nome_desc" : "Nome";
            ViewBag.PrecoSort = sortOrder == "Preco" ? "preco_desc" : "Preco";
            ViewBag.QuantidadeSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";
            ViewBag.DataCompraSort = sortOrder == "DataCompra" ? "data_desc" : "DataCompra";

            var fundos = _context.FundoImobiliario
                .Where(f => f.UsuarioId == usuarioId);

            fundos = sortOrder switch
            {
                "ticker_desc" => fundos.OrderByDescending(f => f.Ticker),
                "Nome" => fundos.OrderBy(f => f.Nome),
                "nome_desc" => fundos.OrderByDescending(f => f.Nome),
                "Preco" => fundos.OrderBy(f => f.PrecoAtual),
                "preco_desc" => fundos.OrderByDescending(f => f.PrecoAtual),
                "Quantidade" => fundos.OrderBy(f => f.Quantidade),
                "quantidade_desc" => fundos.OrderByDescending(f => f.Quantidade),
                "DataCompra" => fundos.OrderBy(f => f.DataCompra),
                "data_desc" => fundos.OrderByDescending(f => f.DataCompra),
                _ => fundos.OrderBy(f => f.Ticker),
            };

            return View(await fundos.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ListarTodosFundos([FromServices] IBrapi brapi)
        {
            var fundos = await brapi.ObterTodosFundos();

            var result = fundos.Select(f => new
            {
                id = f.Ticker,
                text = $"{f.Ticker} - {f.Nome}",
                logo = f.Logo
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarAtivo(string fundo, [FromServices] IBrapi brapi)
        {
            if (string.IsNullOrWhiteSpace(fundo))
                return BadRequest("Fundo é obrigatório");

            var ativo = await brapi.ObterAtivo(fundo);
            if (ativo == null)
                return NotFound();

            return Json(new
            {
                nome = ativo.Nome,
                precoAtual = ativo.PrecoAtual
            });
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Ticker,Quantidade,PrecoMedio,DataCompra,PrecoAtual")] FundoImobiliario novoFundo, [FromServices] IBrapi brapi)
        {
            try
            {
                var usuarioId = ObterUsuarioId();

                var ativo = await brapi.ObterAtivo(novoFundo.Ticker);
                var valorNovaCompra = novoFundo.Quantidade * novoFundo.PrecoAtual;

                var fundoExistente = await _context.FundoImobiliario
                    .FirstOrDefaultAsync(f => f.Ticker == novoFundo.Ticker && f.UsuarioId == usuarioId);

                if (fundoExistente != null)
                {
                    fundoExistente.Quantidade += novoFundo.Quantidade;
                    fundoExistente.PrecoAtual += valorNovaCompra;
                    fundoExistente.DataCompra = novoFundo.DataCompra;
                    fundoExistente.Logo = ativo.Logo;
                    fundoExistente.Nome = ativo.Nome;

                    _context.Update(fundoExistente);
                }
                else
                {
                    novoFundo.UsuarioId = usuarioId;
                    novoFundo.Nome = ativo.Nome;
                    novoFundo.Logo = ativo.Logo;
                    novoFundo.PrecoAtual = valorNovaCompra;

                    _context.Add(novoFundo);
                }
                
                var lancamento = new Lancamento
                {
                    Movimentacao   = "Compra",
                    Produto        = novoFundo.Ticker,
                    Quantidade     = novoFundo.Quantidade,
                    ValorTotal     = valorNovaCompra,
                    Data           = novoFundo.DataCompra,
                    UsuarioId      = usuarioId
                };
                _context.Add(lancamento);

                await _context.SaveChangesAsync();
                TempData["Sucesso"] = true;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar o fundo. Tente novamente.");
                return View(novoFundo);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var usuarioId = ObterUsuarioId();

            if (id == null)
                return NotFound();

            var fundo = await _context.FundoImobiliario
                .FirstOrDefaultAsync(f => f.Id == id && f.UsuarioId == usuarioId);

            if (fundo == null)
                return NotFound();

            return View(fundo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int quantidade, decimal preco)
        {
            var usuarioId = ObterUsuarioId();

            var fundo = await _context.FundoImobiliario
                .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId);

            var lancamento = new Lancamento
            {
                Movimentacao = "Venda",
                Produto = fundo.Ticker,
                Quantidade = quantidade,
                ValorTotal = quantidade * preco,
                Data = DateTime.Now,
                UsuarioId = usuarioId
            };

            _context.Lancamento.Add(lancamento);

            if (quantidade == fundo.Quantidade)
            {
                _context.FundoImobiliario.Remove(fundo);
            }
            else
            {
                fundo.Quantidade -= quantidade;
                fundo.PrecoAtual -= quantidade * preco;
                _context.FundoImobiliario.Update(fundo);
            }

            await _context.SaveChangesAsync();
            TempData["Removido"] = true;

            return RedirectToAction(nameof(Index));
        }
    }
}