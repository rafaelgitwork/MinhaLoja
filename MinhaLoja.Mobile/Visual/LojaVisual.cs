using MinhaLoja.Shared.Enums;
using Microsoft.Maui.Controls.Shapes;
namespace MinhaLoja.Mobile.Visual;
public static class LojaVisual
{
    public static readonly Color Verde = Color.FromArgb("#236550");
    public static readonly Color Suave = Color.FromArgb("#60736D");
    public static readonly Color Erro = Color.FromArgb("#944539");
    public static Label Texto(string texto, double tamanho = 14, bool forte = false) => new()
    { Text = texto, FontSize = tamanho, FontFamily = forte ? "OpenSansSemibold" : "OpenSansRegular", LineBreakMode = LineBreakMode.WordWrap };
    public static Label Legenda(string texto) { var label = Texto(texto, 12); label.TextColor = Suave; return label; }
    public static VerticalStackLayout Cabecalho(string titulo, string descricao) => new()
    { Spacing = 5, Children = { new Label { Text = "MINHALOJA", FontSize = 10, CharacterSpacing = 2, TextColor = Verde, FontAttributes = FontAttributes.Bold }, Texto(titulo, 27, true), Legenda(descricao) } };
    public static Border Cartao(View conteudo) => new() { Padding = 18, Content = conteudo };
    public static Button Secundario(string texto) => new() { Text = texto, BackgroundColor = Color.FromArgb("#E8EFE6"), TextColor = Verde };
    public static Label Status(PedidoStatus status, string texto)
    {
        var (fundo, cor) = status switch
        {
            PedidoStatus.Pendente => ("#FAF0CE", "#795A1E"),
            PedidoStatus.EmPreparacao => ("#E2EFF8", "#285F84"),
            PedidoStatus.ProntoParaRetirada => ("#E5F3E8", "#246145"),
            PedidoStatus.Cancelado => ("#FAE6E1", "#944539"),
            _ => ("#E9EDE9", "#58645E")
        };
        return new Label { Text = texto, FontSize = 11, Padding = new Thickness(10,6), TextColor = Color.FromArgb(cor), BackgroundColor = Color.FromArgb(fundo), HorizontalOptions = LayoutOptions.Start };
    }
    // Rodapé fixo: mesmas rotas e páginas do Shell, com Conta sempre à direita.
    public static void Navegacao(ContentPage pagina, string ativo)
    {
        var conteudo = pagina.Content ?? throw new InvalidOperationException("A página precisa ter conteúdo antes da navegação.");
        pagina.Content = null;
        Shell.SetNavBarIsVisible(pagina, false);
        Shell.SetTabBarIsVisible(pagina, false);
        var abas = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, Padding = new Thickness(12,8), ColumnSpacing = 4, MaximumWidthRequest = 920 };
        var itens = new[] { ("catalogo", "Catálogo", "nav_catalogo.png", "//catalogo/produtos"), ("compras", "Compras", "nav_compras.png", "//compras/carrinho"), ("historico", "Pedidos", "nav_pedidos.png", "//historico/meus-pedidos"), ("minha-conta", "Conta", "nav_conta.png", "//minha-conta/conta") };
        for (var i = 0; i < itens.Length; i++)
        {
            var item = itens[i];
            // Button keeps keyboard focus and native accessibility semantics.
            var botao = new Button { Text = item.Item2, FontSize = 11, ImageSource = item.Item3, ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Top, 4), BackgroundColor = Color.FromArgb(item.Item1 == ativo ? "#E8EFE6" : "#FFFFFF"), TextColor = item.Item1 == ativo ? Verde : Suave, Padding = new Thickness(4,8), MinimumHeightRequest = 64 };
            botao.Clicked += async (_, _) =>
            {
                if (item.Item1 == ativo || Shell.Current is null) return;
                abas.IsEnabled = false;
                try { await Shell.Current.GoToAsync(item.Item4); }
                catch (Exception) { await pagina.DisplayAlertAsync("Navegação", "Não foi possível abrir esta aba. Tente novamente.", "OK"); }
                finally { abas.IsEnabled = true; }
            };
            abas.Add(botao, i, 0);
        }
        var rodape = new Border { StrokeShape = new Rectangle(), Padding = 0, Content = abas, BackgroundColor = Colors.White };
        var raiz = new Grid { RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        raiz.Add(conteudo, 0, 0); raiz.Add(rodape, 0, 1);
        pagina.Content = raiz;
    }
}
