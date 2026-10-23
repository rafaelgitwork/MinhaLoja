using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaLoja.Api.Data;
using MinhaLoja.Api.Services;
using MinhaLoja.Shared.DTOs.Pedidos;
using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Authorize(Roles = "Funcionario,Administrador")]
[Route("api/gestao/pedidos")]
public class PedidosGestaoController : ControllerBase
{
    private readonly PedidoService _pedidoService;
    private readonly MinhaLojaDbContext _context;

    public PedidosGestaoController(
        PedidoService pedidoService,
        MinhaLojaDbContext context)
    {
        _pedidoService = pedidoService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<PedidoGestaoResponse>>> Listar(
        CancellationToken cancellationToken,
        [FromQuery] PedidoStatus? status = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10)
    {
        if (!int.TryParse(
            User.FindFirst("sub")?.Value, out var operadorId))
        {
            return Unauthorized();
        }

        var operadorAutorizado = await _context.Usuarios.AnyAsync(
            u => u.Id == operadorId
                && u.Ativo
                && (u.Perfil == "Funcionario"
                    || u.Perfil == "Administrador"),
            cancellationToken);

        if (!operadorAutorizado)
        {
            return Forbid();
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

        if (status.HasValue
            && !Enum.IsDefined(typeof(PedidoStatus), status.Value))
        {
            ModelState.AddModelError(
                nameof(status),
                "Informe um status válido.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var consulta = _context.Pedidos.AsNoTracking();

        if (status.HasValue)
        {
            consulta = consulta.Where(p => p.Status == status.Value);
        }

        var pedidos = await consulta
            .OrderByDescending(p => p.CriadoEmUtc)
            .ThenByDescending(p => p.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(p => new PedidoGestaoResponse
            {
                Id = p.Id,
                ClienteNome = p.Usuario.Nome,
                CriadoEmUtc = p.CriadoEmUtc,
                Status = p.Status,
                Total = p.Total
            })
            .ToListAsync(cancellationToken);

        foreach (var pedido in pedidos)
        {
            pedido.CriadoEmUtc = DateTime.SpecifyKind(
                pedido.CriadoEmUtc, DateTimeKind.Utc);
        }

        return Ok(pedidos);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(
        int id,
        PedidoStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
            User.FindFirst("sub")?.Value, out var operadorId))
        {
            return Unauthorized();
        }

        try
        {
            var atualizado = await _pedidoService.AtualizarStatusAsync(
                id, operadorId, request, cancellationToken);

            if (!atualizado)
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
                Title = "Não foi possível alterar o status.",
                Detail = ex.Message
            });
        }
    }
}