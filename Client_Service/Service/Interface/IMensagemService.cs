using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;


namespace Client_Service.Service.Interface
{
    public interface IMensagemService : IServiceGenerico<Mensagem>
    {
        Task<IEnumerable<Mensagem>> BuscarPorClienteAsync(string clienteId);
        Task<IEnumerable<Mensagem>> BuscarPorRemetenteAsync(string remetenteId);
        Task<IEnumerable<Mensagem>> BuscarNaoLidasAsync(string conversaId);
        Task<int> ContarNaoLidasAsync(string conversaId);
    }
}