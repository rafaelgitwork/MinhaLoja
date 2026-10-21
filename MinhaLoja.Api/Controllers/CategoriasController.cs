using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var categorias = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Ativa)
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Ativa = c.Ativa
            })
            .ToListAsync();

        return Ok(categorias);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaResponse>> ObterPorId(int id)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Ativa = c.Ativa
            })
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
        var categoria = new Categoria
        {
            Nome = request.Nome.Trim(),
            Ativa = true
        };

        _context.Categorias.Add(categoria);

        await _context.SaveChangesAsync();

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
}