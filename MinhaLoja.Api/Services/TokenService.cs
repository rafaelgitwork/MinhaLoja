using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MinhaLoja.Api.Entities;
using MinhaLoja.Shared.DTOs.Auth;

namespace MinhaLoja.Api.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponse Gerar(Usuario usuario)
    {
        var chave = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada.");

        var emissor = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer não configurado.");

        var publico = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience não configurado.");

        if (!int.TryParse(
                _configuration["Jwt:ExpirationMinutes"], out var minutos)
            || minutos <= 0)
        {
            throw new InvalidOperationException(
                "Jwt:ExpirationMinutes deve ser um inteiro positivo.");
        }

        var agora = DateTimeOffset.UtcNow;
        var expiraEm = agora.AddMinutes(minutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("name", usuario.Nome),
            new Claim("role", usuario.Perfil)
        };

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: publico,
            claims: claims,
            notBefore: agora.UtcDateTime,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credenciais);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiraEm = expiraEm,
            Usuario = new UsuarioResponse
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Perfil = usuario.Perfil
            }
        };
    }
}