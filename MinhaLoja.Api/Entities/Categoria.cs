namespace MinhaLoja.Api.Entities
{
    public class Categoria
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public bool Ativa { get; set; } = true;

        public List<Produto> Produtos { get; set; } = new();
    }
}
