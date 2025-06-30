using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers
{
    [Authorize]
    public class LancamentoController : UtilController
    {
        private readonly Context _context;

        public LancamentoController(Context context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index(string sortOrder)
        {
            var usuarioId = ObterUsuarioId();

            ViewBag.CurrentSort  = sortOrder ?? "";  // <- guardo o sort atual
            ViewBag.MovSort      = sortOrder == "Movimentacao"  ? "mov_desc"        : "Movimentacao";
            ViewBag.ProdSort     = sortOrder == "Produto"       ? "produto_desc"    : "Produto";
            ViewBag.QtdSort      = sortOrder == "Quantidade"    ? "quantidade_desc" : "Quantidade";
            ViewBag.TotalSort    = sortOrder == "ValorTotal"    ? "valortotal_desc" : "ValorTotal";
            ViewBag.DataSort     = sortOrder == "Data"          ? "data_desc"       : "Data";

            var lancamentos = _context.Lancamento
                .Where(l => l.UsuarioId == usuarioId);

            lancamentos = sortOrder switch
            {
                "mov_desc"         => lancamentos.OrderByDescending(l => l.Movimentacao),
                "Movimentacao"     => lancamentos.OrderBy(l => l.Movimentacao),

                "Produto"          => lancamentos.OrderBy(l => l.Produto),
                "produto_desc"     => lancamentos.OrderByDescending(l => l.Produto),

                "Quantidade"       => lancamentos.OrderBy(l => l.Quantidade),
                "quantidade_desc"  => lancamentos.OrderByDescending(l => l.Quantidade),

                "ValorTotal"       => lancamentos.OrderBy(l => l.ValorTotal),
                "valortotal_desc"  => lancamentos.OrderByDescending(l => l.ValorTotal),

                "Data"             => lancamentos.OrderBy(l => l.Data),
                "data_desc"        => lancamentos.OrderByDescending(l => l.Data),

                _                  => lancamentos.OrderBy(l => l.Movimentacao),
            };

            return View(await lancamentos.ToListAsync());
        }
    }
}
