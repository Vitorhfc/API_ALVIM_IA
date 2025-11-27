using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio.Interface
{
    public interface IPlanoContextoRepositorio
    {
        /// <summary>
        /// Busca todos os planos de contexto
        /// </summary>
        Task<List<PlanoContexto>> BuscarTodosAsync();

        /// <summary>
        /// Busca planos ativos ordenados por Ordem
        /// </summary>
        Task<List<PlanoContexto>> BuscarAtivosAsync();

        /// <summary>
        /// Busca plano por ID
        /// </summary>
        Task<PlanoContexto?> BuscarPorIdAsync(string id);

        /// <summary>
        /// Busca planos por tipo
        /// </summary>
        Task<List<PlanoContexto>> BuscarPorTipoAsync(TipoPlanoContexto tipo);

        /// <summary>
        /// Busca o plano base
        /// </summary>
        Task<PlanoContexto?> BuscarPlanoBaseAsync();

        /// <summary>
        /// Cria um novo plano
        /// </summary>
        Task<PlanoContexto> CriarAsync(PlanoContexto plano);

        /// <summary>
        /// Atualiza um plano existente
        /// </summary>
        Task<PlanoContexto> AtualizarAsync(PlanoContexto plano);

        /// <summary>
        /// Deleta um plano (apenas se FlgPadrao = false)
        /// </summary>
        Task<bool> DeletarAsync(string id);

        /// <summary>
        /// Ativa ou desativa um plano
        /// </summary>
        Task<bool> AlterarStatusAsync(string id, bool flgAtivo);

        /// <summary>
        /// Atualiza múltiplos planos em batch
        /// </summary>
        Task<int> AtualizarVariosAsync(List<PlanoContexto> planos);

        /// <summary>
        /// Verifica se existe plano com determinado tipo
        /// </summary>
        Task<bool> ExisteTipoAsync(TipoPlanoContexto tipo);
    }
}
