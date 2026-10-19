using Microsoft.AspNetCore.Mvc;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<ProdutoResponse>> Listar()
    {
        var produtos = new List<ProdutoResponse>
        {
            new ProdutoResponse
            {
                Id = 1,
                Nome = "Detergente neutro 500 ml",
                Preco = 3.50m,
                QuantidadeEstoque = 20
            }
        };

        return Ok(produtos);
    }
}