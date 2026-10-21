using System.Net.Http.Json;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Web.Services;

public class ProdutoWebService
{
    private readonly HttpClient _httpClient;

    public ProdutoWebService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProdutoResponse>> ListarAsync()
    {
        return await _httpClient
            .GetFromJsonAsync<List<ProdutoResponse>>("api/produtos")
            ?? new List<ProdutoResponse>();
    }
}