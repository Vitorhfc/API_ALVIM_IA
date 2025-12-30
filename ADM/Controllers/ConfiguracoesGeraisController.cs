using ADM.Controllers.Base;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.ADM;

namespace ADM_Api.Controllers
{
    [ApiController]
    [Route("api/MenuConfiguracao")]
    [Authorize]
    public class ConfiguracoesGeraisController : ControllerBaseComplemento<ConfiguracoesGeraisController>
    {
        #region Campos

        private readonly IMenuConfiguracaoService _menuConfiguracaoService;

        #endregion

        #region Construtor

        public ConfiguracoesGeraisController(
            IMenuConfiguracaoService menuConfiguracaoService,
            ILogADMService logADMService,
            ILogger<ConfiguracoesGeraisController> logger)
            : base(logADMService, logger)
        {
            _menuConfiguracaoService = menuConfiguracaoService;
        }

        #endregion

        #region Endpoints Menu

        /// <summary>
        /// Busca todos os menus (sem hierarquia, lista plana)
        /// GET /api/MenuConfiguracao/menu
        /// </summary>
        [HttpGet("menu")]
        public async Task<IActionResult> BuscarTodosMenus()
        {
            try
            {
                var menus = await _menuConfiguracaoService.BuscarTodosMenusAsync();

                await LogInfoAsync(
                    $"Buscou {menus.Count()} menus",
                    nameof(BuscarTodosMenus));

                return Sucesso(menus, "Menus obtidos com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarTodosMenus));
            }
        }

        /// <summary>
        /// Busca um menu específico por ID
        /// GET /api/MenuConfiguracao/menu/{id}
        /// </summary>
        /// <param name="id">ID do menu</param>
        [HttpGet("menu/{id}")]
        public async Task<IActionResult> BuscarMenuPorId(string id)
        {
            try
            {
                var menu = await _menuConfiguracaoService.BuscarPorIdAsync(id);

                if (menu == null)
                {
                    await LogInfoAsync($"Menu não encontrado: {id}", nameof(BuscarMenuPorId));
                    return Erro("Menu não encontrado");
                }

                await LogInfoAsync($"Buscou menu: {menu.Label}", nameof(BuscarMenuPorId));

                return Sucesso(menu, "Menu encontrado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarMenuPorId), $"ID: {id}");
            }
        }

        /// <summary>
        /// Cria ou atualiza um menu
        /// POST /api/MenuConfiguracao/menu
        /// Se o menu tiver ID, atualiza; caso contrário, cria um novo
        /// </summary>
        /// <param name="menu">Dados do menu</param>
        [HttpPost("menu")]
        public async Task<IActionResult> SalvarMenu([FromBody] MenuConfiguracao menu)
        {
            try
            {
                if (menu == null)
                    return Erro("Dados do menu são obrigatórios");

                // Se tem ID, atualiza
                if (!string.IsNullOrEmpty(menu.Id))
                {
                    menu.DtaAlteracao = DateTime.Now;

                    var menuAntigo = await _menuConfiguracaoService.BuscarPorIdAsync(menu.Id);
                    if (menuAntigo == null)
                        return Erro("Menu não encontrado");

                    var menuAtualizado = await _menuConfiguracaoService.EditarAsync(menu);

                    await RegistraAcaoAsync(
                        "Atualizar Menu",
                        SerializarParaLog(menuAntigo),
                        SerializarParaLog(menuAtualizado),
                        $"Menu {menu.Label} atualizado com sucesso");

                    return Sucesso(menuAtualizado, "Menu atualizado com sucesso");
                }
                // Se não tem ID, cria novo
                else
                {
                    var menuCriado = await _menuConfiguracaoService.AdicionarAsync(menu);

                    await RegistraAcaoAsync(
                        "Criar Menu",
                        "Novo menu",
                        SerializarParaLog(menuCriado),
                        $"Menu {menu.Label} criado com sucesso");

                    return Sucesso(menuCriado, "Menu criado com sucesso");
                }
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(SalvarMenu), SerializarParaLog(menu));
            }
        }

        /// <summary>
        /// Alterna o status habilitado/desabilitado de um menu
        /// POST /api/MenuConfiguracao/menu/alternar-status
        /// </summary>
        /// <param name="request">Request contendo o ID do menu</param>
        [HttpPost("menu/alternar-status")]
        public async Task<IActionResult> AlternarStatus([FromBody] AlternarStatusRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Id))
                    return Erro("ID do menu é obrigatório");

                var menuAtualizado = await _menuConfiguracaoService.AlternarStatusAsync(request.Id);

                if (menuAtualizado == null)
                    return Erro("Menu não encontrado");

