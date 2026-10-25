using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using MinhaLoja.Shared.DTOs.Auth;

namespace MinhaLoja.Mobile.Services;

public class AuthApiService
{
    private const string ChaveSessao = "minhaloja.sessao";

    private readonly HttpClient _httpClient;
    private LoginResponse? _sessao;

    public UsuarioResponse? Usuario => _sessao?.Usuario;

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task EntrarAsync(string email, string senha)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequest
            {
                Email = email.Trim(),
                Senha = senha
            });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "E-mail ou senha inválidos.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                "Confira o e-mail e a senha informados.");
        }

        response.EnsureSuccessStatusCode();

        var sessao = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        if (sessao is null ||
            string.IsNullOrWhiteSpace(sessao.Token) ||
            sessao.Usuario is null ||
            sessao.ExpiraEm <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException(
                "A API retornou uma sessão inválida.");
        }

        if (sessao.Usuario.Perfil != "Cliente")
        {
            throw new InvalidOperationException(
                "Entre com uma conta de cliente. " +
                "Funcionários e administradores utilizam o painel Web.");
        }

        try
        {
            await SecureStorage.Default.SetAsync(
                ChaveSessao,
                JsonSerializer.Serialize(sessao));
        }
        catch (Exception)
        {
            throw new InvalidOperationException(
                "Não foi possível salvar a sessão com segurança neste dispositivo.");
        }

        _sessao = sessao;
    }

    public async Task RestaurarAsync()
    {
        if (_sessao is null)
        {
            string? dados;

            try
            {
                dados = await SecureStorage.Default.GetAsync(ChaveSessao);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                    "Não foi possível ler a sessão salva. Tente entrar novamente.");
            }

            if (string.IsNullOrWhiteSpace(dados))
                return;

            try
            {
                _sessao = JsonSerializer.Deserialize<LoginResponse>(dados);
            }
            catch (JsonException)
            {
                Sair();

                throw new InvalidOperationException(
                    "A sessão salva estava inválida. Entre novamente.");
            }
        }

        if (_sessao is null ||
            string.IsNullOrWhiteSpace(_sessao.Token) ||
            _sessao.ExpiraEm <= DateTimeOffset.UtcNow)
        {
            Sair();
            return;
        }

        // Confirma na API se a conta continua válida e ativa.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _sessao.Token);

        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode is HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden)
        {
            Sair();
            return;
        }

        response.EnsureSuccessStatusCode();

        var usuario = await response.Content
            .ReadFromJsonAsync<UsuarioResponse>();

        if (usuario is null || usuario.Perfil != "Cliente")
        {
            Sair();

            throw new InvalidOperationException(
                "Esta sessão não pertence a uma conta de cliente.");
        }

        _sessao.Usuario = usuario;
    }

    public string? ObterToken()
    {
        if (_sessao is null ||
            _sessao.ExpiraEm <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return _sessao.Token;
    }

    public void Sair()
    {
        _sessao = null;

        try
        {
            SecureStorage.Default.Remove(ChaveSessao);
        }
        catch (Exception)
        {
            throw new InvalidOperationException(
                "A sessão em memória foi encerrada, mas não foi possível " +
                "remover a sessão salva. Tente sair novamente.");
        }
    }

    public async Task CadastrarAsync(
    string nome,
    string email,
    string senha)
    {
        var dados = new CadastroRequest
        {
            Nome = nome.Trim(),
            Email = email.Trim(),
            Senha = senha
        };

        var resultados = new List<
            System.ComponentModel.DataAnnotations.ValidationResult>();

        var valido = System.ComponentModel.DataAnnotations.Validator
            .TryValidateObject(
                dados,
                new System.ComponentModel.DataAnnotations.ValidationContext(dados),
                resultados,
                validateAllProperties: true);

        if (!valido)
        {
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    resultados.Select(r => r.ErrorMessage)));
        }

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/cadastro",
            dados);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                "Já existe uma conta com esse e-mail. Use a opção Entrar.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var mensagem = "Confira os dados informados.";

            try
            {
                var problema = await response.Content
                    .ReadFromJsonAsync<ErroValidacaoCadastro>();

                if (problema?.Errors is { Count: > 0 })
                {
                    mensagem = string.Join(
                        Environment.NewLine,
                        problema.Errors.Values
                            .SelectMany(erros => erros)
                            .Distinct());
                }
            }
            catch (JsonException)
            {
                // Mantém a mensagem padrão se a resposta não for JSON válido.
            }

            throw new InvalidOperationException(mensagem);
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed class ErroValidacaoCadastro
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}