using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Pedidos;

public class PedidoCreateRequest
{
    [StringLength(1000,
        ErrorMessage = "A observação deve ter até 1000 caracteres.")]
    public string? Observacao { get; set; }

    [Required(ErrorMessage = "Informe os itens do pedido.")]
    [MinLength(1, ErrorMessage = "O pedido precisa ter pelo menos um item.")]
    [MaxLength(100, ErrorMessage = "O pedido pode ter até 100 linhas de itens.")]
    public List<PedidoItemCreateRequest> Itens { get; set; } = new();
}