using Admin_Repository.Configuration;
using Admin_Repository.Repositorio.Interface;
using Admin_Repository.RepositorioGenerico;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio
{
    public class UsuarioEmpresaRepository : RepositorioGenerico<UsuarioEmpresa>, IUsuarioEmpresaRepository
    {
        public UsuarioEmpresaRepository(ContextBaseAdmin contextBaseAdmin)
            : base(contextBaseAdmin, "UsuarioEmpresa")
        {
        }

        public async Task<IEnumerable<UsuarioEmpresa>> BuscarPorUsuarioIdAsync(string usuarioId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId))
                    throw new ArgumentException("UsuarioId é obrigatório", nameof(usuarioId));

                return await BuscarPorFiltroAsync(ue => ue.UsuarioId == usuarioId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar vínculos por UsuarioId '{usuarioId}': {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<UsuarioEmpresa>> BuscarPorEmpresaIdAsync(string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                    throw new ArgumentException("EmpresaId é obrigatório", nameof(empresaId));

                return await BuscarPorFiltroAsync(ue => ue.EmpresaId == empresaId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar vínculos por EmpresaId '{empresaId}': {ex.Message}", ex);
            }
        }

        public async Task<UsuarioEmpresa?> BuscarPorUsuarioEmpresaAsync(string usuarioId, string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId))
                    throw new ArgumentException("UsuarioId é obrigatório", nameof(usuarioId));

                if (string.IsNullOrWhiteSpace(empresaId))
                    throw new ArgumentException("EmpresaId é obrigatório", nameof(empresaId));

                return await BuscarPrimeiroPorFiltroAsync(ue => ue.UsuarioId == usuarioId && ue.EmpresaId == empresaId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar vínculo UsuarioId '{usuarioId}' e EmpresaId '{empresaId}': {ex.Message}", ex);
            }
        }

        public async Task<bool> ExisteVinculoAsync(string usuarioId, string empresaId, string? excluirId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(empresaId))
                    return false;

                var vinculos = await BuscarPorFiltroAsync(ue =>
                    ue.UsuarioId == usuarioId &&
                    ue.EmpresaId == empresaId &&
                    (excluirId == null || ue.Id != excluirId));

                return vinculos.Any();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao verificar existência de vínculo: {ex.Message}", ex);
            }
        }

        public async Task<bool> VerificarVinculoAtivoAsync(string usuarioId, string empresaId)
        {
            var filter = Builders<UsuarioEmpresa>.Filter.And(
                Builders<UsuarioEmpresa>.Filter.Eq(x => x.UsuarioId, usuarioId),
                Builders<UsuarioEmpresa>.Filter.Eq(x => x.EmpresaId, empresaId),
                Builders<UsuarioEmpresa>.Filter.Eq(x => x.FlgAtivo, true)
            );

            var count = await _collection.CountDocumentsAsync(filter);
            return count > 0;
        }

        public async Task<bool> VerificarSeUsuarioEAdminDaEmpresaAsync(string usuarioId, string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(empresaId))
                    return false;

                var filter = Builders<UsuarioEmpresa>.Filter.And(
                    Builders<UsuarioEmpresa>.Filter.Eq(x => x.UsuarioId, usuarioId),
                    Builders<UsuarioEmpresa>.Filter.Eq(x => x.EmpresaId, empresaId),
                    Builders<UsuarioEmpresa>.Filter.Eq(x => x.FlgAtivo, true),
                    Builders<UsuarioEmpresa>.Filter.Eq(x => x.FlgAdministrador, true)
                );

                var count = await _collection.CountDocumentsAsync(filter);
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Erro ao verificar se usuário '{usuarioId}' é admin da empresa '{empresaId}': {ex.Message}",
                    ex
                );
            }
        }
    }
}