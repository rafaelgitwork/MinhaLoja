using System.ComponentModel.DataAnnotations;
using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Shared.DTOs.Pedidos;

public class PedidoStatusUpdateRequest
{
    [EnumDataType(typeof(PedidoStatus),
        ErrorMessage = "Informe um status atual válido.")]
    public PedidoStatus StatusAtual { get; set; }

    [EnumDataType(typeof(PedidoStatus),
        ErrorMessage = "Informe um novo status válido.")]
    public PedidoStatus NovoStatus { get; set; }
}