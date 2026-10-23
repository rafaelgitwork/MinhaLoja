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

        Title = "Carrinho";

        _finalizar.Clicked += OnFinalizarClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "Meu carrinho",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold
                    },
                    _mensagem,
                    _lista,
                    _total,
                    new Label
                    {
                        Text = "Total estimado. Preços e disponibilidade " +
                               "serão confirmados pela API ao finalizar."
                    },
                    _observacao,
                    _finalizar
                }
            }
        };
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
            var menos = new Button { Text = "−" };
            var mais = new Button { Text = "+" };
            var remover = new Button { Text = "Remover" };

            menos.Clicked += (_, _) =>
                AlterarQuantidade(item, item.Quantidade - 1);

            mais.Clicked += (_, _) =>
                AlterarQuantidade(item, item.Quantidade + 1);

            remover.Clicked += (_, _) =>
            {
                _carrinho.Remover(item.ProdutoId);
                AtualizarTela();
            };

            var controles = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    menos,
                    new Label
                    {
                        Text = item.Quantidade.ToString(),
                        VerticalOptions = LayoutOptions.Center
                    },
                    mais,
                    remover
                }
            };

            _lista.Children.Add(new Border
            {
                Padding = new Thickness(12),
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = item.Nome,
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold
                        },
                        new Label
                        {
                            Text = "Preço unitário: " +
                                   item.PrecoUnitario.ToString("C", Cultura)
                        },
                        new Label
                        {
                            Text = "Subtotal: " +
                                   item.Subtotal.ToString("C", Cultura)
                        },
                        controles
                    }
                }
            });
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
        _mensagem.TextColor = erro ? Colors.Red : Colors.Green;
        _mensagem.IsVisible = true;
    }
}