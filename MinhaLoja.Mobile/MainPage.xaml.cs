using MinhaLoja.Mobile.Services;
using MinhaLoja.Shared.DTOs.Produtos;

namespace MinhaLoja.Mobile;

public partial class MainPage : ContentPage
{
    private readonly ProdutoApiService _produtoService;
    private readonly CarrinhoService _carrinho;

    private bool _carregando;
    private bool _consultaInicialRealizada;

    public MainPage(
        ProdutoApiService produtoService,
        CarrinhoService carrinho)
    {
        InitializeComponent();

        _produtoService = produtoService;
        _carrinho = carrinho;
        SizeChanged += (_, _) => GradeProdutos.Span = Width >= 1000 ? 3 : Width >= 680 ? 2 : 1;
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
        ProdutosCollection.IsEnabled = false;
        CarregandoIndicator.IsVisible = true;
        CarregandoIndicator.IsRunning = true;
        ErroLabel.IsVisible = false;
        AvisoCatalogoLabel.IsVisible = false;

        VazioLabel.Text = "Carregando produtos...";

        try
        {
            var resultado = await _produtoService.ObterCatalogoAsync();

            ProdutosCollection.ItemsSource = resultado.Produtos;

            AvisoCatalogoLabel.Text = resultado.Aviso;
            AvisoCatalogoLabel.TextColor = resultado.UsandoCache
                ? Colors.DarkOrange
                : Colors.Green;

            AvisoCatalogoLabel.IsVisible = true;

            VazioLabel.Text = resultado.UsandoCache
                ? "O catálogo salvo está vazio."
                : "Nenhum produto disponível.";
        }
        catch (HttpRequestException)
        {
            MostrarErro(
                "Não foi possível consultar o catálogo. " +
                "Confira a API e tente novamente.");
        }
        catch (TaskCanceledException)
        {
            MostrarErro(
                "A consulta demorou demais. Tente atualizar novamente.");
        }
        catch (System.Text.Json.JsonException)
        {
            MostrarErro(
                "A API retornou dados em um formato inesperado.");
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
        }
        finally
        {
            _carregando = false;
            AtualizarButton.IsEnabled = true;
            ProdutosCollection.IsEnabled = true;
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

    private async void OnAdicionarClicked(object? sender, EventArgs e)
    {
        if (_carregando ||
            sender is not Button button ||
            button.CommandParameter is not ProdutoResponse produto)
        {
            return;
        }

        var erro = _carrinho.Adicionar(produto);

        if (erro is not null)
        {
            await DisplayAlertAsync("Carrinho", erro, "OK");
            return;
        }

        await DisplayAlertAsync(
            "Carrinho",
            $"{produto.Nome} foi adicionado.",
            "OK");
    }
}