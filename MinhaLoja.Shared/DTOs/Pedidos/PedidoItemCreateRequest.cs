using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Pedidos;

public class PedidoItemCreateRequest
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Informe um produto válido.")]
    public int ProdutoId { get; set; }

    [Range(1, 1000,
        ErrorMessage = "A quantidade deve estar entre 1 e 1000.")]
    public int Quantidade { get; set; }
}