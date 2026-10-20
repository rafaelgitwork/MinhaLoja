using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaLoja.Api.Data;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Route("api/produtos")]

public class ProdutosController : ControllerBase
{
    private readonly MinhaLojaDbContext _context;

    public ProdutosController(MinhaLojaDbContext context)
    {
        _context = context;
    }

    [HttpGet]

    public async Task<ActionResult<List<ProdutoResponse>>> Listar()
    {
        var produtos = await _context.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo && p.Categoria.Ativa)
            .OrderBy(p => p.Nome)
            .Select(p => new ProdutoResponse
            {
                Id = p.Id,
                Nome = p.Nome,
                Preco = p.Preco,
                QuantidadeEstoque = p.QuantidadeEstoque
            })
            .ToListAsync();

        return Ok(produtos);
    }
}