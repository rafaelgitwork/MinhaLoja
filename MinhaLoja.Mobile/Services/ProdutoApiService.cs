using System.Diagnostics;
using System.Net.Http.Json;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Mobile.Services;

public class CatalogoResultado
{
    public List<ProdutoResponse> Produtos { get; init; } = new();

    public bool UsandoCache { get; init; }

    public string Aviso { get; init; } = "";
}

public class ProdutoApiService
{
    private readonly HttpClient _httpClient;
    private readonly CatalogoLocalService _catalogoLocal;

    public ProdutoApiService(
        HttpClient httpClient,
        CatalogoLocalService catalogoLocal)
    {
        _httpClient = httpClient;
        _catalogoLocal = catalogoLocal;
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
                cancellationToken: cancellationToken)
            ?? throw new System.Text.Json.JsonException(
                "A API retornou um catálogo inválido.");
    }

    public async Task<CatalogoResultado> ObterCatalogoAsync()
    {
        List<ProdutoResponse> produtos;

        try
        {
            produtos = await ListarAsync();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode is null || (int)ex.StatusCode.Value >= 500)
        {
            return await RecuperarCacheAsync();
        }
        catch (TaskCanceledException)
        {
            return await RecuperarCacheAsync();
        }

        // Uma falha no SQLite não impede exibir os dados recebidos da API.
        try
        {
            await _catalogoLocal.SalvarAsync(produtos);

            return new CatalogoResultado
            {
                Produtos = produtos,
                Aviso = "Catálogo atualizado e salvo no dispositivo."
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Falha ao salvar catálogo local: {ex}");

            return new CatalogoResultado
            {
                Produtos = produtos,
                Aviso =
                    "Catálogo atualizado pela API, mas não foi possível " +
                    "salvar a cópia para consulta sem conexão."
            };
        }
    }

    private async Task<CatalogoResultado> RecuperarCacheAsync()
    {
        List<ProdutoResponse> produtos;
        DateTimeOffset? atualizadoEm;

        try
        {
            var cache = await _catalogoLocal.LerAsync();

            produtos = cache.Produtos;
            atualizadoEm = cache.AtualizadoEm;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Falha ao ler catálogo local: {ex}");

            throw new InvalidOperationException(
                "A API está indisponível e não foi possível ler " +
                "o catálogo salvo neste dispositivo.");
        }

        if (atualizadoEm is null)
        {
            throw new InvalidOperationException(
                "A API está indisponível e ainda não existe catálogo salvo. " +
                "Conecte-se à API pelo menos uma vez.");
        }

        var data = atualizadoEm.Value
            .ToLocalTime()
            .ToString("dd/MM/yyyy HH:mm");

        return new CatalogoResultado
        {
            Produtos = produtos,
            UsandoCache = true,
            Aviso =
                $"Exibindo catálogo local salvo em {data}. " +
                "Preços e estoque podem estar desatualizados. " +
                "A finalização da compra exige conexão."
        };
    }
}