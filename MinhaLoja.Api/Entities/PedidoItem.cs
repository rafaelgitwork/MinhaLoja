namespace MinhaLoja.Api.Entities;

public class PedidoItem
{
    public int Id { get; set; }

    public int PedidoId { get; set; }

    public Pedido Pedido { get; set; } = null!;

    public int ProdutoId { get; set; }

    public Produto Produto { get; set; } = null!;

    public string NomeProduto { get; set; } = string.Empty;

    public decimal PrecoUnitario { get; set; }

    public int Quantidade { get; set; }
}