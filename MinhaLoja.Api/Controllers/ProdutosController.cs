using MinhaLoja.Api.Entities;
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

    [HttpPost]
    public async Task<ActionResult<ProdutoResponse>> Criar(
    ProdutoCreateRequest request)
    {
        if (decimal.Round(request.Preco, 2) != request.Preco)
        {
            ModelState.AddModelError(
                nameof(request.Preco),
                "O preço deve ter no máximo duas casas decimais.");

            return ValidationProblem(ModelState);
        }

        var categoriaValida = await _context.Categorias
            .AnyAsync(c => c.Id == request.CategoriaId && c.Ativa);

        if (!categoriaValida)
        {
            ModelState.AddModelError(
                nameof(request.CategoriaId),
                "A categoria não existe ou está inativa.");

            return ValidationProblem(ModelState);
        }

        var produto = new Produto
        {
            Nome = request.Nome.Trim(),
            Descricao = request.Descricao?.Trim(),
            Preco = request.Preco,
            QuantidadeEstoque = request.QuantidadeEstoque,
            CategoriaId = request.CategoriaId,
            Ativo = true
        };

        _context.Produtos.Add(produto);

        await _context.SaveChangesAsync();

        var response = new ProdutoResponse
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Preco = produto.Preco,
            QuantidadeEstoque = produto.QuantidadeEstoque
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

}