using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaLoja.Api.Data;
using MinhaLoja.Api.Entities;
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
                Descricao = p.Descricao,
                Preco = p.Preco,
                QuantidadeEstoque = p.QuantidadeEstoque,
                CategoriaId = p.CategoriaId
            })
            .ToListAsync();

        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> ObterPorId(int id)
    {
        var produto = await _context.Produtos
            .AsNoTracking()
            .Where(p => p.Id == id && p.Ativo && p.Categoria.Ativa)
            .Select(p => new ProdutoResponse
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                Preco = p.Preco,
                QuantidadeEstoque = p.QuantidadeEstoque,
                CategoriaId = p.CategoriaId
            })
            .FirstOrDefaultAsync();

        if (produto is null)
        {
            return NotFound();
        }

        return Ok(produto);
    }

    [Authorize(Roles = "Funcionario,Administrador")]
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
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            QuantidadeEstoque = produto.QuantidadeEstoque,
            CategoriaId = produto.CategoriaId
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Roles = "Funcionario,Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        ProdutoUpdateRequest request)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

        if (produto is null)
        {
            return NotFound();
        }

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

        produto.Nome = request.Nome.Trim();
        produto.Descricao = request.Descricao?.Trim();
        produto.Preco = request.Preco;
        produto.CategoriaId = request.CategoriaId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Funcionario,Administrador")]
    [HttpPost("{id:int}/estoque/reposicoes")]
    public async Task<IActionResult> ReporEstoque(
    int id,
    ProdutoReposicaoRequest request,
    CancellationToken cancellationToken)
    {
        await using var transacao = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        // Aguarda outras operações sobre este produto e lê o saldo atual.
        var produtos = await _context.Produtos
            .FromSqlInterpolated(
                $"SELECT * FROM `Produtos` WHERE `Id` = {id} FOR UPDATE")
            .AsTracking()
            .ToListAsync(cancellationToken);

        var produto = produtos.SingleOrDefault();

        if (produto is null || !produto.Ativo)
        {
            return NotFound();
        }

        if (produto.QuantidadeEstoque < 0 ||
            produto.QuantidadeEstoque > int.MaxValue - request.Quantidade)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível repor o estoque.",
                detail: "O saldo atual não permite adicionar essa quantidade.");
        }

        produto.QuantidadeEstoque += request.Quantidade;

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return NoContent();
    }
}