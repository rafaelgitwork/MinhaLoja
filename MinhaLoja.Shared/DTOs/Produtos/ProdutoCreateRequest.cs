using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Produtos;

public class ProdutoCreateRequest
{
    [Required(ErrorMessage = "Informe o nome do produto.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descricao { get; set; }

    [Range(typeof(decimal), "0.01", "99999999",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "O preço deve estar entre 0,01 e 99.999.999.")]
    public decimal Preco { get; set; }

    [Range(0, int.MaxValue,
        ErrorMessage = "O estoque não pode ser negativo.")]
    public int QuantidadeEstoque { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Informe uma categoria válida.")]
    public int CategoriaId { get; set; }
}