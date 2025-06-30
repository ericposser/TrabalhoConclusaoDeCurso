using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Models;
using Microsoft.AspNetCore.Identity;

namespace PlataformaInvestimentos.Controllers
{
    public class UsuarioController : UtilController
    {
        private readonly Context _context;

        public UsuarioController(Context context)
        {
            _context = context;
        }
        
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string login, string senha)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Login == login);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuário ou senha inválidos.");
                TempData["Erro"] = true;
                return View();
            }

            var hasher = new PasswordHasher<Usuario>();
            var resultado = hasher.VerifyHashedPassword(usuario, usuario.Senha, senha);

            if (resultado != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Usuário ou senha inválidos.");
                TempData["Erro"] = true;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Login)
            };

            var identity = new ClaimsIdentity(claims, "LoginCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("LoginCookie", principal);
            
            return RedirectToAction("Index", "Home");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("LoginCookie");
            return RedirectToAction("Login", "Usuario");
        }
        
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Login,SenhaTexto,ConfirmarSenha")] Usuario usuario)
        {
            var loginExistente = await _context.Usuario
                .AnyAsync(u => u.Login == usuario.Login);

            if (loginExistente)
            {
                ModelState.AddModelError("Login", "Este nome de usuário já está em uso.");
                return View(usuario);
            }

            var hasher = new PasswordHasher<Usuario>();
            usuario.Senha = hasher.HashPassword(usuario, usuario.SenhaTexto);

            _context.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Cadastro"] = true;
            return RedirectToAction("Index", "Home");
        }
        
        // GET: Usuario/TrocarNome
        public async Task<IActionResult> TrocarNome()
        {
            var usuarioId = ObterUsuarioId();
            var usuario = await _context.Usuario.FindAsync(usuarioId);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrocarNome(string novoNome)
        {
            var usuarioId = ObterUsuarioId();
            var usuario = await _context.Usuario.FindAsync(usuarioId);
            
            var loginExistente = await _context.Usuario
                .AnyAsync(u => u.Login == novoNome);

            if (loginExistente)
            {
                ModelState.AddModelError("Login", "Este nome de usuário já está em uso.");
                ViewBag.NomeDigitado = novoNome;
                return View();
            }

            usuario.Login = novoNome;
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync("LoginCookie");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, novoNome)
            };

            var identity = new ClaimsIdentity(claims, "LoginCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("LoginCookie", principal);

            TempData["Sucesso"] = "Nome atualizado com sucesso!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Usuario/TrocarSenha
        public IActionResult TrocarSenha() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrocarSenha(string novaSenha, string confirmarSenha)
        {
            var usuarioId = ObterUsuarioId();
            var usuario = await _context.Usuario.FindAsync(usuarioId);

            if (novaSenha != confirmarSenha)
            {
                ModelState.AddModelError("", "As senhas não coincidem.");
                return View();
            }
            
            var hasher = new PasswordHasher<Usuario>();
            usuario.Senha = hasher.HashPassword(usuario, novaSenha);
            
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Senha alterada com sucesso!";
            return RedirectToAction("Index", "Home");
        }
        
    }
}
