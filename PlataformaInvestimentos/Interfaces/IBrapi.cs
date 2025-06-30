using Microsoft.Build.Framework;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Interfaces;

public interface IBrapi
{
    Task<Brapi?> ObterAtivo(string ticker);
    
    Task<List<Brapi>> ObterTodasAcoes();
    
    Task<List<Brapi>> ObterTodosFundos();
    
}