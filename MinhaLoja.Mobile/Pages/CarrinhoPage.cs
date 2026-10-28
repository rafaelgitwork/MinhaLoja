using MinhaLoja.Mobile.Visual;
using System.Globalization;
using MinhaLoja.Mobile.Services;

namespace MinhaLoja.Mobile.Pages;

public class CarrinhoPage : ContentPage
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("pt-BR");

    private readonly CarrinhoService _carrinho;
    private readonly PedidoApiService _pedidoService;
    private readonly AuthApiService _authService;

    private readonly VerticalStackLayout _lista = new()
    {
        Spacing = 12
    };

    private readonly Label _total = new()
    {
        FontSize = 22,
        FontAttributes = FontAttributes.Bold
    };

    private readonly Label _mensagem = new()
    {
        IsVisible = false
    };

    private readonly Editor _observacao = new()
    {
        Placeholder = "Observação do pedido (opcional)",
        MaxLength = 1000,
        HeightRequest = 90
    };

    private readonly Button _finalizar = new()
    {
        Text = "Finalizar pedido"
    };

    private bool _resultadoIncerto;

    public CarrinhoPage(
        CarrinhoService carrinho,
        PedidoApiService pedidoService,
        AuthApiService authService)
    {
        _carrinho = carrinho;
        _pedidoService = pedidoService;
        _authService = authService;
        _carrinho.SessaoEncerrada += OnSessaoEncerrada;

        Title = "Carrinho";

        _finalizar.Clicked += OnFinalizarClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                MaximumWidthRequest = 760,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 16,
                Children =
                {
                    LojaVisual.Cabecalho("Minhas compras", "Confira os itens e prepare seu pedido."),
                    _mensagem,
                    _lista,
                    LojaVisual.Cartao(new VerticalStackLayout
                    {
                        Spacing = 14,
                        Children =
                        {
                            LojaVisual.Texto("Resumo da compra", 18, true),
                            _total,
                            LojaVisual.Legenda("Total estimado. Preços e disponibilidade serão confirmados ao finalizar."),
                            LojaVisual.Texto("Algum detalhe para a loja?", 13, true),
                            _observacao,
                            _finalizar
                        }
                    })
                }
            }
        };
    }

    private void OnSessaoEncerrada()
    {
        _observacao.Text = string.Empty;

        _mensagem.Text = string.Empty;
        _mensagem.IsVisible = false;

        _resultadoIncerto = false;

        AtualizarTela();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AtualizarTela();
    }

    private void AtualizarTela()
    {
        _lista.Children.Clear();

        foreach (var item in _carrinho.Itens)
        {
            var menos = LojaVisual.Secundario("−");
            var mais = LojaVisual.Secundario("+");
            var remover = LojaVisual.Secundario("Remover");

            menos.Clicked += (_, _) =>
                AlterarQuantidade(item, item.Quantidade - 1);

            mais.Clicked += (_, _) =>
                AlterarQuantidade(item, item.Quantidade + 1);

            remover.Clicked += (_, _) =>
            {
                _carrinho.Remover(item.ProdutoId);
                AtualizarTela();
            };

            var quantidade = LojaVisual.Texto(item.Quantidade.ToString(), 15, true);
            quantidade.VerticalOptions = LayoutOptions.Center;
            quantidade.HorizontalTextAlignment = TextAlignment.Center;
            var controles = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(new GridLength(40)), new(GridLength.Auto), new(GridLength.Star) },
                ColumnSpacing = 6
            };
            controles.Add(menos, 0, 0); controles.Add(quantidade, 1, 0); controles.Add(mais, 2, 0);
            remover.HorizontalOptions = LayoutOptions.End;
            controles.Add(remover, 3, 0);
            var cabecalho = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 12 };
            cabecalho.Add(LojaVisual.Texto(item.Nome, 16, true), 0, 0);
            var subtotal = LojaVisual.Texto(item.Subtotal.ToString("C", Cultura), 17, true);
            subtotal.TextColor = LojaVisual.Verde;
            cabecalho.Add(subtotal, 1, 0);
            _lista.Children.Add(LojaVisual.Cartao(new VerticalStackLayout
            {
                Spacing = 14,
                Children = { cabecalho, LojaVisual.Legenda(item.PrecoUnitario.ToString("C", Cultura) + " por unidade"), controles }
            }));
        }

        if (_carrinho.Itens.Count == 0)
        {
            _lista.Children.Add(new Label
            {
                Text = "Seu carrinho está vazio. Adicione produtos no catálogo."
            });
        }

        _total.Text = "Total: " + _carrinho.Total.ToString("C", Cultura);

        _lista.IsEnabled = !_carrinho.EmEnvio && !_resultadoIncerto;
        _observacao.IsEnabled = !_carrinho.EmEnvio && !_resultadoIncerto;

        _finalizar.IsEnabled =
            _carrinho.Itens.Count > 0 &&
            !_carrinho.EmEnvio &&
            !_resultadoIncerto;

        _finalizar.Text = _carrinho.EmEnvio
            ? "Enviando..."
            : "Finalizar pedido";
    }

    private void AlterarQuantidade(CarrinhoItem item, int quantidade)
    {
        var erro = _carrinho.AlterarQuantidade(
            item.ProdutoId,
            quantidade);

        if (erro is not null)
            MostrarMensagem(erro, true);

        AtualizarTela();
    }

    private async void OnFinalizarClicked(object? sender, EventArgs e)
    {
        if (_carrinho.EmEnvio ||
            _resultadoIncerto ||
            _carrinho.Itens.Count == 0)
        {
            return;
        }

        // Restaura e valida a sessão antes de enviar qualquer pedido.
        _carrinho.EmEnvio = true;
        _mensagem.IsVisible = false;
        AtualizarTela();

        var envioIniciado = false;

        try
        {
            await _authService.RestaurarAsync();

            if (_authService.ObterToken() is null)
            {
                MostrarMensagem(
                    "Entre na aba Conta e volte para finalizar.",
                    true);

                return;
            }

            envioIniciado = true;

            var pedido = await _pedidoService.CriarAsync(
                _carrinho.Itens,
                _observacao.Text);

            _carrinho.LimparAposCompra();
            _observacao.Text = "";

            MostrarMensagem(
                $"Pedido #{pedido.Id} criado! " +
                $"Total confirmado: {pedido.Total.ToString("C", Cultura)}.",
                false);
        }
        catch (InvalidOperationException ex)
        {
            MostrarMensagem(ex.Message, true);
        }
        catch (HttpRequestException)
        {
            TratarFalhaComunicacao(envioIniciado);
        }
        catch (TaskCanceledException)
        {
            TratarFalhaComunicacao(envioIniciado);
        }
        catch (System.Text.Json.JsonException)
        {
            TratarFalhaComunicacao(envioIniciado);
        }
        finally
        {
            _carrinho.EmEnvio = false;
            AtualizarTela();
        }
    }

    private void TratarFalhaComunicacao(bool envioIniciado)
    {
        if (envioIniciado)
        {
            _resultadoIncerto = true;

            MostrarMensagem(
                "Não foi possível confirmar o resultado do envio. " +
                "O pedido pode ter sido criado. Não repita a compra " +
                "antes de conferir seus pedidos. O reenvio nesta tela " +
                "foi bloqueado para evitar duplicidade.",
                true);
        }
        else
        {
            MostrarMensagem(
                "Não foi possível validar sua sessão. " +
                "Confira a conexão com a API e tente novamente. " +
                "Nenhum pedido foi enviado.",
                true);
        }
    }

    private void MostrarMensagem(string texto, bool erro)
    {
        _mensagem.Text = texto;
        _mensagem.TextColor = erro ? LojaVisual.Erro : LojaVisual.Verde;
        _mensagem.IsVisible = true;
    }
}