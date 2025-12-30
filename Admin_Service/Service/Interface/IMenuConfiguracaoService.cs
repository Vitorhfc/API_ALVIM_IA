using Admin_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Service.Service.Interface
{
    /// <summary>
    /// Interface para serviço de MenuConfiguracao
    /// </summary>
    public interface IMenuConfiguracaoService : IServiceGenerico<MenuConfiguracao>
    {
        /// <summary>
        /// Busca todos os menus raiz (sem pai), ordenados por Ordem
        /// </summary>
        Task<IEnumerable<MenuConfiguracao>> BuscarMenusRaizAsync();

        /// <summary>
        /// Busca todos os menus (raiz e filhos), ordenados por Ordem
        /// </summary>
        Task<IEnumerable<MenuConfiguracao>> BuscarTodosMenusAsync();

        /// <summary>
        /// Busca os submenus de um menu pai
        /// </summary>
        /// <param name="menuPaiId">ID do menu pai</param>
        Task<IEnumerable<MenuConfiguracao>> BuscarSubmenusPorMenuPaiAsync(string menuPaiId);

        /// <summary>
        /// Busca um menu por rota
        /// </summary>
        /// <param name="route">Rota do menu</param>
        Task<MenuConfiguracao?> BuscarPorRotaAsync(string route);

        /// <summary>
        /// Constrói a estrutura hierárquica de menus com seus filhos
        /// </summary>
        Task<IEnumerable<MenuConfiguracao>> BuscarMenusComHierarquiaAsync();

        /// <summary>
        /// Salva múltiplos menus de uma vez (útil para configuração inicial)
        /// </summary>
        /// <param name="menus">Lista de menus a serem salvos</param>
        Task<List<MenuConfiguracao>> SalvarMenusAsync(List<MenuConfiguracao> menus);

        /// <summary>
        /// Alterna o status habilitado/desabilitado de um menu
        /// </summary>
        /// <param name="id">ID do menu</param>
        Task<MenuConfiguracao?> AlternarStatusAsync(string id);

        /// <summary>
        /// Busca a estrutura hierárquica de menus ativos para exibição no menu lateral
        /// Retorna apenas menus com FlgAtivo = true e Disabled = false
        /// </summary>
        Task<IEnumerable<MenuConfiguracao>> BuscarMenuLateralAsync();
    }
}
