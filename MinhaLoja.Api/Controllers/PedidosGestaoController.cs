using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinhaLoja.Api.Services;
using MinhaLoja.Shared.DTOs.Pedidos;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Authorize(Roles = "Funcionario,Administrador")]
[Route("api/gestao/pedidos")]
public class PedidosGestaoController : ControllerBase
{
    private readonly PedidoService _pedidoService;

    public PedidosGestaoController(PedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(
        int id,
        PedidoStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var operadorId))
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