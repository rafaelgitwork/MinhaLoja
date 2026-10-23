using Microsoft.Maui.Devices;

namespace MinhaLoja.Mobile.Configuracao;

public static class ApiConfiguracao
{
    public static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5271/"
            : "http://localhost:5271/";
}