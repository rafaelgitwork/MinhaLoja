using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Produtos;

public class ProdutoReposicaoRequest
{
    [Range(1, 1000000,
        ErrorMessage = "Informe uma quantidade entre 1 e 1.000.000.")]
    public int Quantidade { get; set; }
}