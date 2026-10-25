using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MinhaLoja.Api.Data;
using MinhaLoja.Api.Entities;
using MinhaLoja.Shared.DTOs.Categorias;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly MinhaLojaDbContext _context;

    public CategoriasController(MinhaLojaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoriaResponse>>> Listar()
    {
        var categorias = await ProjetarCategorias(
            _context.Categorias.Where(c => c.Ativa))
            .ToListAsync();

        return Ok(categorias);
    }

    [Authorize(Roles = "Funcionario,Administrador")]
    [HttpGet("gestao")]
    public async Task<ActionResult<List<CategoriaResponse>>> ListarGestao()
    {
        var categorias = await ProjetarCategorias(
            _context.Categorias)
            .ToListAsync();

        return Ok(categorias);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaResponse>> ObterPorId(int id)
    {
        var categoria = await ProjetarCategorias(
            _context.Categorias.Where(c => c.Id == id))
            .FirstOrDefaultAsync();

        if (categoria is null)
        {
            return NotFound();
        }

        return Ok(categoria);
    }

    [Authorize(Roles = "Funcionario,Administrador")]
    [HttpPost]
    public async Task<ActionResult<CategoriaResponse>> Criar(
        CategoriaCreateRequest request)
    {
        var nome = request.Nome?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nome))
        {
            ModelState.AddModelError(
                nameof(request.Nome),
                "Informe o nome da categoria.");

            return ValidationProblem(ModelState);
        }

        var nomeExistente = await _context.Categorias
            .AnyAsync(c => c.Nome == nome);

        if (nomeExistente)
        {
            return ConflitoNome();
        }

        var categoria = new Categoria
        {
            Nome = nome,
            Ativa = true
        };

        _context.Categorias.Add(categoria);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EhNomeDuplicado(ex))
        {
            return ConflitoNome();
        }

        var response = new CategoriaResponse
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Ativa = categoria.Ativa
        };

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = categoria.Id },
            response);
    }

    [Authorize(Roles = "Funcionario,Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        CategoriaUpdateRequest request)
    {
        var categoria = await _context.Categorias
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null)
        {
            return NotFound();
        }

        var nome = request.Nome?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nome))
        {
            ModelState.AddModelError(
                nameof(request.Nome),
                "Informe o nome da categoria.");
        }

        if (!request.Ativa.HasValue)
        {
            ModelState.AddModelError(
                nameof(request.Ativa),
                "Informe se a categoria está ativa.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var nomeExistente = await _context.Categorias
            .AnyAsync(c => c.Id != id && c.Nome == nome);

        if (nomeExistente)
        {
            return ConflitoNome();
        }

        categoria.Nome = nome;
        categoria.Ativa = request.Ativa.GetValueOrDefault();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EhNomeDuplicado(ex))
        {
            return ConflitoNome();
        }

        return NoContent();
    }

    private static IQueryable<CategoriaResponse> ProjetarCategorias(
        IQueryable<Categoria> query)
    {
        return query
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Ativa = c.Ativa
            });
    }

    private ObjectResult ConflitoNome()
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Nome de categoria já utilizado.",
            detail:
                "Já existe uma categoria com esse nome, ativa ou inativa. " +
                "Edite ou reative a existente.");
    }

    private static bool EhNomeDuplicado(DbUpdateException exception)
    {
        return exception.InnerException is MySqlException
        {
            Number: 1062
        };
    }
}