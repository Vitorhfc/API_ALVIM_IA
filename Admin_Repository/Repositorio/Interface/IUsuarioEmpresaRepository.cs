using Admin_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio.Interface
{
    public interface IUsuarioEmpresaRepository : IRepositorioGenerico<UsuarioEmpresa>
    {
        Task<IEnumerable<UsuarioEmpresa>> BuscarPorUsuarioIdAsync(string usuarioId);
        Task<IEnumerable<UsuarioEmpresa>> BuscarPorEmpresaIdAsync(string empresaId);
        Task<UsuarioEmpresa?> BuscarPorUsuarioEmpresaAsync(string usuarioId, string empresaId);
        Task<bool> ExisteVinculoAsync(string usuarioId, string empresaId, string? excluirId = null);
        Task<bool> VerificarVinculoAtivoAsync(string usuarioId, string empresaId);
        Task<bool> VerificarSeUsuarioEAdminDaEmpresaAsync(string usuarioId, string empresaId);
    }
}