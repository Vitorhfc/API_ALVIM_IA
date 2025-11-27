using Admin_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio.Interface
{
    public interface IRefreshTokenRepository : IRepositorioGenerico<RefreshToken>
    {
        Task<RefreshToken?> BuscarPorTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> BuscarPorUsuarioIdAsync(string usuarioId);
        Task<IEnumerable<RefreshToken>> BuscarPorUsuarioEmpresaAsync(string usuarioId, string empresaId);
        Task RevogarTokensUsuarioAsync(string usuarioId);
        Task RevogarTokensUsuarioEmpresaAsync(string usuarioId, string empresaId);
        Task LimparTokensExpiradosAsync();
    }
}