using MinhaLoja.Mobile.Pages;

namespace MinhaLoja.Mobile;

public partial class AppShell : Shell
{
    public AppShell(
        MainPage mainPage,
        CarrinhoPage carrinhoPage,
        MeusPedidosPage meusPedidosPage,
        ContaPage contaPage)
    {
        InitializeComponent();

        var abas = new TabBar();

        abas.Items.Add(CriarAba(
            "Produtos", "catalogo", "produtos", mainPage));

        abas.Items.Add(CriarAba(
            "Carrinho", "compras", "carrinho", carrinhoPage));

        abas.Items.Add(CriarAba(
            "Pedidos", "historico", "meus-pedidos", meusPedidosPage));

        abas.Items.Add(CriarAba(
            "Conta", "minha-conta", "conta", contaPage));

        Items.Add(abas);
    }

    private static Tab CriarAba(
        string titulo,
        string rotaAba,
        string rotaPagina,
        ContentPage pagina)
    {
        var aba = new Tab
        {
            Title = titulo,
            Route = rotaAba
        };

        aba.Items.Add(new ShellContent
        {
            Title = titulo,
            Route = rotaPagina,
            Content = pagina
        });

        return aba;
    }
}