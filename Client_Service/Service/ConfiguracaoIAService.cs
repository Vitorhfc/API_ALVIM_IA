using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service
{
    public class ConfiguracaoIAService : ServiceGenerico<ConfiguracaoIA>, IConfiguracaoIAService
    {
        private readonly IConfiguracaoIARepositorio _repositorio;
        public ConfiguracaoIAService(IConfiguracaoIARepositorio repositorio) : base(repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<ConfiguracaoIA?> BuscarConfiguracaoAsync()
        {
            try
            {
                return await _repositorio.BuscarPrimeiroPorFiltroAsync(c => true);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar configuração: {ex.Message}", ex);
            }
        }

        protected override async Task ValidarEntidade(ConfiguracaoIA entidade)
        {
            var erros = new List<string>();

            if (erros.Any())
                throw new ArgumentException($"Validação falhou: {string.Join(", ", erros)}");

            await Task.CompletedTask;
        }
    }
}