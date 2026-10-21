namespace MinhaLoja.Shared.DTOs.Produtos;

public class ProdutoResponse
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public decimal Preco { get; set; }

    public int QuantidadeEstoque { get; set; }

    public int CategoriaId { get; set; }
}