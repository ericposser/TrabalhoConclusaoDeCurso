using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PlataformaInvestimentos.Controllers;

public class UtilController : Controller
{
    protected int ObterUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}