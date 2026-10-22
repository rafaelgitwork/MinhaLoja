using System.Net;
using System.Net.Http.Json;
using MinhaLoja.Shared.DTOs.Auth;

namespace MinhaLoja.Web.Services;

public class AuthWebService
{
    private readonly HttpClient _httpClient;

    public AuthWebService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(LoginResponse? Login, string? Erro)> LoginAsync(
        LoginRequest request)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login", request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (null, "E-mail ou senha inválidos.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (null, "Confira o e-mail e a senha informados.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (
                    null,
                    "Não foi possível entrar. Tente novamente mais tarde.");
            }

            var login = await response.Content
                .ReadFromJsonAsync<LoginResponse>();

            if (login is null || string.IsNullOrWhiteSpace(login.Token))
            {
                return (null, "A API retornou uma resposta de login inválida.");
            }

            if (login.ExpiraEm <= DateTimeOffset.UtcNow)
            {
                return (null, "O token recebido já expirou.");
            }

            if (login.Usuario.Perfil != "Funcionario"
                && login.Usuario.Perfil != "Administrador")
            {
                return (
                    null,
                    "Este painel é destinado a funcionários e administradores.");
            }

            return (login, null);
        }
        catch (HttpRequestException)
        {
            return (
                null,
                "Não foi possível comunicar com a API.");
        }
        catch (TaskCanceledException)
        {
            return (
                null,
                "O tempo de espera terminou. Tente novamente.");
        }
        catch (System.Text.Json.JsonException)
        {
            return (
                null,
                "Não foi possível interpretar a resposta da API.");
        }
    }
}