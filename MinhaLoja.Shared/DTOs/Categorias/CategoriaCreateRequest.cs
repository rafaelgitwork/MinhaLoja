using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Categorias;

public class CategoriaCreateRequest
{
    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}
