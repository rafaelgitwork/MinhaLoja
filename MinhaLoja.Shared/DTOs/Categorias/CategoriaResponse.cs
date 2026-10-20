using System;
using System.Collections.Generic;
using System.Text;

namespace MinhaLoja.Shared.DTOs.Categorias;

public class CategoriaResponse
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public bool Ativa { get; set; }
}