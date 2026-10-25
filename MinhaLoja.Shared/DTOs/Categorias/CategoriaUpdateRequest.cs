using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Categorias;

public class CategoriaUpdateRequest
{
    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe se a categoria está ativa.")]
    public bool? Ativa { get; set; }
}