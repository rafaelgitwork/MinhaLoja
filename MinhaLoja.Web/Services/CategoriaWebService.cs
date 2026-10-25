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

    public async Task<List<CategoriaResponse>> ListarAsync()
    {
        return await _httpClient
            .GetFromJsonAsync<List<CategoriaResponse>>("api/categorias")
            ?? new List<CategoriaResponse>();
    }

    public async Task<string?> CriarAsync(CategoriaCreateRequest dados)
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

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/categorias");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(dados);

        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "Sua autenticação não é mais válida. Entre novamente.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "Seu usuário não tem permissão para cadastrar categorias.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return "Confira o nome da categoria. Ele deve ter de 1 a 100 caracteres.";
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return "O cadastro entrou em conflito com os dados existentes. Atualize a lista.";
        }

        response.EnsureSuccessStatusCode();

        return null;
    }
}