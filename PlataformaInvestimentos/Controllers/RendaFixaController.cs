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
    public class RendaFixaController : Controller
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

        // GET: RendaFixa/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: RendaFixa/Create
        public IActionResult Create()
        {
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login");
            return View();
        }

        // POST: RendaFixa/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Emissor,DataCompra,Valor,Taxa,UsuarioId")] RendaFixa rendaFixa)
        {
            if (ModelState.IsValid)
            {
                _context.Add(rendaFixa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login", rendaFixa.UsuarioId);
            return View(rendaFixa);
        }

        // GET: RendaFixa/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rendaFixa = await _context.RendaFixa.FindAsync(id);
            if (rendaFixa == null)
            {
                return NotFound();
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login", rendaFixa.UsuarioId);
            return View(rendaFixa);
        }

        // POST: RendaFixa/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Emissor,DataCompra,Valor,Taxa,UsuarioId")] RendaFixa rendaFixa)
        {
            if (id != rendaFixa.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rendaFixa);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RendaFixaExists(rendaFixa.Id))
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
            ViewData["UsuarioId"] = new SelectList(_context.Usuario, "Id", "Login", rendaFixa.UsuarioId);
            return View(rendaFixa);
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

        // POST: RendaFixa/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rendaFixa = await _context.RendaFixa.FindAsync(id);
            if (rendaFixa != null)
            {
                _context.RendaFixa.Remove(rendaFixa);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RendaFixaExists(int id)
        {
            return _context.RendaFixa.Any(e => e.Id == id);
        }
    }
}
