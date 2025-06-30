using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
public class AcaoController : UtilController
{
    private readonly Context _context;

    public AcaoController(Context context)
    {
        _context = context;
    }
    
    [HttpGet] 
    public async Task<IActionResult> Index(string sortOrder)
    {
        var usuarioId = ObterUsuarioId();
        
        var acoes = _context.Acao.Where(a => a.UsuarioId == usuarioId);
        
        ViewBag.TickerSort = string.IsNullOrEmpty(sortOrder) ? "ticker_desc" : "";
        ViewBag.NomeSort = sortOrder == "Nome" ? "nome_desc" : "Nome";
        ViewBag.PrecoSort = sortOrder == "Preco" ? "preco_desc" : "Preco";
        ViewBag.QuantidadeSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";
        
        acoes = sortOrder switch
        {
            "ticker_desc"      => acoes.OrderByDescending(a => a.Ticker),
            "Nome"             => acoes.OrderBy(a => a.Nome),
            "nome_desc"        => acoes.OrderByDescending(a => a.Nome),
            "Preco"            => acoes.OrderBy(a => a.PrecoAtual),
            "preco_desc"       => acoes.OrderByDescending(a => a.PrecoAtual),
            "Quantidade"       => acoes.OrderBy(a => a.Quantidade),
            "quantidade_desc"  => acoes.OrderByDescending(a => a.Quantidade),
            _                  => acoes.OrderBy(a => a.Ticker),
        };
        
        return View(await acoes.ToListAsync());
    }
    
    [HttpGet]
    public async Task<IActionResult> ListarTodasAcoes([FromServices] IBrapi brapi)
    {
        var tickers = await brapi.ObterTodasAcoes();

        var result = tickers.Select(t => new
        {
            id = t.Ticker,
            text = $"{t.Ticker} - {t.Nome}",
            logo = t.Logo
        });

        return Json(result);
    }
    
    [HttpGet]
    public async Task<IActionResult> BuscarAtivo(string acao, [FromServices] IBrapi brapi)
    {
        var ativo = await brapi.ObterAtivo(acao);

        if (ativo is null)
        {
            return NotFound(new { mensagem = "Ativo não encontrado." });
        }
        
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
    public async Task<IActionResult> Create([Bind("Ticker,Quantidade,PrecoMedio,DataCompra,PrecoAtual")] Acao novaAcao, [FromServices] IBrapi brapi)
    {
        try
        {
            var usuarioId = ObterUsuarioId();
            
            var ativo = await brapi.ObterAtivo(novaAcao.Ticker);
            
            var valorNovaCompra  = novaAcao.Quantidade * novaAcao.PrecoAtual;
            
            var acaoExistente = await _context.Acao
                .FirstOrDefaultAsync(a => a.Ticker == novaAcao.Ticker && a.UsuarioId == usuarioId);

            if (acaoExistente != null)
            {
                acaoExistente.Quantidade += novaAcao.Quantidade;
                acaoExistente.PrecoAtual += valorNovaCompra;
                acaoExistente.DataCompra = novaAcao.DataCompra;
                acaoExistente.Logo = ativo.Logo;
                acaoExistente.Nome = ativo.Nome;

                _context.Update(acaoExistente);
            }
            else
            {
                novaAcao.UsuarioId = usuarioId;
                novaAcao.Nome = ativo.Nome;
                novaAcao.Logo = ativo.Logo;
                novaAcao.PrecoAtual = valorNovaCompra;

                _context.Add(novaAcao);
            }
            
            var lancamento = new Lancamento
            {
                Movimentacao   = "Compra",
                Produto        = novaAcao.Ticker,
                Quantidade     = novaAcao.Quantidade,
                ValorTotal     = valorNovaCompra,
                Data           = novaAcao.DataCompra,
                UsuarioId      = usuarioId
            };
            _context.Add(lancamento);
            
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = true;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar a ação. Tente novamente.");
            return View(novaAcao);
        }
    }
    
    public async Task<IActionResult> Delete(int? id)
    {
        var usuarioId = ObterUsuarioId();

        if (id == null)
            return NotFound();

        var acao = await _context.Acao
            .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId);

        if (acao == null)
            return NotFound();

        return View(acao);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int quantidade, decimal preco)
    {
        var usuarioId = ObterUsuarioId();

        var acao = await _context.Acao
            .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId);

        var lancamento = new Lancamento
        {
            Movimentacao = "Venda",
            Produto = acao.Ticker,
            Quantidade = quantidade,
            ValorTotal = quantidade * preco,
            Data = DateTime.Now,
            UsuarioId = usuarioId
        };

        _context.Lancamento.Add(lancamento);

        if (quantidade == acao.Quantidade)
        {
            _context.Acao.Remove(acao);
        }
        else
        {
            acao.Quantidade -= quantidade;
            acao.PrecoAtual -= quantidade * preco;
            _context.Acao.Update(acao);
        }

        await _context.SaveChangesAsync();
        TempData["Removido"] = true;

        return RedirectToAction(nameof(Index));
    }
}
}
