using System.ComponentModel.DataAnnotations;

namespace MinhaLoja.Shared.DTOs.Auth;

public class CadastroRequest
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma senha.")]
    [StringLength(128, MinimumLength = 15,
        ErrorMessage = "A senha deve ter entre 15 e 128 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}