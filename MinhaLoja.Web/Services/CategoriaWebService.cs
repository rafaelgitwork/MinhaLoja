using System.Net.Http.Json;
using MinhaLoja.Shared.DTOs.Categorias;

namespace MinhaLoja.Web.Services;

public class CategoriaWebService
{
    private readonly HttpClient _httpClient;

    public CategoriaWebService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CategoriaResponse>> ListarAsync()
    {
        return await _httpClient
            .GetFromJsonAsync<List<CategoriaResponse>>("api/categorias")
            ?? new List<CategoriaResponse>();
    }
}