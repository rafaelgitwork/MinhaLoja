namespace MinhaLoja.Shared.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiraEm { get; set; }

    public UsuarioResponse Usuario { get; set; } = new();
}