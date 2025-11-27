using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface IConfiguracaoIAService : IServiceGenerico<ConfiguracaoIA>
    {
        Task<ConfiguracaoIA?> BuscarConfiguracaoAsync();
    }
}