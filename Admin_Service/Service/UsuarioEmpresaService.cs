using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Admin_Service.ServiceGenerico;
using Shared.Classes.Entidades.ADM;

namespace Admin_Service.Service
{
    public class UsuarioEmpresaService : ServiceGenerico<UsuarioEmpresa>, IUsuarioEmpresaService
    {
        private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

        public UsuarioEmpresaService(IUsuarioEmpresaRepository usuarioEmpresaRepository)
            : base(usuarioEmpresaRepository)
        {
            _usuarioEmpresaRepository = usuarioEmpresaRepository;
        }

        public async Task<IEnumerable<UsuarioEmpresa>> BuscarPorUsuarioIdAsync(string usuarioId)
        {
            try
            {
                return await _usuarioEmpresaRepository.BuscarPorUsuarioIdAsync(usuarioId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar vínculos por UsuarioId: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<UsuarioEmpresa>> BuscarPorEmpresaIdAsync(string empresaId)
        {
            try
            {
                return await _usuarioEmpresaRepository.BuscarPorEmpresaIdAsync(empresaId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar vínculos por EmpresaId: {ex.Message}", ex);
            }
        }

        public async Task<UsuarioEmpresa?> BuscarPorUsuarioEmpresaAsync(string usuarioId, string empresaId)
        {
            try
            {
                return await _usuarioEmpresaRepository.BuscarPorUsuarioEmpresaAsync(usuarioId, empresaId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar vínculo: {ex.Message}", ex);
            }
        }

        public override async Task<UsuarioEmpresa?> AdicionarAsync(UsuarioEmpresa usuarioEmpresa)
        {
            try
            {
                await ValidarUsuarioEmpresaParaCriacaoAsync(usuarioEmpresa);
                return await base.AdicionarAsync(usuarioEmpresa);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao criar vínculo: {ex.Message}", ex);
            }
        }

        public override async Task<UsuarioEmpresa?> EditarAsync(UsuarioEmpresa usuarioEmpresa)
        {
            try
            {
                await ValidarUsuarioEmpresaParaEdicaoAsync(usuarioEmpresa);
                return await base.EditarAsync(usuarioEmpresa);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao editar vínculo: {ex.Message}", ex);
            }
        }

        public override async Task ValidarEntidadeAsync(UsuarioEmpresa entity)
        {
            ValidarCampoObrigatorio(entity.UsuarioId, "UsuarioId");
            ValidarCampoObrigatorio(entity.EmpresaId, "EmpresaId");
            await Task.CompletedTask;
        }

        private async Task ValidarUsuarioEmpresaParaCriacaoAsync(UsuarioEmpresa usuarioEmpresa)
        {
            await ValidarEntidadeAsync(usuarioEmpresa);

            if (await _usuarioEmpresaRepository.ExisteVinculoAsync(usuarioEmpresa.UsuarioId, usuarioEmpresa.EmpresaId))
                throw new InvalidOperationException("Já existe um vínculo entre este usuário e esta empresa");
        }

        private async Task ValidarUsuarioEmpresaParaEdicaoAsync(UsuarioEmpresa usuarioEmpresa)
        {
            await ValidarEntidadeAsync(usuarioEmpresa);

            if (await _usuarioEmpresaRepository.ExisteVinculoAsync(usuarioEmpresa.UsuarioId, usuarioEmpresa.EmpresaId, usuarioEmpresa.Id))
                throw new InvalidOperationException("Já existe um vínculo entre este usuário e esta empresa");
        }
    }
}