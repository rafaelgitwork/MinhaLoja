using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Shared.DTOs.Pedidos;

public class PedidoGestaoResponse
{
    public int Id { get; set; }

    public string ClienteNome { get; set; } = string.Empty;

    public DateTime CriadoEmUtc { get; set; }

    public PedidoStatus Status { get; set; }

    public decimal Total { get; set; }
}