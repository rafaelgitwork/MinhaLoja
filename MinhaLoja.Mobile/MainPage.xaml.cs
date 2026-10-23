using MinhaLoja.Mobile.Services;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Mobile;

public partial class MainPage : ContentPage
{
    private readonly ProdutoApiService _produtoService;
    private bool _carregando;
    private bool _consultaInicialRealizada;

    public MainPage(ProdutoApiService produtoService)
    {
        InitializeComponent();
        _produtoService = produtoService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_consultaInicialRealizada)
            return;

        _consultaInicialRealizada = true;

        await CarregarProdutosAsync();
    }

    private async void OnAtualizarClicked(object? sender, EventArgs e)
    {
        await CarregarProdutosAsync();
    }

    private async Task CarregarProdutosAsync()
    {
        if (_carregando)
            return;

        _carregando = true;
        AtualizarButton.IsEnabled = false;
        CarregandoIndicator.IsVisible = true;
        CarregandoIndicator.IsRunning = true;
        ErroLabel.IsVisible = false;
        VazioLabel.Text = "Carregando produtos...";

        try
        {
            var produtos = await _produtoService.ListarAsync();

            ProdutosCollection.ItemsSource = produtos;
            VazioLabel.Text = "Nenhum produto disponível.";
        }
        catch (HttpRequestException)
        {
            MostrarErro(
                "Não foi possível carregar os produtos. " +
                "Confira se a API e o MySQL estão rodando.");
        }
        catch (TaskCanceledException)
        {
            MostrarErro(
                "A consulta demorou demais. Toque em Atualizar para tentar novamente.");
        }
        catch (System.Text.Json.JsonException)
        {
            MostrarErro(
                "A API retornou dados em um formato inesperado.");
        }
        finally
        {
            _carregando = false;
            AtualizarButton.IsEnabled = true;
            CarregandoIndicator.IsRunning = false;
            CarregandoIndicator.IsVisible = false;
        }
    }

    private void MostrarErro(string mensagem)
    {
        ProdutosCollection.ItemsSource = Array.Empty<ProdutoResponse>();
        VazioLabel.Text = "Consulta indisponível.";
        ErroLabel.Text = mensagem;
        ErroLabel.IsVisible = true;
    }
}