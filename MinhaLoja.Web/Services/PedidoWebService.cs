using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using MinhaLoja.Shared.DTOs.Pedidos;
using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Web.Services;

public class PedidoWebService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public PedidoWebService(
        HttpClient httpClient,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<List<PedidoGestaoResponse>> ListarAsync(
        PedidoStatus? status,
        int pagina,
        int tamanhoPagina = 10)
    {
        var endereco =
            $"api/gestao/pedidos?pagina={pagina}&tamanhoPagina={tamanhoPagina}";

        if (status.HasValue)
        {
            endereco += $"&status={(int)status.Value}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, endereco);

        await AdicionarAutenticacaoAsync(request);

        using var response = await _httpClient.SendAsync(request);

        ValidarAutenticacao(response);
        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<PedidoGestaoResponse>>() ?? new();
    }

    public async Task<string?> AtualizarStatusAsync(
        int pedidoId,
        PedidoStatus statusAtual,
        PedidoStatus novoStatus)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/gestao/pedidos/{pedidoId}/status");

        request.Content = JsonContent.Create(new PedidoStatusUpdateRequest
        {
            StatusAtual = statusAtual,
            NovoStatus = novoStatus
        });

        await AdicionarAutenticacaoAsync(request);

        using var response = await _httpClient.SendAsync(request);

        ValidarAutenticacao(response);

        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict =>
                "O pedido mudou ou essa transição não é permitida. " +
                "Confira a lista atualizada.",

            HttpStatusCode.NotFound =>
                "O pedido não foi encontrado.",

            HttpStatusCode.BadRequest =>
                "Os dados enviados para alterar o status são inválidos.",

            _ => "Não foi possível atualizar o status do pedido."
        };
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
                "Seu usuário não tem permissão para realizar essa ação.");
        }
    }
}