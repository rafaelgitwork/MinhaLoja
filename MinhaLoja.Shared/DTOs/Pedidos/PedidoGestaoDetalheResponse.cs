using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Shared.DTOs.Pedidos;

public class PedidoGestaoDetalheResponse
{
    public int Id { get; set; }

    public string ClienteNome { get; set; } = "";

    public DateTime CriadoEmUtc { get; set; }

    public PedidoStatus Status { get; set; }

    public string? Observacao { get; set; }

    public decimal Total { get; set; }

    public List<PedidoItemResponse> Itens { get; set; } = new();
}