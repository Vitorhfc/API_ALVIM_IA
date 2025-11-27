using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;

namespace Client_Service.Service.Interface
{
    public interface IPlanoContextoService
    {
        /// <summary>
        /// Busca todos os planos de contexto
        /// </summary>
        Task<List<PlanoContextoResponse>> BuscarTodosAsync();

        /// <summary>
        /// Busca apenas planos ativos
        /// </summary>
        Task<List<PlanoContextoResponse>> BuscarAtivosAsync();

        /// <summary>
        /// Busca plano por ID
        /// </summary>
        Task<PlanoContextoResponse?> BuscarPorIdAsync(string id);

        /// <summary>
        /// Cria um novo plano de contexto
        /// </summary>
        Task<PlanoContextoResponse> CriarAsync(CriarPlanoContextoRequest request);

        /// <summary>
        /// Atualiza um plano existente
        /// </summary>
        Task<PlanoContextoResponse> AtualizarAsync(string id, AtualizarPlanoContextoRequest request);

        /// <summary>
        /// Deleta um plano (apenas se não for padrão)
        /// </summary>
        Task<bool> DeletarAsync(string id);

        /// <summary>
        /// Configura múltiplos planos (ativa/desativa)
        /// REGRA: Plano Base sempre ativo
        /// </summary>
        Task<ConfigurarPlanosResponse> ConfigurarPlanosAsync(ConfigurarPlanosRequest request);

        /// <summary>
        /// Aplica planos ativos na ConfiguracaoIA
        /// Preenche os campos da ConfiguracaoIA com base nos planos ativos
        /// </summary>
        Task<bool> AplicarPlanosNaConfiguracaoAsync(string configuracaoIAId);

        /// <summary>
        /// Inicializa planos padrão do sistema (se não existirem)
        /// </summary>
        Task InicializarPlanosPadraoAsync();
    }
}