                await RegistraAcaoAsync(
                    "Alternar Status Menu",
                    $"Status anterior: {!menuAtualizado.Disabled}",
                    $"Status novo: {menuAtualizado.Disabled}",
                    $"Status do menu {menuAtualizado.Label} alterado para {(menuAtualizado.Disabled ? "desabilitado" : "habilitado")}");

                return Sucesso(menuAtualizado, $"Menu {(menuAtualizado.Disabled ? "desabilitado" : "habilitado")} com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(AlternarStatus), $"ID: {request?.Id}");
            }
        }

        /// <summary>
        /// Remove um menu
        /// DELETE /api/MenuConfiguracao/menu/{id}
        /// </summary>
        /// <param name="id">ID do menu</param>
        [HttpDelete("menu/{id}")]
        public async Task<IActionResult> RemoverMenu(string id)
        {
            try
            {
                var menu = await _menuConfiguracaoService.BuscarPorIdAsync(id);
                if (menu == null)
                    return Erro("Menu não encontrado");

                await _menuConfiguracaoService.ExcluirPorIdAsync(id);

                await RegistraAcaoAsync(
                    "Remover Menu",
                    SerializarParaLog(menu),
                    "Menu removido",
                    $"Menu {menu.Label} removido com sucesso");

                return Sucesso(null, "Menu removido com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(RemoverMenu), $"ID: {id}");
            }
        }

        /// <summary>
        /// Reordena múltiplos menus
        /// POST /api/MenuConfiguracao/menu/reordenar
        /// </summary>
        /// <param name="request">Lista de IDs e novas ordens</param>
        [HttpPost("menu/reordenar")]
        public async Task<IActionResult> ReordenarMenus([FromBody] List<ReordenarMenuRequest> request)
        {
            try
            {
                if (request == null || request.Count == 0)
                    return Erro("Lista de menus é obrigatória");

                // Buscar e atualizar cada menu com a nova ordem
                foreach (var item in request)
                {
                    var menu = await _menuConfiguracaoService.BuscarPorIdAsync(item.Id);
                    if (menu != null)
                    {
                        menu.Ordem = item.Ordem;
                        menu.DtaAlteracao = DateTime.Now;
                        await _menuConfiguracaoService.EditarAsync(menu);
                    }
                }

                await RegistraAcaoAsync(
                    "Reordenar Menus",
                    "Ordem anterior",
                    SerializarParaLog(request),
                    $"Reordenação de {request.Count} menus realizada com sucesso");

                return Sucesso(null, "Menus reordenados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ReordenarMenus), SerializarParaLog(request));
            }
        }

        /// <summary>
        /// Sincroniza a lista completa de menus (reordena e remove itens não enviados)
        /// POST /api/MenuConfiguracao/menu/sincronizar
        /// </summary>
        /// <param name="request">Lista completa de menus atualizada</param>
        [HttpPost("menu/sincronizar")]
        public async Task<IActionResult> SincronizarMenus([FromBody] SincronizarMenusRequest request)
        {
            try
            {
                if (request?.Menus == null)
                    return Erro("Lista de menus é obrigatória");

                // Buscar todos os menus existentes
                var menusExistentes = (await _menuConfiguracaoService.BuscarTodosMenusAsync()).ToList();

                // IDs dos menus enviados na requisição
                var idsEnviados = request.Menus.Where(m => !string.IsNullOrEmpty(m.Id)).Select(m => m.Id).ToHashSet();

                // Remover menus que não estão na lista enviada
                var menusParaRemover = menusExistentes.Where(m => !idsEnviados.Contains(m.Id)).ToList();
                foreach (var menuRemover in menusParaRemover)
                {
                    await _menuConfiguracaoService.ExcluirPorIdAsync(menuRemover.Id);
                }

                var menusAtualizados = new List<MenuConfiguracao>();
                var menusCriados = new List<MenuConfiguracao>();

                // Processar menus enviados
                foreach (var menuRequest in request.Menus)
                {
                    // Se tem ID, atualiza
                    if (!string.IsNullOrEmpty(menuRequest.Id))
                    {
                        var menuExistente = await _menuConfiguracaoService.BuscarPorIdAsync(menuRequest.Id);
                        if (menuExistente != null)
                        {
                            menuExistente.Icone = menuRequest.Icone;
                            menuExistente.Label = menuRequest.Label;
                            menuExistente.Route = menuRequest.Route;
                            menuExistente.Disabled = menuRequest.Disabled;
                            menuExistente.Ordem = menuRequest.Ordem;
                            menuExistente.MenuPaiId = menuRequest.MenuPaiId;
                            menuExistente.DtaAlteracao = DateTime.Now;

                            var menuAtualizado = await _menuConfiguracaoService.EditarAsync(menuExistente);
                            if (menuAtualizado != null)
                                menusAtualizados.Add(menuAtualizado);
                        }
                    }
                    // Se não tem ID, cria novo
                    else
                    {
                        var novoMenu = new MenuConfiguracao
                        {
                            Icone = menuRequest.Icone,
                            Label = menuRequest.Label,
                            Route = menuRequest.Route,
                            Disabled = menuRequest.Disabled,
                            Ordem = menuRequest.Ordem,
                            MenuPaiId = menuRequest.MenuPaiId,
                            DtaCadastro = DateTime.Now,
                            DtaAlteracao = DateTime.Now,
                            FlgAtivo = true
                        };

                        var menuCriado = await _menuConfiguracaoService.AdicionarAsync(novoMenu);
                        if (menuCriado != null)
                            menusCriados.Add(menuCriado);
                    }
                }

                await RegistraAcaoAsync(
                    "Sincronizar Menus",
                    $"Menus anteriores: {menusExistentes.Count}",
                    SerializarParaLog(new
                    {
                        removidos = menusParaRemover.Count,
                        atualizados = menusAtualizados.Count,
                        criados = menusCriados.Count,
                        total = request.Menus.Count
                    }),
                    $"Sincronização completa: {menusParaRemover.Count} removidos, {menusAtualizados.Count} atualizados, {menusCriados.Count} criados");

                return Sucesso(
                    new
                    {
                        removidos = menusParaRemover.Count,
                        atualizados = menusAtualizados.Count,
                        criados = menusCriados.Count,
                        total = menusAtualizados.Count + menusCriados.Count
                    },
                    "Menus sincronizados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(SincronizarMenus), SerializarParaLog(request));
            }
        }

        /// <summary>
        /// Cria múltiplos menus de uma vez (cadastro em lote)
        /// POST /api/MenuConfiguracao/menu/lote
        /// </summary>
        /// <param name="request">Lista de menus a serem criados</param>
        [HttpPost("menu/lote")]
        public async Task<IActionResult> CriarMenusEmLote([FromBody] CriarMenusLoteRequest request)
        {
            try
            {
                if (request?.Menus == null || request.Menus.Count == 0)
                    return Erro("Lista de menus é obrigatória");

                // Preparar menus para cadastro
                foreach (var menu in request.Menus)
                {
                    menu.DtaCadastro = DateTime.Now;
                    menu.DtaAlteracao = DateTime.Now;
                    menu.FlgAtivo = true;
                }

                var menusCriados = await _menuConfiguracaoService.SalvarMenusAsync(request.Menus);

                await RegistraAcaoAsync(
                    "Criar Menus em Lote",
                    "Novo cadastro",
                    SerializarParaLog(menusCriados),
                    $"Cadastro em lote de {menusCriados.Count} menus");

                return Sucesso(menusCriados, $"{menusCriados.Count} menus criados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(CriarMenusEmLote), SerializarParaLog(request));
            }
        }

        /// <summary>
        /// Carrega a estrutura hierárquica do menu lateral com apenas os menus ativos
        /// GET /api/MenuConfiguracao/CarregarMenuLateral
        /// Retorna apenas menus com FlgAtivo = true e Disabled = false
        /// </summary>
        [HttpGet("CarregarMenuLateral")]
        public async Task<IActionResult> CarregarMenuLateral()
        {
            try
            {
                var menuLateral = await _menuConfiguracaoService.BuscarMenuLateralAsync();

                await LogInfoAsync(
                    $"Menu lateral carregado com {menuLateral.Count()} itens raiz",
                    nameof(CarregarMenuLateral));

                return Sucesso(menuLateral, "Menu lateral carregado com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(CarregarMenuLateral));
            }
        }

        #endregion
    }

    #region DTOs

    /// <summary>
    /// Request para reordenar menus
    /// </summary>
    public class ReordenarMenuRequest
    {
        /// <summary>
        /// ID do menu
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Nova ordem do menu
        /// </summary>
        public int Ordem { get; set; }
    }

    /// <summary>
    /// Request para sincronizar menus (reordenar e remover itens não enviados)
    /// </summary>
    public class SincronizarMenusRequest
    {
        /// <summary>
        /// Lista completa de menus atualizada (com IDs para atualizar, sem IDs para criar)
        /// </summary>
        public List<MenuConfiguracao> Menus { get; set; } = new();
    }

    /// <summary>
    /// Request para criar menus em lote
    /// </summary>
    public class CriarMenusLoteRequest
    {
        /// <summary>
        /// Lista de menus a serem criados
        /// </summary>
        public List<MenuConfiguracao> Menus { get; set; } = new();
    }

    /// <summary>
    /// Request para alternar status de um menu
    /// </summary>
    public class AlternarStatusRequest
    {
        /// <summary>
        /// ID do menu
        /// </summary>
        public string Id { get; set; } = string.Empty;
    }

    #endregion
}
