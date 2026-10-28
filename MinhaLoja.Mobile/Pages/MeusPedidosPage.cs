using MinhaLoja.Mobile.Visual;
using System.Globalization;
using MinhaLoja.Mobile.Services;
using MinhaLoja.Shared.DTOs.Pedidos;
using MinhaLoja.Shared.Enums;

namespace MinhaLoja.Mobile.Pages;

public class MeusPedidosPage : ContentPage
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("pt-BR");

    private const int TamanhoPagina = 10;

    private readonly PedidoApiService _pedidoService;

    private readonly VerticalStackLayout _lista = new()
    {
        Spacing = 12
    };

    private readonly VerticalStackLayout _detalhes = new()
    {
        Spacing = 8,
        IsVisible = false
    };

    private readonly Label _mensagem = new()
    {
        IsVisible = false
    };

    private readonly ActivityIndicator _indicador = new()
    {
        IsVisible = false
    };

    private readonly Button _atualizar = new()
    {
        Text = "Atualizar pedidos"
    };

    private readonly Button _anterior = new()
    {
        Text = "Anterior"
    };

    private readonly Button _proxima = new()
    {
        Text = "Próxima"
    };

    private readonly Label _paginaLabel = new()
    {
        VerticalOptions = LayoutOptions.Center
    };

    private bool _ocupado;
    private int _pagina = 1;
    private int _quantidadeCarregada;

    public MeusPedidosPage(PedidoApiService pedidoService)
    {
        _pedidoService = pedidoService;

        Title = "Meus pedidos";

        _atualizar.Clicked += async (_, _) =>
            await ExecutarAsync(CarregarListaAsync);

        _anterior.Clicked += async (_, _) =>
        {
            if (_ocupado || _pagina <= 1)
                return;

            await ExecutarAsync(async () =>
            {
                _pagina--;
                await CarregarListaAsync();
            });
        };

        _proxima.Clicked += async (_, _) =>
        {
            if (_ocupado || _quantidadeCarregada < TamanhoPagina)
                return;

            await ExecutarAsync(async () =>
            {
                _pagina++;
                await CarregarListaAsync();
            });
        };

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
                    LojaVisual.Cabecalho("Meus pedidos", "Da preparação à retirada, acompanhe tudo aqui."),
                    _atualizar,
                    _indicador,
                    _mensagem,
                    CriarCartaoDetalhes(),
                    _lista,
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            _anterior,
                            _paginaLabel,
                            _proxima
                        }
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await ExecutarAsync(async () =>
        {
            _pagina = 1;
            await CarregarListaAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Evita manter os dados de uma conta visíveis ao trocar de usuário.
        _lista.Children.Clear();
        _detalhes.Children.Clear();
        _detalhes.IsVisible = false;
    }

    private async Task CarregarListaAsync()
    {
        _lista.Children.Clear();
        _detalhes.Children.Clear();
        _detalhes.IsVisible = false;
        _quantidadeCarregada = 0;

        var pedidos = await _pedidoService.ListarAsync(
            _pagina,
            TamanhoPagina);

        _quantidadeCarregada = pedidos.Count;

        if (pedidos.Count == 0)
        {
            _lista.Children.Add(new Label
            {
                Text = _pagina == 1
                    ? "Você ainda não tem pedidos."
                    : "Não há mais pedidos nesta página."
            });

            return;
        }

        foreach (var pedido in pedidos)
        {
            var detalhesButton = new Button
            {
                Text = "Ver detalhes"
            };

            detalhesButton.Clicked += async (_, _) =>
                await ExecutarAsync(async () =>
                {
                    _detalhes.IsVisible = false;

                    var atualizado = await _pedidoService
                        .ObterPorIdAsync(pedido.Id);

                    MostrarDetalhes(atualizado);
                });

            var topo = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10 };
            topo.Add(LojaVisual.Texto($"Pedido #{pedido.Id}", 18, true), 0, 0);
            topo.Add(LojaVisual.Status(pedido.Status, NomeStatus(pedido.Status)), 1, 0);
            var rodape = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 12 };
            var valor = LojaVisual.Texto(pedido.Total.ToString("C", Cultura), 22, true);
            valor.VerticalOptions = LayoutOptions.Center;
            valor.TextColor = LojaVisual.Verde;
            rodape.Add(valor, 0, 0); rodape.Add(detalhesButton, 1, 0);
            _lista.Children.Add(LojaVisual.Cartao(new VerticalStackLayout
            {
                Spacing = 16,
                Children = { topo, LojaVisual.Legenda("Criado em " + pedido.CriadoEmUtc.ToLocalTime().ToString("dd/MM/yyyy · HH:mm")), rodape }
            }));
        }
    }

    private void MostrarDetalhes(PedidoResponse pedido)
    {
        _detalhes.Children.Clear();

        _detalhes.Children.Add(new Label
        {
            Text = $"Detalhes do pedido #{pedido.Id}",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold
        });

        _detalhes.Children.Add(LojaVisual.Status(pedido.Status, NomeStatus(pedido.Status)));

        foreach (var item in pedido.Itens)
        {
            _detalhes.Children.Add(new Label
            {
                Text =
                    $"{item.NomeProduto}\n" +
                    $"{item.Quantidade} × " +
                    $"{item.PrecoUnitario.ToString("C", Cultura)} = " +
                    item.Subtotal.ToString("C", Cultura)
            });
        }

        _detalhes.Children.Add(new Label
        {
            Text = "Total: " + pedido.Total.ToString("C", Cultura),
            FontAttributes = FontAttributes.Bold
        });

        if (!string.IsNullOrWhiteSpace(pedido.Observacao))
        {
            _detalhes.Children.Add(new Label
            {
                Text = "Observação: " + pedido.Observacao
            });
        }

        if (pedido.Status == PedidoStatus.Pendente)
        {
            var cancelar = new Button
            {
                Text = "Cancelar pedido",
                BackgroundColor = Color.FromArgb("#944539"),
                TextColor = Colors.White
            };

            cancelar.Clicked += async (_, _) =>
                await ExecutarAsync(async () =>
                {
                    var confirmado = await DisplayAlertAsync(
                        "Cancelar pedido",
                        $"Deseja cancelar o pedido #{pedido.Id}?",
                        "Sim, cancelar",
                        "Voltar");

                    if (!confirmado)
                        return;

                    await _pedidoService.CancelarAsync(pedido.Id);

                    MostrarMensagem(
                        $"Pedido #{pedido.Id} cancelado. " +
                        "O estoque foi devolvido pela API.",
                        false);

                    await CarregarListaAsync();
                });

            _detalhes.Children.Add(cancelar);
        }

        var fechar = new Button
        {
            Text = "Fechar detalhes"
        };

        fechar.Clicked += (_, _) => _detalhes.IsVisible = false;

        _detalhes.Children.Add(fechar);
        _detalhes.IsVisible = true;
    }

    private async Task ExecutarAsync(Func<Task> acao)
    {
        if (_ocupado)
            return;

        _ocupado = true;
        _mensagem.IsVisible = false;
        AtualizarControles();

        try
        {
            await acao();
        }
        catch (InvalidOperationException ex)
        {
            MostrarMensagem(ex.Message, true);
        }
        catch (HttpRequestException)
        {
            MostrarMensagem(
                "Falha na comunicação com a API. " +
                "Se tentou cancelar, atualize os pedidos " +
                "para conferir o resultado.",
                true);
        }
        catch (TaskCanceledException)
        {
            MostrarMensagem(
                "A operação demorou demais. Atualize os pedidos " +
                "para conferir o resultado.",
                true);
        }
        catch (System.Text.Json.JsonException)
        {
            MostrarMensagem(
                "A API retornou dados em formato inesperado.",
                true);
        }
        finally
        {
            _ocupado = false;
            AtualizarControles();
        }
    }

    private void AtualizarControles()
    {
        _indicador.IsVisible = _ocupado;
        _indicador.IsRunning = _ocupado;

        _atualizar.IsEnabled = !_ocupado;
        _lista.IsEnabled = !_ocupado;
        _detalhes.IsEnabled = !_ocupado;

        _anterior.IsEnabled = !_ocupado && _pagina > 1;

        _proxima.IsEnabled =
            !_ocupado &&
            _quantidadeCarregada == TamanhoPagina;

        _paginaLabel.Text = $"Página {_pagina}";
    }

    private void MostrarMensagem(string texto, bool erro)
    {
        _mensagem.Text = texto;
        _mensagem.TextColor = erro ? LojaVisual.Erro : LojaVisual.Verde;
        _mensagem.IsVisible = true;
    }

    private Border CriarCartaoDetalhes()
    {
        var cartao = LojaVisual.Cartao(_detalhes);
        cartao.SetBinding(IsVisibleProperty, new Binding(nameof(IsVisible), source: _detalhes));
        return cartao;
    }

    private static string NomeStatus(PedidoStatus status) =>
        status switch
        {
            PedidoStatus.Pendente => "Pendente",
            PedidoStatus.EmPreparacao => "Em preparação",
            PedidoStatus.ProntoParaRetirada => "Pronto para retirada",
            PedidoStatus.Concluido => "Concluído",
            PedidoStatus.Cancelado => "Cancelado",
            _ => "Desconhecido"
        };
}