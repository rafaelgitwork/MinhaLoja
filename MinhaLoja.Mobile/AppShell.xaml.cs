namespace MinhaLoja.Mobile;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage)
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            Title = "Produtos",
            Route = "produtos",
            Content = mainPage
        });
    }
}