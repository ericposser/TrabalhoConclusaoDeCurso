using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;
using System.Globalization;

namespace PlataformaInvestimentos.Controllers
{ 
    [Authorize]
   public class CriptomoedaController : UtilController
{
    private readonly Context _context;

    public CriptomoedaController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string sortOrder)
    {
        var usuarioId = ObterUsuarioId();
        var criptos = _context.Criptomoeda.Where(c => c.UsuarioId == usuarioId);

        ViewBag.TickerSort = string.IsNullOrEmpty(sortOrder) ? "ticker_desc" : "";
        ViewBag.NomeSort = sortOrder == "Nome" ? "nome_desc" : "Nome";
        ViewBag.PrecoSort = sortOrder == "Preco" ? "preco_desc" : "Preco";
        ViewBag.QuantidadeSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";

        criptos = sortOrder switch
        {
            "ticker_desc"      => criptos.OrderByDescending(c => c.Ticker),
            "Nome"             => criptos.OrderBy(c => c.Nome),
            "nome_desc"        => criptos.OrderByDescending(c => c.Nome),
            "Preco"            => criptos.OrderBy(c => c.PrecoAtual),
            "preco_desc"       => criptos.OrderByDescending(c => c.PrecoAtual),
            "Quantidade"       => criptos.OrderBy(c => c.Quantidade),
            "quantidade_desc"  => criptos.OrderByDescending(c => c.Quantidade),
            _                  => criptos.OrderBy(c => c.Ticker),
        };

        return View(await criptos.ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> ListarTodasCriptos([FromServices] IBrapi brapi)
    {
        var criptos = await brapi.ObterTodasCripto();

        var result = criptos.Select(c => new
        {
            id = c.Ticker,
            text = $"{c.Ticker} - {c.Nome}",
            logo = c.Logo
        });

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarCripto(string cripto, [FromServices] IBrapi brapi)
    {
        var criptomoeda = await brapi.ObterCripto(cripto);

        if (criptomoeda is null)
        {
            return NotFound(new { mensagem = "Criptomoeda não encontrada." });
        }

        return Json(new
        {
            nome = criptomoeda.Nome,
            precoAtual = criptomoeda.PrecoAtual
        });
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
   public async Task<IActionResult> Create(string Ticker, string Quantidade, string PrecoAtual, DateTime DataCompra, [FromServices] IBrapi brapi)
{
    try
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var quantidadeParsed = decimal.Parse(Quantidade.Replace(",", "."), culture);
        var precoParsed = decimal.Parse(PrecoAtual.Replace(",", "."), culture);

        var novaCripto = new Criptomoeda
        {
            Ticker = Ticker,
            Quantidade = quantidadeParsed,
            PrecoAtual = precoParsed,
            DataCompra = DataCompra
        };

        var usuarioId = ObterUsuarioId();
        var ativo = await brapi.ObterCripto(Ticker);

        //var valorTotal = novaCripto.Quantidade * novaCripto.PrecoAtual;

        var existente = await _context.Criptomoeda
            .FirstOrDefaultAsync(c => c.Ticker == Ticker && c.UsuarioId == usuarioId);

        if (existente != null)
        {
            existente.Quantidade += novaCripto.Quantidade;
            existente.PrecoAtual += novaCripto.PrecoAtual;
            existente.DataCompra = novaCripto.DataCompra;
            existente.Nome = ativo.Nome;
            existente.Logo = ativo.Logo;
            _context.Update(existente);
        }
        else
        {
            novaCripto.UsuarioId = usuarioId;
            novaCripto.Nome = ativo.Nome;
            novaCripto.Logo = ativo.Logo;
            novaCripto.PrecoAtual = novaCripto.PrecoAtual;
            _context.Add(novaCripto);
        }

        var lancamento = new Lancamento
        {
            Movimentacao = "Compra",
            Produto = Ticker,
            Quantidade = novaCripto.Quantidade,
            ValorTotal = novaCripto.PrecoAtual,
            Data = DataCompra,
            UsuarioId = usuarioId
        };
        _context.Lancamento.Add(lancamento);

        await _context.SaveChangesAsync();
        TempData["Sucesso"] = true;
        return RedirectToAction(nameof(Index));
    }
    catch
    {
        ModelState.AddModelError(string.Empty, "Erro ao salvar a criptomoeda. Tente novamente.");
        return View();
    }
}

    public async Task<IActionResult> Delete(int? id)
    {
        var usuarioId = ObterUsuarioId();

        if (id == null)
            return NotFound();

        var cripto = await _context.Criptomoeda.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (cripto == null)
            return NotFound();

        return View(cripto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int quantidade, decimal preco)
    {
        var usuarioId = ObterUsuarioId();

        var cripto = await _context.Criptomoeda.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);

        var lancamento = new Lancamento
        {
            Movimentacao = "Venda",
            Produto = cripto.Ticker,
            Quantidade = quantidade,
            ValorTotal = quantidade * preco,
            Data = DateTime.Now,
            UsuarioId = usuarioId
        };

        _context.Lancamento.Add(lancamento);

        if (quantidade == cripto.Quantidade)
        {
            _context.Criptomoeda.Remove(cripto);
        }
        else
        {
            cripto.Quantidade -= quantidade;
            cripto.PrecoAtual -= quantidade * preco;
            _context.Criptomoeda.Update(cripto);
        }

        await _context.SaveChangesAsync();
        TempData["Removido"] = true;
        return RedirectToAction(nameof(Index));
    }
}
}
