namespace MinhaLoja.Shared.DTOs.Pedidos;

public class PedidoItemResponse
{
    public int ProdutoId { get; set; }

    public string NomeProduto { get; set; } = string.Empty;

    public decimal PrecoUnitario { get; set; }

    public int Quantidade { get; set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;
}