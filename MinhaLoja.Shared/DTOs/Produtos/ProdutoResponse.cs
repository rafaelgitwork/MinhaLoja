using System;
using System.Collections.Generic;
using System.Text;

namespace MinhaLoja.Shared.DTOs.Produtos
{
    public class ProdutoResponse
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public int QuantidadeEstoque { get; set; }
    }
}