using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClienteController : ControllerBaseClient<ClienteController>
    {
        #region Campos

        private readonly IClienteService _clienteService;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<ClienteController> _logger;
        private readonly IClienteEmpresaMapService _clienteEmpresaMapService;
        private readonly Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService _contextoMultiTenant;
        private readonly Client_Repository.Repositorio.Interface.IMensagemRepositorio _mensagemRepositorio;
        #endregion

        #region Construtor

        public ClienteController(
            IClienteService clienteService,
            ILogClientService logClientService,
            ILogger<ClienteController> logger,
            IClienteEmpresaMapService clienteEmpresaMapService,
            Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService contextoMultiTenant,
            Client_Repository.Repositorio.Interface.IMensagemRepositorio mensagemRepositorio)
            : base(logClientService, logger)
        {
            _clienteService = clienteService;
            _logClientService = logClientService;
            _logger = logger;
            _clienteEmpresaMapService = clienteEmpresaMapService;
            _contextoMultiTenant = contextoMultiTenant;
            _mensagemRepositorio = mensagemRepositorio;
        }

        #endregion

        #region Endpoints - Consultas

        [HttpGet]
        public async Task<IActionResult> ListarClientes()
        {
            try
            {
                var clientes = await _clienteService.BuscarTodosAsync();

                await LogInfoAsync($"Listou {clientes.Count()} clientes", nameof(ListarClientes));

                return Sucesso(clientes, "Clientes listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarClientes));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarClientePorId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do cliente é obrigatório");

                var cliente = await _clienteService.BuscarPorIdAsync(id);

                if (cliente == null)
                {
                    await LogInfoAsync($"Tentativa de buscar cliente inexistente: {id}", nameof(BuscarClientePorId));
                    return NaoEncontrado("Cliente não encontrado");
                }

                await LogInfoAsync($"Buscou cliente: {cliente.Nome}", nameof(BuscarClientePorId));

                return Sucesso(cliente, "Cliente encontrado");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarClientePorId), $"ID: {id}");
            }
        }

        [HttpGet("ativos")]
        public async Task<IActionResult> ListarClientesAtivos()
        {
            try
            {
                var clientes = await _clienteService.BuscarPorFiltroAsync(c => c.FlgAtivo == true);

                await LogInfoAsync($"Listou {clientes.Count()} clientes ativos", nameof(ListarClientesAtivos));

                return Sucesso(clientes, "Clientes ativos listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarClientesAtivos));
            }
        }

        [HttpGet("estatisticas")]
        public async Task<IActionResult> ObterEstatisticas()
        {
            try
            {
                var totalClientes = await _clienteService.BuscarContagemTotalAsync();
                var clientesAtivos = await _clienteService.BuscarContagemTotalPorFiltroAsync(c => c.FlgAtivo == true);
                var clientesInativos = totalClientes - clientesAtivos;

                var estatisticas = new
                {
                    totalClientes,
                    clientesAtivos,
                    clientesInativos,
                    percentualAtivos = totalClientes > 0 ? (clientesAtivos * 100.0 / totalClientes) : 0
                };

                await LogInfoAsync("Consultou estatísticas de clientes", nameof(ObterEstatisticas));

                return Sucesso(estatisticas, "Estatísticas obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterEstatisticas));
            }
        }

        /// <summary>
        /// Busca a quantidade de mensagens trocadas com um cliente
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <returns>Quantidade total de mensagens e detalhamento por tipo</returns>
        [HttpGet("{id}/mensagens/contagem")]
        public async Task<IActionResult> ObterContagemMensagensDoCliente(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do cliente é obrigatório");

                var cliente = await _clienteService.BuscarPorIdAsync(id);
                if (cliente == null)
                {
                    await LogInfoAsync($"Tentativa de buscar contagem de mensagens de cliente inexistente: {id}", nameof(ObterContagemMensagensDoCliente));
                    return NaoEncontrado("Cliente não encontrado");
                }

                // Buscar todas as mensagens do cliente
                var mensagens = await _mensagemRepositorio.BuscarPorFiltroAsync(m => m.ClienteId == id);

                var totalMensagens = mensagens.Count();
                var mensagensCliente = mensagens.Count(m => m.FlgMensagemCliente);
                var mensagensResponsavel = mensagens.Count(m => !m.FlgMensagemCliente);

                var resultado = new
                {
                    clienteId = id,
                    clienteNome = cliente.Nome,
                    totalMensagens = totalMensagens,
                    mensagensCliente = mensagensCliente,
                    mensagensResponsavel = mensagensResponsavel
                };

                await LogInfoAsync($"Contagem de mensagens do cliente {cliente.Nome}: {totalMensagens} total", nameof(ObterContagemMensagensDoCliente));

                return Sucesso(resultado, "Contagem de mensagens obtida com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterContagemMensagensDoCliente), $"ID: {id}");
            }
        }

        /// <summary>
        /// Busca todas as mensagens de um cliente
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <param name="limite">Número máximo de mensagens a retornar (padrão: todas)</param>
        /// <param name="ordenacao">Ordenação: 'asc' (mais antiga primeiro) ou 'desc' (mais recente primeiro). Padrão: 'asc'</param>
        /// <returns>Lista de mensagens do cliente</returns>
        [HttpGet("{id}/mensagens")]
        public async Task<IActionResult> BuscarMensagensDoCliente(
            string id,
            [FromQuery] int? limite = null,
            [FromQuery] string ordenacao = "asc")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do cliente é obrigatório");

                var cliente = await _clienteService.BuscarPorIdAsync(id);
                if (cliente == null)
                {
                    await LogInfoAsync($"Tentativa de buscar mensagens de cliente inexistente: {id}", nameof(BuscarMensagensDoCliente));
                    return NaoEncontrado("Cliente não encontrado");
                }

                IEnumerable<Shared.Classes.Entidades.Client.Mensagem> mensagens;

                if (limite.HasValue && limite.Value > 0)
                {
                    // Buscar últimas N mensagens
                    mensagens = await _mensagemRepositorio.BuscarUltimasMensagensAsync(id, limite.Value);
                }
                else
                {
                    // Buscar todas as mensagens
                    mensagens = await _mensagemRepositorio.BuscarPorFiltroAsync(m => m.ClienteId == id);

                    // Ordenar por data de recebimento
                    mensagens = ordenacao?.ToLower() == "desc"
                        ? mensagens.OrderByDescending(m => m.DtRecebido)
                        : mensagens.OrderBy(m => m.DtRecebido);
                }

                var totalMensagens = mensagens.Count();
                var mensagensCliente = mensagens.Count(m => m.FlgMensagemCliente);
                var mensagensResponsavel = mensagens.Count(m => !m.FlgMensagemCliente);

                var resultado = new
                {
                    clienteId = id,
                    clienteNome = cliente.Nome,
                    totalMensagens = totalMensagens,
                    mensagensCliente = mensagensCliente,
                    mensagensResponsavel = mensagensResponsavel,
                    mensagens = mensagens
                };

                await LogInfoAsync($"Buscou {totalMensagens} mensagens do cliente: {cliente.Nome}", nameof(BuscarMensagensDoCliente));

                return Sucesso(resultado, $"{totalMensagens} mensagem(ns) encontrada(s)");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarMensagensDoCliente), $"ID: {id}");
            }
        }

        #endregion

        #region Endpoints - Operações

        [HttpPost]
        public async Task<IActionResult> CriarCliente([FromBody] Shared.Classes.Entidades.Client.Cliente cliente)
        {
            try
            {
                if (cliente == null)
                    return Erro("Dados do cliente são obrigatórios");

                cliente.FlgAtivo = true;
                cliente.DtaCadastro = DateTime.UtcNow;
                cliente.DtaAlteracao = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(cliente.Email))
                    cliente.Email = cliente.Email.ToLower();

                if (!await _clienteService.ValidarTelefoneUnicoAsync(cliente.Numero))
                    return Erro("Já existe um cliente cadastrado com este telefone");

                var clienteCriado = await _clienteService.AdicionarAsync(cliente);

                // Sincronizar mapeamento no banco Admin
                var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                if (!string.IsNullOrWhiteSpace(empresaId))
                {
                    await _clienteEmpresaMapService.SincronizarMapeamentoAsync(
                        clienteCriado.Id,
                        empresaId,
                        clienteCriado.NumeroTelefoneWaha,
                        clienteCriado.Nome
                    );
                }

                await RegistraAcaoAsync(
                    "Criar Cliente",
                    "Novo cliente",
                    SerializarParaLog(clienteCriado),
                    $"Cliente {cliente.Nome} criado com sucesso");

                return Criado($"/api/cliente/{clienteCriado.Id}", clienteCriado, "Cliente criado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(CriarCliente), SerializarParaLog(cliente));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarCliente(string id, [FromBody] Shared.Classes.Entidades.Client.Cliente cliente)
        {
            try
            {
                if (cliente == null)
                    return Erro("Dados do cliente são obrigatórios");

                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do cliente é obrigatório");

                var clienteAntigo = await _clienteService.BuscarPorIdAsync(id);
                if (clienteAntigo == null)
                    return NaoEncontrado("Cliente não encontrado");

                cliente.Id = id;
                cliente.DtaAlteracao = DateTime.UtcNow;
                cliente.DtaCadastro = clienteAntigo.DtaCadastro;

                if (!string.IsNullOrWhiteSpace(cliente.Email))
                    cliente.Email = cliente.Email.ToLower();

                if (!await _clienteService.ValidarTelefoneUnicoAsync(cliente.Numero, id))
                    return Erro("Já existe outro cliente cadastrado com este telefone");

                var clienteAtualizado = await _clienteService.EditarAsync(cliente);

                // Sincronizar mapeamento no banco Admin (caso nome ou número tenham mudado)
                var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                if (!string.IsNullOrWhiteSpace(empresaId))
                {
                    await _clienteEmpresaMapService.SincronizarMapeamentoAsync(
                        clienteAtualizado.Id,
                        empresaId,
                        clienteAtualizado.NumeroTelefoneWaha,
                        clienteAtualizado.Nome
                    );
                }

                await RegistraAcaoAsync(
                    "Atualizar Cliente",
                    SerializarParaLog(clienteAntigo),
                    SerializarParaLog(clienteAtualizado),
                    $"Cliente {cliente.Nome} atualizado com sucesso");

                return Sucesso(clienteAtualizado, "Cliente atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AtualizarCliente), $"ID: {id}, Dados: {SerializarParaLog(cliente)}");
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> AlterarStatusCliente(string id, [FromQuery] bool ativo)
        {
            try
            {
                var cliente = await _clienteService.BuscarPorIdAsync(id);
                if (cliente == null)
                    return NaoEncontrado("Cliente não encontrado");

                var statusAnterior = cliente.FlgAtivo;
                cliente.FlgAtivo = ativo;
                cliente.DtaAlteracao = DateTime.UtcNow;

                await _clienteService.EditarAsync(cliente);

                var acao = ativo ? "Ativar Cliente" : "Inativar Cliente";
                var mensagem = ativo ? "ativado" : "inativado";

                await RegistraAcaoAsync(
                    acao,
                    $"Cliente {cliente.Nome} {(statusAnterior ? "ativo" : "inativo")}",
                    $"Cliente {cliente.Nome} {(ativo ? "ativo" : "inativo")}",
                    $"Cliente {cliente.Nome} foi {mensagem}");

                return Sucesso(cliente, $"Cliente {mensagem} com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AlterarStatusCliente), $"ID: {id}, Ativo: {ativo}");
            }
        }

        [HttpPatch("{id}/resposta-responsavel")]
        public async Task<IActionResult> AlterarModoRespostaResponsavel(string id, [FromQuery] bool ativo)
        {
            try
            {
                var cliente = await _clienteService.BuscarPorIdAsync(id);
                if (cliente == null)
                    return NaoEncontrado("Cliente não encontrado");

                var statusAnterior = cliente.FlgRespostaResponsavel;
                cliente.FlgRespostaResponsavel = ativo;
                cliente.DtaAlteracao = DateTime.UtcNow;

                if (ativo)
                {
                    cliente.DtFlgResponsavelAtiva = DateTime.UtcNow;
                    cliente.DtFlgResponsavelDesativada = null;
                }
                else
                {
                    cliente.DtFlgResponsavelDesativada = DateTime.UtcNow;
                }

                await _clienteService.EditarAsync(cliente);

                var acao = ativo ? "Ativar Resposta por Responsável" : "Desativar Resposta por Responsável";
                var mensagem = ativo ? "ativado" : "desativado";

                await RegistraAcaoAsync(
                    acao,
                    $"Cliente {cliente.Nome} - Flag resposta responsável {(statusAnterior ? "ativa" : "inativa")}",
                    $"Cliente {cliente.Nome} - Flag resposta responsável {(ativo ? "ativa" : "inativa")}",
                    $"Modo de resposta por responsável foi {mensagem} para o cliente {cliente.Nome}");

                _logger.LogInformation("Modo de resposta por responsável {Acao} para cliente {ClienteId} - {ClienteNome}",
                    acao, cliente.Id, cliente.Nome);

                return Sucesso(cliente, $"Modo de resposta por responsável {mensagem} com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AlterarModoRespostaResponsavel), $"ID: {id}, Ativo: {ativo}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoverCliente(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do cliente é obrigatório");

                var cliente = await _clienteService.BuscarPorIdAsync(id);
                if (cliente == null)
                    return NaoEncontrado("Cliente não encontrado");

                await _clienteService.ExcluirAsync(cliente);

                await RegistraAcaoAsync(
                    "Remover Cliente",
                    SerializarParaLog(cliente),
                    "Cliente removido",
                    $"Cliente {cliente.Nome} removido permanentemente");

                return Sucesso(null, "Cliente removido com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(RemoverCliente), $"ID: {id}");
            }
        }

        #endregion
    }
}