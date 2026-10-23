using MinhaLoja.Mobile.Pages;

namespace MinhaLoja.Mobile;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage, ContaPage contaPage)
    {
        InitializeComponent();

        var abas = new TabBar();

        var produtosTab = new Tab
        {
            Title = "Produtos",
            Route = "catalogo"
        };

        produtosTab.Items.Add(new ShellContent
        {
            Title = "Produtos",
            Route = "produtos",
            Content = mainPage
        });

        var contaTab = new Tab
        {
            Title = "Conta",
            Route = "minha-conta"
        };

        contaTab.Items.Add(new ShellContent
        {
            Title = "Conta",
            Route = "conta",
            Content = contaPage
        });

        abas.Items.Add(produtosTab);
        abas.Items.Add(contaTab);

        Items.Add(abas);
    }
}