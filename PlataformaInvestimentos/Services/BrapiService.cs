using System.Text.Json;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Services;

public class BrapiService : IBrapi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public BrapiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<Brapi?> ObterAtivo(string ticker)
    {
        var token = _config["Brapi:Token"];
        
        var url = $"https://brapi.dev/api/quote/{ticker}?token={token}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonParaString = await response.Content.ReadAsStringAsync();
        using var documentoJson = JsonDocument.Parse(jsonParaString);
        var ativoJson = documentoJson.RootElement.GetProperty("results")[0];

        return new Brapi
        {
            Ticker = ativoJson.GetProperty("symbol").GetString() ?? string.Empty,
            Nome = ativoJson.GetProperty("longName").GetString() ?? string.Empty,
            Logo = ativoJson.GetProperty("logourl").GetString() ?? string.Empty,
            PrecoAtual = ativoJson.GetProperty("regularMarketPrice").GetDecimal()
        };
        
    }
    
    public async Task<List<Brapi>> ObterTodasAcoes()
    {
        var token = _config["Brapi:Token"];
        var url = $"https://brapi.dev/api/quote/list?token={token}&type=stock";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<Brapi>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var resultado = new List<Brapi>();

        if (doc.RootElement.TryGetProperty("stocks", out var stocks) && stocks.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in stocks.EnumerateArray())
            {
                resultado.Add(new Brapi
                {
                    Ticker = item.GetProperty("stock").GetString(),
                    Nome = item.GetProperty("name").GetString(),
                    Logo = item.TryGetProperty("logo", out var logoProp) && logoProp.ValueKind == JsonValueKind.String
                        ? logoProp.GetString()
                        : ""
                });
            }
        }

        return resultado;
    }
    
    public async Task<List<Brapi>> ObterTodosFundos()
    {
        var token = _config["Brapi:Token"];
        var url = $"https://brapi.dev/api/quote/list?token={token}&type=fund";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<Brapi>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var resultado = new List<Brapi>();

        if (doc.RootElement.TryGetProperty("stocks", out var stocks) && stocks.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in stocks.EnumerateArray())
            {
                resultado.Add(new Brapi
                {
                    Ticker = item.GetProperty("stock").GetString(),
                    Nome = item.GetProperty("name").GetString(),
                    Logo = item.TryGetProperty("logo", out var logoProp) && logoProp.ValueKind == JsonValueKind.String
                        ? logoProp.GetString()
                        : ""
                });
            }
        }

        return resultado;
    }
}