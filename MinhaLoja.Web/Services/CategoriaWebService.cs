using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using MinhaLoja.Shared.DTOs.Categorias;

namespace MinhaLoja.Web.Services;

public class CategoriaWebService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CategoriaWebService(
        HttpClient httpClient,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
    }

    // Usado pelos formulários de produtos: somente categorias ativas.
    public async Task<List<CategoriaResponse>> ListarAsync()
    {
        return await _httpClient
            .GetFromJsonAsync<List<CategoriaResponse>>("api/categorias")
            ?? new();
    }

    public async Task<List<CategoriaResponse>> ListarGestaoAsync()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/categorias/gestao");

        await AdicionarAutenticacaoAsync(request);

        using var response = await _httpClient.SendAsync(request);

        ValidarAutenticacao(response);
        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<CategoriaResponse>>() ?? new();
    }

    public Task<string?> CriarAsync(CategoriaCreateRequest dados)
    {
        return EnviarAsync(
            HttpMethod.Post,
            "api/categorias",
            dados);
    }

    public Task<string?> AtualizarAsync(
        int id,
        CategoriaUpdateRequest dados)
    {
        return EnviarAsync(
            HttpMethod.Put,
            $"api/categorias/{id}",
            dados);
    }

    private async Task<string?> EnviarAsync<T>(
        HttpMethod metodo,
        string endereco,
        T dados)
    {
        using var request = new HttpRequestMessage(metodo, endereco)
        {
            Content = JsonContent.Create(dados)
        };

        await AdicionarAutenticacaoAsync(request);

        using var response = await _httpClient.SendAsync(request);

        ValidarAutenticacao(response);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return "Confira o nome da categoria. Ele deve ter de 1 a 100 caracteres.";
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return "Já existe uma categoria com esse nome, ativa ou inativa. " +
                   "Edite ou reative a existente.";
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return "A categoria não foi encontrada. Atualize a lista.";
        }

        response.EnsureSuccessStatusCode();

        return null;
    }

    private async Task AdicionarAutenticacaoAsync(
        HttpRequestMessage request)
    {
        var estado = await _authenticationStateProvider
            .GetAuthenticationStateAsync();

        var usuario = estado.User;
        var token = usuario.FindFirst("api_token")?.Value;
        var expiracao = usuario.FindFirst("api_expira_em")?.Value;

        if (usuario.Identity?.IsAuthenticated != true ||
            string.IsNullOrWhiteSpace(token) ||
            !long.TryParse(expiracao, out var expiraEm) ||
            expiraEm <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            throw new UnauthorizedAccessException(
                "Sua sessão expirou. Entre novamente no painel.");
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static void ValidarAutenticacao(
        HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "Sua autenticação não é mais válida. Entre novamente.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "Seu usuário não tem permissão para gerenciar categorias.");
        }
    }
}