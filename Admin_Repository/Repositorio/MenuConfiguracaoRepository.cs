using Admin_Repository.Configuration;
using Admin_Repository.Repositorio.Interface;
using Admin_Repository.RepositorioGenerico;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio
{
    /// <summary>
    /// Repositório específico para operações com a entidade MenuConfiguracao
    /// </summary>
    public class MenuConfiguracaoRepository : RepositorioGenerico<MenuConfiguracao>, IMenuConfiguracaoRepository
    {
        public MenuConfiguracaoRepository(ContextBaseAdmin contextBaseAdmin)
            : base(contextBaseAdmin, "MenuConfiguracao")
        {
        }

        #region Métodos Específicos

        /// <summary>
        /// Busca todos os menus raiz (sem pai), ordenados por Ordem
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarMenusRaizAsync()
        {
            try
            {
                var menus = await BuscarPorFiltroAsync(m => m.MenuPaiId == null);

                return menus
                    .Where(m => m != null)
                    .OrderBy(m => m!.Ordem)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Erro ao buscar menus raiz: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Busca todos os menus (raiz e filhos), ordenados por Ordem
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarTodosMenusAsync()
        {
            try
            {
                var menus = await BuscarTodosAsync();

                return menus
                    .Where(m => m != null)
                    .OrderBy(m => m!.Ordem)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Erro ao buscar todos os menus: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Busca os submenus de um menu pai
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarSubmenusPorMenuPaiAsync(string menuPaiId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(menuPaiId))
                    throw new ArgumentException("MenuPaiId é obrigatório", nameof(menuPaiId));

                var submenus = await BuscarPorFiltroAsync(m => m.MenuPaiId == menuPaiId);

                return submenus
                    .Where(m => m != null)
                    .OrderBy(m => m!.Ordem)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Erro ao buscar submenus do menu pai '{menuPaiId}': {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Busca um menu por rota
        /// </summary>
        public async Task<MenuConfiguracao?> BuscarPorRotaAsync(string route)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(route))
                    throw new ArgumentException("Route é obrigatória", nameof(route));

                return await BuscarPrimeiroPorFiltroAsync(m => m.Route == route);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Erro ao buscar menu por rota '{route}': {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Busca todos os menus ativos (FlgAtivo = true e Disabled = false)
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarMenusAtivosAsync()
        {
            try
            {
                var menus = await BuscarPorFiltroAsync(m =>
                    m.FlgAtivo == true &&
                    m.Disabled == false);

                return menus
                    .Where(m => m != null)
                    .OrderBy(m => m!.Ordem)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Erro ao buscar menus ativos: {ex.Message}",
                    ex);
            }
        }

        #endregion
    }
}
