using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [StringLength(128)]
    public string Senha { get; set; } = string.Empty;
}