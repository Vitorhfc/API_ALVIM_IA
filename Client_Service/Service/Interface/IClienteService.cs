using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface IClienteService : IServiceGenerico<Cliente>
    {
        Task<Cliente?> BuscarPorTelefoneAsync(string telefone);
        Task<Cliente?> BuscarPorEmailAsync(string email);
        Task<IEnumerable<Cliente>> BuscarClientesAtivosPorTermoAsync(string termo);
        Task<bool> ValidarTelefoneUnicoAsync(string telefone, string? idExcluir = null);
        Task<bool> ValidarEmailUnicoAsync(string email, string? idExcluir = null);
    }
}