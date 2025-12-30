using Admin_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio.Interface
{
    /// <summary>
    /// Interface para repositório de MenuConfiguracao
    /// </summary>
    public interface IMenuConfiguracaoRepository : IRepositorioGenerico<MenuConfiguracao>
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
        /// Busca todos os menus ativos (FlgAtivo = true e Disabled = false)
        /// </summary>
        Task<IEnumerable<MenuConfiguracao>> BuscarMenusAtivosAsync();
    }
}
