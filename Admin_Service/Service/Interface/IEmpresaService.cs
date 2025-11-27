using Admin_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Service.Service.Interface
{
    public interface IEmpresaService : IServiceGenerico<Empresa>
    {
        Task<Empresa?> BuscarPorCnpjAsync(string cnpj);
        Task<Empresa?> BuscarPorEmailAsync(string email);
        Task<Empresa?> CadastrarEmpresaComUsuarioAsync(Empresa empresa, string usuarioId);
        Task<Empresa?> ProvisionarBancoDadosAsync(string empresaId);
        Task<Empresa?> BuscarPorSessionNameAsync(string sessionName);
    }
}