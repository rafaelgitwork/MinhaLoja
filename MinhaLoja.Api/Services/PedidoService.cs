using Microsoft.EntityFrameworkCore;
using MinhaLoja.Api.Data;
using MinhaLoja.Api.Entities;
using MinhaLoja.Shared.DTOs.Pedidos;
using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Api.Services;

public class PedidoService
{
    private readonly MinhaLojaDbContext _context;

    public PedidoService(MinhaLojaDbContext context)
    {
        _context = context;
    }

    public async Task<PedidoResponse> CriarAsync(
        int usuarioId,
        PedidoCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Itens is null
            || request.Itens.Count == 0
            || request.Itens.Count > 100)
        {
            throw new PedidoRegraException(
                "O pedido deve conter entre 1 e 100 linhas de itens.");
        }

        if (request.Observacao?.Length > 1000)
        {
            throw new PedidoRegraException(
                "A observação deve ter até 1000 caracteres.");
        }

        if (request.Itens.Any(i =>
            i is null
            || i.ProdutoId <= 0
            || i.Quantidade < 1
            || i.Quantidade > 1000))
        {
            throw new PedidoRegraException(
                "Informe produtos válidos e quantidades entre 1 e 1000.");
        }

        var itensAgrupados = request.Itens
            .GroupBy(i => i.ProdutoId)
            .Select(grupo => new
            {
                ProdutoId = grupo.Key,
                Quantidade = grupo.Sum(i => i.Quantidade)
            })
            .OrderBy(i => i.ProdutoId)
            .ToList();

        if (itensAgrupados.Any(i => i.Quantidade > 1000))
        {
            throw new PedidoRegraException(
                "O limite é de 1000 unidades por produto em cada pedido.");
        }

        await using var transacao = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var clienteValido = await _context.Usuarios.AnyAsync(
            u => u.Id == usuarioId
                && u.Ativo
                && u.Perfil == "Cliente",
            cancellationToken);

        if (!clienteValido)
        {
            throw new PedidoRegraException(
                "É necessário possuir uma conta de cliente ativa.");
        }

        var pedido = new Pedido
        {
            UsuarioId = usuarioId,
            CriadoEmUtc = DateTime.UtcNow,
            Status = PedidoStatus.Pendente,
            Observacao = request.Observacao?.Trim()
        };

        foreach (var item in itensAgrupados)
        {
            var produtos = await _context.Produtos
                .FromSqlInterpolated(
                    $"SELECT * FROM `Produtos` WHERE `Id` = {item.ProdutoId} FOR UPDATE")
                .AsTracking()
                .ToListAsync(cancellationToken);

            var produto = produtos.SingleOrDefault();

            if (produto is null || !produto.Ativo)
            {
                throw new PedidoRegraException(
                    $"O produto {item.ProdutoId} não está disponível.");
            }

            var categoriaAtiva = await _context.Categorias.AnyAsync(
                c => c.Id == produto.CategoriaId && c.Ativa,
                cancellationToken);

            if (!categoriaAtiva)
            {
                throw new PedidoRegraException(
                    $"A categoria do produto '{produto.Nome}' está inativa.");
            }

            if (produto.Preco <= 0)
            {
                throw new PedidoRegraException(
                    $"O produto '{produto.Nome}' está com preço inválido.");
            }

            if (produto.QuantidadeEstoque < item.Quantidade)
            {
                throw new PedidoRegraException(
                    $"Estoque insuficiente para '{produto.Nome}'.");
            }

            produto.QuantidadeEstoque -= item.Quantidade;

            pedido.Itens.Add(new PedidoItem
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                PrecoUnitario = produto.Preco,
                Quantidade = item.Quantidade
            });
        }

        pedido.Total = pedido.Itens.Sum(
            i => i.PrecoUnitario * i.Quantidade);

        _context.Pedidos.Add(pedido);

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return new PedidoResponse
        {
            Id = pedido.Id,
            CriadoEmUtc = pedido.CriadoEmUtc,
            Status = pedido.Status,
            Observacao = pedido.Observacao,
            Total = pedido.Total,
            Itens = pedido.Itens.Select(i => new PedidoItemResponse
            {
                ProdutoId = i.ProdutoId,
                NomeProduto = i.NomeProduto,
                PrecoUnitario = i.PrecoUnitario,
                Quantidade = i.Quantidade
            }).ToList()
        };
    }

    public async Task<bool> CancelarAsync(
    int pedidoId,
    int usuarioId,
    CancellationToken cancellationToken = default)
    {
        await using var transacao = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var clienteAtivo = await _context.Usuarios.AnyAsync(
            u => u.Id == usuarioId
                && u.Ativo
                && u.Perfil == "Cliente",
            cancellationToken);

        if (!clienteAtivo)
        {
            throw new PedidoRegraException(
                "É necessário possuir uma conta de cliente ativa.");
        }

        var pedidos = await _context.Pedidos
            .FromSqlInterpolated(
                $"""
            SELECT * FROM `Pedidos`
            WHERE `Id` = {pedidoId} AND `UsuarioId` = {usuarioId}
            FOR UPDATE
            """)
            .AsTracking()
            .ToListAsync(cancellationToken);

        var pedido = pedidos.SingleOrDefault();

        if (pedido is null)
        {
            return false;
        }

        if (pedido.Status != PedidoStatus.Pendente)
        {
            throw new PedidoRegraException(
                "Somente pedidos pendentes podem ser cancelados.");
        }

        var itens = await _context.PedidoItens
            .AsNoTracking()
            .Where(i => i.PedidoId == pedido.Id)
            .OrderBy(i => i.ProdutoId)
            .ToListAsync(cancellationToken);

        if (itens.Count == 0)
        {
            throw new PedidoRegraException(
                "O pedido está sem itens. O cancelamento precisa ser verificado.");
        }

        foreach (var item in itens)
        {
            var produtos = await _context.Produtos
                .FromSqlInterpolated(
                    $"SELECT * FROM `Produtos` WHERE `Id` = {item.ProdutoId} FOR UPDATE")
                .AsTracking()
                .ToListAsync(cancellationToken);

            var produto = produtos.SingleOrDefault();

            if (produto is null)
            {
                throw new PedidoRegraException(
                    "Um produto do pedido não foi encontrado.");
            }

            if (item.Quantidade <= 0
                || produto.QuantidadeEstoque < 0
                || produto.QuantidadeEstoque > int.MaxValue - item.Quantidade)
            {
                throw new PedidoRegraException(
                    "Não foi possível devolver o estoque deste pedido.");
            }

            produto.QuantidadeEstoque += item.Quantidade;
        }

        pedido.Status = PedidoStatus.Cancelado;

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return true;
    }
}