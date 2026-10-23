using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MinhaLoja.Shared.DTOs.Pedidos;

namespace MinhaLoja.Mobile.Services;

public class PedidoApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthApiService _authService;

    public PedidoApiService(
        HttpClient httpClient,
        AuthApiService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<PedidoResponse> CriarAsync(
        IReadOnlyList<CarrinhoItem> itens,
        string? observacao)
    {
        var token = _authService.ObterToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Entre na aba Conta antes de finalizar o pedido.");
        }

        var dados = new PedidoCreateRequest
        {
            Observacao = string.IsNullOrWhiteSpace(observacao)
                ? null
                : observacao.Trim(),

            Itens = itens.Select(i => new PedidoItemCreateRequest
            {
                ProdutoId = i.ProdutoId,
                Quantidade = i.Quantidade
            }).ToList()
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/pedidos");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(dados);

        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "Sua sessão expirou. Entre novamente na aba Conta.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Sua conta não tem permissão para criar pedidos.");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                "O pedido não foi criado. Um produto pode estar " +
                "indisponível ou sem estoque suficiente. " +
                "Atualize o catálogo e revise o carrinho.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                "O pedido não foi criado. Confira os itens e a observação.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PedidoResponse>()
            ?? throw new HttpRequestException(
                "A API não retornou a confirmação do pedido.");
    }
}