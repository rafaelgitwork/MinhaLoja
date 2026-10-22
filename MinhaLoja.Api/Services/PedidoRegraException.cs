namespace MinhaLoja.Api.Services;

public class PedidoRegraException : Exception
{
    public PedidoRegraException(string mensagem) : base(mensagem)
    {
    }
}