using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Web.Services;

public class ProdutoWebService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ProdutoWebService(
        HttpClient httpClient,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<List<ProdutoResponse>> ListarAsync()
    {
        return await _httpClient
            .GetFromJsonAsync<List<ProdutoResponse>>("api/produtos")
            ?? new List<ProdutoResponse>();
    }

    public async Task<ProdutoResponse?> ObterPorIdAsync(int id)
    {
        using var response = await _httpClient.GetAsync(
            $"api/produtos/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProdutoResponse>();
    }

    public Task<string[]> CriarAsync(ProdutoCreateRequest request)
    {
        return EnviarAutenticadoAsync(
            HttpMethod.Post,
            "api/produtos",
            request);
    }

    public Task<string[]> AtualizarAsync(
        int id,
        ProdutoUpdateRequest request)
    {
        return EnviarAutenticadoAsync(
            HttpMethod.Put,
            $"api/produtos/{id}",
            request);
    }

    public async Task<string[]> ReporEstoqueAsync(
    int id,
    ProdutoReposicaoRequest request)
    {
        return await EnviarAutenticadoAsync(
            HttpMethod.Post,
            $"api/produtos/{id}/estoque/reposicoes",
            request);
    }

    private async Task<string[]> EnviarAutenticadoAsync<T>(
        HttpMethod metodo,
        string endereco,
        T dados)
    {
        var estado = await _authenticationStateProvider
            .GetAuthenticationStateAsync();

        var usuario = estado.User;

        if (usuario.Identity?.IsAuthenticated != true)
        {
            return new[] { "Entre no painel antes de salvar." };
        }

        var token = usuario.FindFirst("api_token")?.Value;
        var expiracao = usuario.FindFirst("api_expira_em")?.Value;

        if (string.IsNullOrWhiteSpace(token)
            || !long.TryParse(expiracao, out var expiraEm)
            || expiraEm <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return new[] { "Sua sessão expirou. Entre novamente no painel." };
        }

        using var mensagem = new HttpRequestMessage(metodo, endereco);

        mensagem.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        mensagem.Content = JsonContent.Create(dados);

        using var response = await _httpClient.SendAsync(mensagem);

        if (response.IsSuccessStatusCode)
        {
            return Array.Empty<string>();
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new[]
            {
                "Sua autenticação não é mais válida. Entre novamente."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new[]
            {
                "Seu usuário não tem permissão para executar esta operação."
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new[] { "O produto não foi encontrado ou está inativo." };
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
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

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new[]
            {
        "A operação entrou em conflito com o saldo atual. Confira o estoque."
    };
        }

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException(
                "O servidor apresentou uma falha ao processar a operação.",
                null,
                response.StatusCode);
        }

        return new[]
        {
            $"A API não concluiu a operação (HTTP {(int)response.StatusCode})."
        };
    }
}