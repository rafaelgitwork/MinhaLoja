using MinhaLoja.Mobile.Services;

namespace MinhaLoja.Mobile.Pages;

public partial class ContaPage : ContentPage
{
    private readonly AuthApiService _authService;
    private bool _ocupado;

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
                MostrarMensagem("Login realizado com sucesso.", false);
            }
            finally
            {
                SenhaEntry.Text = "";
            }
        });
    }

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        await ExecutarAsync(() =>
        {
            _authService.Sair();

            SenhaEntry.Text = "";
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
            SessaoLayout.IsEnabled = true;
            CarregandoIndicator.IsRunning = false;
            CarregandoIndicator.IsVisible = false;
        }
    }

    private void AtualizarTela()
    {
        var usuario = _authService.Usuario;

        LoginLayout.IsVisible = usuario is null;
        SessaoLayout.IsVisible = usuario is not null;

        NomeLabel.Text = usuario?.Nome ?? "";
        EmailUsuarioLabel.Text = usuario?.Email ?? "";
    }

    private void MostrarMensagem(string texto, bool erro)
    {
        MensagemLabel.Text = texto;
        MensagemLabel.TextColor = erro
            ? Colors.Red
            : Colors.Green;

        MensagemLabel.IsVisible = true;
    }
}