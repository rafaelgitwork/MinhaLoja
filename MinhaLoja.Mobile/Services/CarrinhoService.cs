using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Mobile.Services;

public class CarrinhoItem
{
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = "";
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
    public int EstoqueConsultado { get; set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;
}

public class CarrinhoService
{
    private readonly List<CarrinhoItem> _itens = new();

    public IReadOnlyList<CarrinhoItem> Itens => _itens.AsReadOnly();

    public decimal Total => _itens.Sum(i => i.Subtotal);

    public bool EmEnvio { get; set; }

    public string? Adicionar(ProdutoResponse produto)
    {
        if (EmEnvio)
            return "Aguarde o envio do pedido.";

        var item = _itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
        var novaQuantidade = (item?.Quantidade ?? 0) + 1;

        if (novaQuantidade > Math.Min(produto.QuantidadeEstoque, 1000))
            return "Quantidade indisponível no estoque consultado.";

        if (item is null)
        {
            if (_itens.Count >= 100)
                return "O carrinho aceita até 100 produtos diferentes.";

            _itens.Add(new CarrinhoItem
            {
                ProdutoId = produto.Id,
                Nome = produto.Nome,
                PrecoUnitario = produto.Preco,
                Quantidade = 1,
                EstoqueConsultado = produto.QuantidadeEstoque
            });
        }
        else
        {
            item.Quantidade = novaQuantidade;
            item.PrecoUnitario = produto.Preco;
            item.EstoqueConsultado = produto.QuantidadeEstoque;
        }

        return null;
    }

    public string? AlterarQuantidade(int produtoId, int quantidade)
    {
        if (EmEnvio)
            return "Aguarde o envio do pedido.";

        var item = _itens.FirstOrDefault(i => i.ProdutoId == produtoId);

        if (item is null)
            return "Item não encontrado.";

        if (quantidade < 1 ||
            quantidade > Math.Min(item.EstoqueConsultado, 1000))
        {
            return "Quantidade indisponível no estoque consultado.";
        }

        item.Quantidade = quantidade;
        return null;
    }

    public void Remover(int produtoId)
    {
        if (!EmEnvio)
            _itens.RemoveAll(i => i.ProdutoId == produtoId);
    }

    public void LimparAposCompra()
    {
        _itens.Clear();
    }
}