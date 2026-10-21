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

    public async Task<ProdutoResponse?> ObterPorIdAsync(int id)
    {
        using var response = await _httpClient.GetAsync($"api/produtos/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProdutoResponse>();
    }

    public async Task<string[]> AtualizarAsync(
        int id,
        ProdutoUpdateRequest request)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/produtos/{id}", request);

        if (response.IsSuccessStatusCode)
        {
            return Array.Empty<string>();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new[] { "O produto não foi encontrado ou está inativo." };
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
        $"Não foi possível atualizar o produto (HTTP {(int)response.StatusCode})."
    };
    }
}