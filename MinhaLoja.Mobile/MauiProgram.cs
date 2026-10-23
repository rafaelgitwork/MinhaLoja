using Microsoft.Extensions.Logging;
using MinhaLoja.Mobile.Configuracao;
using MinhaLoja.Mobile.Services;
using System.Globalization;
using MinhaLoja.Mobile.Pages;

namespace MinhaLoja.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
        var cultura = CultureInfo.GetCultureInfo("pt-BR");

        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;

        var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<HttpClient>(_ => new HttpClient
        {
            BaseAddress = new Uri(ApiConfiguracao.BaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        });

        builder.Services.AddSingleton<ProdutoApiService>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddSingleton<AuthApiService>();
        builder.Services.AddSingleton<ContaPage>();

        builder.Services.AddSingleton<CarrinhoService>();
        builder.Services.AddSingleton<PedidoApiService>();
        builder.Services.AddSingleton<CarrinhoPage>();

        return builder.Build();
	}
}
