using Microsoft.Maui.Storage;
using MinhaLoja.Mobile.Models;
using MinhaLoja.Shared.DTOs.Produtos;
using SQLite;

namespace MinhaLoja.Mobile.Services;

public class CatalogoLocalService
{
    private readonly SemaphoreSlim _acesso = new(1, 1);
    private SQLiteAsyncConnection? _database;

    private async Task InicializarAsync()
    {
        if (_database is not null)
            return;

        var caminho = Path.Combine(
            FileSystem.AppDataDirectory,
            "minhaloja-cache.db3");

        var conexao = new SQLiteAsyncConnection(caminho);

        await conexao.CreateTableAsync<ProdutoLocal>();
        await conexao.CreateTableAsync<CacheCatalogoInfo>();

        _database = conexao;
    }

    public async Task SalvarAsync(List<ProdutoResponse> produtos)
    {
        var registros = produtos.Select(p => new ProdutoLocal
        {
            Id = p.Id,
            Nome = p.Nome,
            Descricao = p.Descricao,
            PrecoCentavos = checked((long)decimal.Round(p.Preco * 100m, 0)),
            QuantidadeEstoque = p.QuantidadeEstoque,
            CategoriaId = p.CategoriaId
        }).ToList();

        var informacao = new CacheCatalogoInfo
        {
            Id = 1,
            AtualizadoEmUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await _acesso.WaitAsync();

        try
        {
            await InicializarAsync();

            // Substitui o catálogo inteiro em uma única transação.
            // Se houver falha, a cópia anterior é preservada.
            await _database!.RunInTransactionAsync(conexao =>
            {
                conexao.DeleteAll<ProdutoLocal>();

                foreach (var produto in registros)
                {
                    conexao.Insert(produto);
                }

                conexao.InsertOrReplace(informacao);
            });
        }
        finally
        {
            _acesso.Release();
        }
    }

    public async Task<(
        List<ProdutoResponse> Produtos,
        DateTimeOffset? AtualizadoEm)> LerAsync()
    {
        await _acesso.WaitAsync();

        try
        {
            await InicializarAsync();

            var informacao = await _database!
                .FindAsync<CacheCatalogoInfo>(1);

            if (informacao is null)
            {
                return (new List<ProdutoResponse>(), null);
            }

            var registros = await _database.Table<ProdutoLocal>()
                .OrderBy(p => p.Nome)
                .ToListAsync();

            var produtos = registros.Select(p => new ProdutoResponse
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                Preco = p.PrecoCentavos / 100m,
                QuantidadeEstoque = p.QuantidadeEstoque,
                CategoriaId = p.CategoriaId
            }).ToList();

            return (
                produtos,
                DateTimeOffset.FromUnixTimeSeconds(
                    informacao.AtualizadoEmUnix));
        }
        finally
        {
            _acesso.Release();
        }
    }
}