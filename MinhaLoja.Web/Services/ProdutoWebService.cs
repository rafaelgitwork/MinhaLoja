using Microsoft.AspNetCore.Mvc;
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

    public async Task<string[]> CriarAsync(ProdutoCreateRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/produtos", request);

        if (response.IsSuccessStatusCode)
        {
            return Array.Empty<string>();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var problema = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>();

            if (problema is not null && problema.Errors.Count > 0)
            {
                return problema.Errors.Values
                    .SelectMany(mensagens => mensagens)
                    .ToArray();
            }

            return new[] { "Confira os dados informados." };
        }

        return new[]
        {
        $"A API não conseguiu concluir o cadastro (HTTP {(int)response.StatusCode})."
    };
    }
}