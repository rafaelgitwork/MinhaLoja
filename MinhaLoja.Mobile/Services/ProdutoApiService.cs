using System.Net.Http.Json;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Mobile.Services;

public class ProdutoApiService
{
    private readonly HttpClient _httpClient;

    public ProdutoApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProdutoResponse>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/produtos",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<ProdutoResponse>>(
                cancellationToken: cancellationToken) ?? new();
    }
}