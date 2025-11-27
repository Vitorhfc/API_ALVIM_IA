using Admin_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Service.Service.Interface
{
    public interface IUsuarioEmpresaService : IServiceGenerico<UsuarioEmpresa>
    {
        Task<IEnumerable<UsuarioEmpresa>> BuscarPorUsuarioIdAsync(string usuarioId);
        Task<IEnumerable<UsuarioEmpresa>> BuscarPorEmpresaIdAsync(string empresaId);
        Task<UsuarioEmpresa?> BuscarPorUsuarioEmpresaAsync(string usuarioId, string empresaId);
    }
}