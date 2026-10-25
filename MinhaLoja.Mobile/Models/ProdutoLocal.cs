using SQLite;

namespace MinhaLoja.Mobile.Models;

[Table("Produtos")]
public class ProdutoLocal
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Nome { get; set; } = "";

    public string? Descricao { get; set; }

    public long PrecoCentavos { get; set; }

    public int QuantidadeEstoque { get; set; }

    public int CategoriaId { get; set; }
}

[Table("CacheCatalogo")]
public class CacheCatalogoInfo
{
    [PrimaryKey]
    public int Id { get; set; }

    public long AtualizadoEmUnix { get; set; }
}