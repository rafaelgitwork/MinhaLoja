using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Api.Entities;

public class Pedido
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;

    public PedidoStatus Status { get; set; } = PedidoStatus.Pendente;

    public string? Observacao { get; set; }

    public decimal Total { get; set; }

    public List<PedidoItem> Itens { get; set; } = new();
}