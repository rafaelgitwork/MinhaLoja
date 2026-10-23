using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaLoja.Api.Data;
using MinhaLoja.Api.Services;
using MinhaLoja.Shared.DTOs.Pedidos;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Authorize(Roles = "Cliente")]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidoService;
    private readonly MinhaLojaDbContext _context;

    public PedidosController(
        PedidoService pedidoService,
        MinhaLojaDbContext context)
    {
        _pedidoService = pedidoService;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<PedidoResponse>> Criar(
        PedidoCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            var pedido = await _pedidoService.CriarAsync(
                usuarioId, request, cancellationToken);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = pedido.Id },
                pedido);
        }
        catch (PedidoRegraException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Não foi possível criar o pedido.",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoResponse>> ObterPorId(
        int id,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
        {
            return Unauthorized();
        }

        var clienteAtivo = await _context.Usuarios.AnyAsync(
            u => u.Id == usuarioId && u.Ativo && u.Perfil == "Cliente",
            cancellationToken);

        if (!clienteAtivo)
        {
            return Forbid();
        }

        var pedido = await _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(
                p => p.Id == id && p.UsuarioId == usuarioId,
                cancellationToken);

        if (pedido is null)
        {
            return NotFound();
        }

        return Ok(new PedidoResponse
        {
            Id = pedido.Id,
            CriadoEmUtc = DateTime.SpecifyKind(
                           pedido.CriadoEmUtc, DateTimeKind.Utc),
            Status = pedido.Status,
            Observacao = pedido.Observacao,
            Total = pedido.Total,
            Itens = pedido.Itens.Select(i => new PedidoItemResponse
            {
                ProdutoId = i.ProdutoId,
                NomeProduto = i.NomeProduto,
                PrecoUnitario = i.PrecoUnitario,
                Quantidade = i.Quantidade
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<PedidoResponse>>> ListarMeusPedidos(
    CancellationToken cancellationToken,
    [FromQuery] int pagina = 1,
    [FromQuery] int tamanhoPagina = 10)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
        {
            return Unauthorized();
        }

        if (pagina < 1 || pagina > 1000000)
        {
            ModelState.AddModelError(
                nameof(pagina),
                "A página deve estar entre 1 e 1000000.");
        }

        if (tamanhoPagina < 1 || tamanhoPagina > 50)
        {
            ModelState.AddModelError(
                nameof(tamanhoPagina),
                "O tamanho da página deve estar entre 1 e 50.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var clienteAtivo = await _context.Usuarios.AnyAsync(
            u => u.Id == usuarioId && u.Ativo && u.Perfil == "Cliente",
            cancellationToken);

        if (!clienteAtivo)
        {
            return Forbid();
        }

        var pedidos = await _context.Pedidos
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEmUtc)
            .ThenByDescending(p => p.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Include(p => p.Itens)
            .ToListAsync(cancellationToken);

        var response = pedidos.Select(p => new PedidoResponse
        {
            Id = p.Id,
            CriadoEmUtc = DateTime.SpecifyKind(
                p.CriadoEmUtc, DateTimeKind.Utc),
            Status = p.Status,
            Observacao = p.Observacao,
            Total = p.Total,
            Itens = p.Itens
                .OrderBy(i => i.Id)
                .Select(i => new PedidoItemResponse
                {
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.NomeProduto,
                    PrecoUnitario = i.PrecoUnitario,
                    Quantidade = i.Quantidade
                })
                .ToList()
        }).ToList();

        return Ok(response);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(
    int id,
    CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            var cancelado = await _pedidoService.CancelarAsync(
                id, usuarioId, cancellationToken);

            if (!cancelado)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (PedidoRegraException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Não foi possível cancelar o pedido.",
                Detail = ex.Message
            });
        }
    }
}