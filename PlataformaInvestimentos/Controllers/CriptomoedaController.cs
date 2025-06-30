using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers
{
    public class CriptomoedaController : Controller
    {
        private readonly Context _context;

        public CriptomoedaController(Context context)
        {
            _context = context;
        }

        // GET: Criptomoeda
        public async Task<IActionResult> Index()
        {
            var context = _context.Criptomoeda.Include(c => c.Usuario);
            return View(await context.ToListAsync());
        }

        // GET: Criptomoeda/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var criptomoeda = await _context.Criptomoeda
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (criptomoeda == null)
            {
                return NotFound();
            }

            return View(criptomoeda);
        }

        // GET: Criptomoeda/Create
        public IActionResult Create()
        {
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login");
            return View();
        }

        // POST: Criptomoeda/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Ticker,Nome,Logo,Quantidade,PrecoMedio,PrecoAtual,DataCompra,UsuarioId")] Criptomoeda criptomoeda)
        {
            if (ModelState.IsValid)
            {
                _context.Add(criptomoeda);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login", criptomoeda.UsuarioId);
            return View(criptomoeda);
        }

        // GET: Criptomoeda/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var criptomoeda = await _context.Criptomoeda.FindAsync(id);
            if (criptomoeda == null)
            {
                return NotFound();
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login", criptomoeda.UsuarioId);
            return View(criptomoeda);
        }

        // POST: Criptomoeda/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ticker,Nome,Logo,Quantidade,PrecoMedio,PrecoAtual,DataCompra,UsuarioId")] Criptomoeda criptomoeda)
        {
            if (id != criptomoeda.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(criptomoeda);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CriptomoedaExists(criptomoeda.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login", criptomoeda.UsuarioId);
            return View(criptomoeda);
        }

        // GET: Criptomoeda/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var criptomoeda = await _context.Criptomoeda
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (criptomoeda == null)
            {
                return NotFound();
            }

            return View(criptomoeda);
        }

        // POST: Criptomoeda/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var criptomoeda = await _context.Criptomoeda.FindAsync(id);
            if (criptomoeda != null)
            {
                _context.Criptomoeda.Remove(criptomoeda);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CriptomoedaExists(int id)
        {
            return _context.Criptomoeda.Any(e => e.Id == id);
        }
    }
}
