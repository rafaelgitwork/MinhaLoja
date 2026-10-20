namespace MinhaLoja.Api.Entities
{
    public class Produto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public decimal Preco { get; set; }

        public int QuantidadeEstoque { get; set; }

        public bool Ativo { get; set; } = true;

        public int CategoriaId { get; set; }

        public Categoria Categoria { get; set; } = null!;
    }
}
