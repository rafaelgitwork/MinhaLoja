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
}