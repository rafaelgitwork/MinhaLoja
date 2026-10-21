using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MinhaLoja.Api.Data;
using MinhaLoja.Api.Entities;
using MinhaLoja.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using MinhaLoja.Api.Services;

namespace MinhaLoja.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MinhaLojaDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly TokenService _tokenService;

    public AuthController(
    MinhaLojaDbContext context,
    IPasswordHasher<Usuario> passwordHasher,
    TokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [HttpPost("cadastro")]
    public async Task<ActionResult<UsuarioResponse>> Cadastrar(
        CadastroRequest request)
    {
        var email = request.Email.Trim();
        var emailNormalizado = email.ToUpperInvariant();

        var emailEmUso = await _context.Usuarios
            .AnyAsync(u => u.EmailNormalizado == emailNormalizado);

        if (emailEmUso)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Este e-mail já está cadastrado."
            });
        }

        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = email,
            EmailNormalizado = emailNormalizado,
            Perfil = "Cliente",
            Ativo = true
        };

        usuario.SenhaHash = _passwordHasher.HashPassword(
            usuario, request.Senha);

        _context.Usuarios.Add(usuario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1062 })
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Este e-mail já está cadastrado."
            });
        }

        var response = new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var emailNormalizado = request.Email.Trim().ToUpperInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.EmailNormalizado == emailNormalizado);

        if (usuario is null || !usuario.Ativo)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "E-mail ou senha inválidos."
            });
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario, usuario.SenhaHash, request.Senha);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "E-mail ou senha inválidos."
            });
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(
                usuario, request.Senha);

            await _context.SaveChangesAsync();
        }

        return Ok(_tokenService.Gerar(usuario));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioResponse>> Me()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
        {
            return Unauthorized();
        }

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Ativo);

        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil
        });
    }
}