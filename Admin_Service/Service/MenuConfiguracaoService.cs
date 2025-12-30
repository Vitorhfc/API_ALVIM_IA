using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Admin_Service.ServiceGenerico;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.ADM;

namespace Admin_Service.Service
{
    /// <summary>
    /// Serviço para gerenciamento de MenuConfiguracao
    /// </summary>
    public class MenuConfiguracaoService : ServiceGenerico<MenuConfiguracao>, IMenuConfiguracaoService
    {
        #region Campos

        private readonly IMenuConfiguracaoRepository _menuConfiguracaoRepository;
        private readonly ILogger<MenuConfiguracaoService> _logger;

        #endregion

        #region Construtor

        public MenuConfiguracaoService(
            IMenuConfiguracaoRepository menuConfiguracaoRepository,
            ILogger<MenuConfiguracaoService> logger)
            : base(menuConfiguracaoRepository)
        {
            _menuConfiguracaoRepository = menuConfiguracaoRepository;
            _logger = logger;
        }

        #endregion

        #region Métodos Específicos

        /// <summary>
        /// Busca todos os menus raiz (sem pai), ordenados por Ordem
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarMenusRaizAsync()
        {
            try
            {
                _logger.LogInformation("Buscando menus raiz");
                return await _menuConfiguracaoRepository.BuscarMenusRaizAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar menus raiz");
                throw new ApplicationException($"Erro no serviço ao buscar menus raiz: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca todos os menus (raiz e filhos), ordenados por Ordem
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarTodosMenusAsync()
        {
            try
            {
                _logger.LogInformation("Buscando todos os menus");
                return await _menuConfiguracaoRepository.BuscarTodosMenusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os menus");
                throw new ApplicationException($"Erro no serviço ao buscar menus: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca os submenus de um menu pai
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarSubmenusPorMenuPaiAsync(string menuPaiId)
        {
            try
            {
                _logger.LogInformation("Buscando submenus do menu pai: {MenuPaiId}", menuPaiId);
                return await _menuConfiguracaoRepository.BuscarSubmenusPorMenuPaiAsync(menuPaiId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar submenus do menu pai: {MenuPaiId}", menuPaiId);
                throw new ApplicationException($"Erro no serviço ao buscar submenus: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca um menu por rota
        /// </summary>
        public async Task<MenuConfiguracao?> BuscarPorRotaAsync(string route)
        {
            try
            {
                _logger.LogInformation("Buscando menu por rota: {Route}", route);
                return await _menuConfiguracaoRepository.BuscarPorRotaAsync(route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar menu por rota: {Route}", route);
                throw new ApplicationException($"Erro no serviço ao buscar menu por rota: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Constrói a estrutura hierárquica de menus com seus filhos
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarMenusComHierarquiaAsync()
        {
            try
            {
                _logger.LogInformation("Construindo hierarquia de menus");

                // Buscar todos os menus
                var todosMenus = (await _menuConfiguracaoRepository.BuscarTodosMenusAsync()).ToList();

                // Separar menus raiz
                var menusRaiz = todosMenus.Where(m => m.MenuPaiId == null).OrderBy(m => m.Ordem).ToList();

                // Para cada menu raiz, carregar seus filhos recursivamente
                foreach (var menuRaiz in menusRaiz)
                {
                    menuRaiz.Filhos = CarregarFilhosRecursivo(menuRaiz.Id, todosMenus);
                }

                return menusRaiz;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao construir hierarquia de menus");
                throw new ApplicationException($"Erro no serviço ao construir hierarquia de menus: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Salva múltiplos menus de uma vez (útil para configuração inicial)
        /// </summary>
        public async Task<List<MenuConfiguracao>> SalvarMenusAsync(List<MenuConfiguracao> menus)
        {
            try
            {
                _logger.LogInformation("Salvando {Count} menus", menus.Count);

                if (menus == null || menus.Count == 0)
                    throw new ArgumentException("Lista de menus não pode ser vazia", nameof(menus));

                // Validar e preparar menus
                foreach (var menu in menus)
                {
                    menu.DtaCadastro = DateTime.Now;
                    menu.DtaAlteracao = DateTime.Now;
                    menu.FlgAtivo = true;
                }

                return await _menuConfiguracaoRepository.AdicionarArrayAsync(menus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar menus");
                throw new ApplicationException($"Erro no serviço ao salvar menus: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Alterna o status habilitado/desabilitado de um menu
        /// </summary>
        public async Task<MenuConfiguracao?> AlternarStatusAsync(string id)
        {
            try
            {
                _logger.LogInformation("Alternando status do menu: {MenuId}", id);

                var menu = await _menuConfiguracaoRepository.BuscarPorIdAsync(id);

                if (menu == null)
                {
                    _logger.LogWarning("Menu não encontrado para alternar status: {MenuId}", id);
                    return null;
                }

                // Alterna o status
                menu.Disabled = !menu.Disabled;
                menu.DtaAlteracao = DateTime.Now;

                return await _menuConfiguracaoRepository.EditarAsync(menu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alternar status do menu: {MenuId}", id);
                throw new ApplicationException($"Erro no serviço ao alternar status do menu: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca a estrutura hierárquica de menus ativos para exibição no menu lateral
        /// Retorna apenas menus com FlgAtivo = true e Disabled = false
        /// </summary>
        public async Task<IEnumerable<MenuConfiguracao>> BuscarMenuLateralAsync()
        {
            try
            {
                _logger.LogInformation("Construindo menu lateral");

                // Buscar apenas menus ativos
                var menusAtivos = (await _menuConfiguracaoRepository.BuscarMenusAtivosAsync()).ToList();

                // Separar menus raiz (sem pai)
                var menusRaiz = menusAtivos.Where(m => m.MenuPaiId == null).OrderBy(m => m.Ordem).ToList();

                // Para cada menu raiz, carregar seus filhos recursivamente
                foreach (var menuRaiz in menusRaiz)
                {
                    menuRaiz.Filhos = CarregarFilhosRecursivo(menuRaiz.Id, menusAtivos);
                }

                return menusRaiz;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao construir menu lateral");
                throw new ApplicationException($"Erro no serviço ao construir menu lateral: {ex.Message}", ex);
            }
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Carrega filhos de um menu recursivamente
        /// </summary>
        private List<MenuConfiguracao>? CarregarFilhosRecursivo(string menuPaiId, List<MenuConfiguracao> todosMenus)
        {
            var filhos = todosMenus
                .Where(m => m.MenuPaiId == menuPaiId)
                .OrderBy(m => m.Ordem)
                .ToList();

            if (filhos.Count == 0)
                return null;

            foreach (var filho in filhos)
            {
                filho.Filhos = CarregarFilhosRecursivo(filho.Id, todosMenus);
            }

            return filhos;
        }

        #endregion
    }
}
