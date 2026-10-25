using MinhaLoja.Mobile.Services;

namespace MinhaLoja.Mobile.Pages;

public partial class ContaPage : ContentPage
{
    private readonly AuthApiService _authService;

    private bool _ocupado;
    private bool _exibindoCadastro;

    public ContaPage(AuthApiService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await ExecutarAsync(async () =>
        {
            await _authService.RestaurarAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        SenhaEntry.Text = "";
        LimparSenhasCadastro();
    }

    private void OnAbrirCadastroClicked(object? sender, EventArgs e)
    {
        if (_ocupado)
            return;

        _exibindoCadastro = true;
        MensagemLabel.IsVisible = false;
        SenhaEntry.Text = "";

        CadastroEmailEntry.Text = EmailEntry.Text;

        AtualizarTela();
    }

    private void OnVoltarLoginClicked(object? sender, EventArgs e)
    {
        if (_ocupado)
            return;

        _exibindoCadastro = false;
        MensagemLabel.IsVisible = false;
        LimparSenhasCadastro();

        AtualizarTela();
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        if (_ocupado)
            return;

        var email = EmailEntry.Text?.Trim() ?? "";
        var senha = SenhaEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            MostrarMensagem("Preencha o e-mail e a senha.", true);
            return;
        }

        await ExecutarAsync(async () =>
        {
            try
            {
                await _authService.EntrarAsync(email, senha);

                _exibindoCadastro = false;
                MostrarMensagem("Login realizado com sucesso.", false);
            }
            finally
            {
                SenhaEntry.Text = "";
            }
        });
    }

    private async void OnCadastrarClicked(object? sender, EventArgs e)
    {
        if (_ocupado)
            return;

        var nome = CadastroNomeEntry.Text?.Trim() ?? "";
        var email = CadastroEmailEntry.Text?.Trim() ?? "";
        var senha = CadastroSenhaEntry.Text ?? "";
        var confirmacao = ConfirmacaoSenhaEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            MostrarMensagem("Preencha nome, e-mail e senha.", true);
            return;
        }

        if (senha != confirmacao)
        {
            MostrarMensagem("A confirmação da senha não confere.", true);
            return;
        }

        await ExecutarAsync(async () =>
        {
            try
            {
                await _authService.CadastrarAsync(nome, email, senha);
            }
            catch (HttpRequestException)
            {
                MostrarFalhaCadastro();
                return;
            }
            catch (TaskCanceledException)
            {
                MostrarFalhaCadastro();
                return;
            }
            finally
            {
                LimparSenhasCadastro();
            }

            EmailEntry.Text = email;
            CadastroNomeEntry.Text = "";
            CadastroEmailEntry.Text = "";
            _exibindoCadastro = false;

            MostrarMensagem(
                "Conta criada com sucesso! Digite sua senha para entrar.",
                false);
        });
    }

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        await ExecutarAsync(() =>
        {
            _authService.Sair();

            _exibindoCadastro = false;
            SenhaEntry.Text = "";
            LimparSenhasCadastro();

            MostrarMensagem("Você saiu da conta.", false);

            return Task.CompletedTask;
        });
    }

    private async Task ExecutarAsync(Func<Task> acao)
    {
        if (_ocupado)
            return;

        _ocupado = true;
        MensagemLabel.IsVisible = false;
        CarregandoIndicator.IsVisible = true;
        CarregandoIndicator.IsRunning = true;

        LoginLayout.IsEnabled = false;
        CadastroLayout.IsEnabled = false;
        SessaoLayout.IsEnabled = false;

        try
        {
            await acao();
        }
        catch (HttpRequestException)
        {
            MostrarMensagem(
                "Não foi possível comunicar com a API. " +
                "Confira se a API e o MySQL estão rodando.",
                true);
        }
        catch (TaskCanceledException)
        {
            MostrarMensagem(
                "A operação demorou demais. Tente novamente.",
                true);
        }
        catch (System.Text.Json.JsonException)
        {
            MostrarMensagem(
                "A API retornou dados em um formato inesperado.",
                true);
        }
        catch (InvalidOperationException ex)
        {
            MostrarMensagem(ex.Message, true);
        }
        finally
        {
            AtualizarTela();

            _ocupado = false;
            LoginLayout.IsEnabled = true;
            CadastroLayout.IsEnabled = true;
            SessaoLayout.IsEnabled = true;

            CarregandoIndicator.IsRunning = false;
            CarregandoIndicator.IsVisible = false;
        }
    }

    private void AtualizarTela()
    {
        var usuario = _authService.Usuario;
        var conectado = usuario is not null;

        LoginLayout.IsVisible = !conectado && !_exibindoCadastro;
        CadastroLayout.IsVisible = !conectado && _exibindoCadastro;
        SessaoLayout.IsVisible = conectado;

        NomeLabel.Text = usuario?.Nome ?? "";
        EmailUsuarioLabel.Text = usuario?.Email ?? "";
    }

    private void LimparSenhasCadastro()
    {
        CadastroSenhaEntry.Text = "";
        ConfirmacaoSenhaEntry.Text = "";
    }

    private void MostrarFalhaCadastro()
    {
        MostrarMensagem(
            "Não foi possível confirmar o cadastro. " +
            "Quando a conexão voltar, tente entrar com esse e-mail e senha. " +
            "A conta pode ter sido criada antes da falha na resposta.",
            true);
    }

    private void MostrarMensagem(string texto, bool erro)
    {
        MensagemLabel.Text = texto;
        MensagemLabel.TextColor = erro ? Colors.Red : Colors.Green;
        MensagemLabel.IsVisible = true;
    }
}