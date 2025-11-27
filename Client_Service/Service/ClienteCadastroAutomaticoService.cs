using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;
using Shared.Messaging.Models;
using Shared.Services.Interface;
using Shared.Utils;

namespace Client_Service.Service
{
    /// <summary>
    /// Serviço responsável pelo cadastro automático de clientes via webhook
    /// </summary>
    public class ClienteCadastroAutomaticoService : IClienteCadastroAutomaticoService
    {
        #region Campos

        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly ILogger<ClienteCadastroAutomaticoService> _logger;
        private readonly IClienteEmpresaMapService _clienteEmpresaMapService;
        private readonly Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService _contextoMultiTenant;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IConfiguration _configuration;
        private readonly Admin_Repository.Repositorio.Interface.IEmpresaRepository _empresaRepository;

        // Flag para garantir que o índice seja criado apenas uma vez por tenant
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _indicesCriados = new();

        #endregion

        #region Construtor

        public ClienteCadastroAutomaticoService(
            IClienteRepositorio clienteRepositorio,
            ILogger<ClienteCadastroAutomaticoService> logger,
            IClienteEmpresaMapService clienteEmpresaMapService,
            Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService contextoMultiTenant,
            IWhatsAppService whatsAppService,
            IConfiguration configuration,
            Admin_Repository.Repositorio.Interface.IEmpresaRepository empresaRepository)
        {
            _clienteRepositorio = clienteRepositorio;
            _logger = logger;
            _clienteEmpresaMapService = clienteEmpresaMapService;
            _contextoMultiTenant = contextoMultiTenant;
            _whatsAppService = whatsAppService;
            _configuration = configuration;
            _empresaRepository = empresaRepository;
        }

        #endregion

        #region Métodos Públicos

        public async Task<Cliente> BuscarOuCriarClienteAsync(StandardWhatsAppEvent webhookEvent)
        {
            try
            {
                if (webhookEvent?.ContactInfo == null)
                {
                    _logger.LogWarning("Webhook sem informações de contato");
                    throw new ArgumentException("Webhook não contém informações de contato");
                }

                // Extrair informações do número
                var numeroInfo = TelefoneHelper.ExtrairInformacoes(webhookEvent.ContactInfo.PhoneNumber);

                _logger.LogInformation(
                    "Buscando ou criando cliente - Número WAHA: {NumeroWaha}, JID: {Jid}",
                    numeroInfo.NumeroWaha,
                    numeroInfo.JidCompleto
                );

                // Garantir que o índice único existe (executa apenas uma vez por tenant)
                await GarantirIndiceUnicoAsync();

                // ✅ NOVO: Obter informações do contato na API WAHA ANTES de criar o cliente
                WahaContactInfo? contatoWaha = null;
                try
                {
                    var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                    if (!string.IsNullOrWhiteSpace(empresaId))
                    {
                        var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);
                        if (empresa != null && !string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                        {
                            var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                            var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                            if (!string.IsNullOrWhiteSpace(wahaApiUrl) && !string.IsNullOrWhiteSpace(wahaApiKey))
                            {
                                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(96));

                                contatoWaha = await _whatsAppService.ObterInformacoesContatoAsync(
                                    webhookEvent.ContactInfo.PhoneNumber,
                                    empresa.WahaSessionName,
                                    wahaApiUrl,
                                    wahaApiKey,
                                    cts.Token
                                );

                                if (contatoWaha != null)
                                {
                                    _logger.LogInformation(
                                        "✅ Informações do contato obtidas da API WAHA - Número: {Numero}, Nome: {Nome}, PushName: {PushName}",
                                        numeroInfo.NumeroWaha,
                                        contatoWaha.Name,
                                        contatoWaha.PushName
                                    );
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(
                        "Timeout ao consultar WAHA (5s) - Número: {Numero}. Continuando com dados do webhook.",
                        numeroInfo.NumeroWaha
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Erro ao consultar API WAHA - Número: {Numero}. Continuando com dados do webhook.",
                        numeroInfo.NumeroWaha
                    );
                }

                // Criar objeto do cliente com informações da WAHA (se disponível) ou do webhook
                var novoCliente = await CriarNovoClienteAsync(webhookEvent, numeroInfo, contatoWaha);

                // Usar operação atômica (FindOneAndUpdate com upsert)
                // Isso evita duplicação mesmo com condições de corrida
                var cliente = await _clienteRepositorio.BuscarOuCriarClienteAtomicoAsync(novoCliente);

                var foiCriado = cliente.DtPrimeiroContato >= DateTime.UtcNow.AddSeconds(-2);

                if (foiCriado)
                {
                    _logger.LogInformation(
                        "✅ NOVO Cliente criado de forma atômica - ID: {ClienteId}, Nome: {Nome}, Número: {Numero}",
                        cliente.Id,
                        cliente.Nome,
                        cliente.NumeroTelefoneWaha
                    );

                    // Sincronizar mapeamento no banco Admin (apenas para novos clientes)
                    var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                    if (!string.IsNullOrWhiteSpace(empresaId))
                    {
                        await _clienteEmpresaMapService.SincronizarMapeamentoAsync(
                            cliente.Id,
                            empresaId,
                            cliente.NumeroTelefoneWaha,
                            cliente.Nome
                        );
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "✅ Cliente EXISTENTE encontrado - ID: {ClienteId}, Nome: {Nome}, Número: {Numero}",
                        cliente.Id,
                        cliente.Nome,
                        cliente.NumeroTelefoneWaha
                    );
                }

                return cliente;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao buscar ou criar cliente - Número: {Numero}",
                    webhookEvent?.ContactInfo?.PhoneNumber
                );
                throw;
            }
        }

        public async Task<bool> AtualizarInformacoesClienteAsync(Cliente cliente, StandardWhatsAppEvent webhookEvent)
        {
            try
            {
                var houveMudanca = false;
                var numeroInfo = TelefoneHelper.ExtrairInformacoes(webhookEvent.ContactInfo.PhoneNumber);

                // 1. Atualizar nome se veio diferente e não está vazio
                var nomeWebhook = ExtrairNomeDoContato(webhookEvent);
                if (!string.IsNullOrWhiteSpace(nomeWebhook) && cliente.Nome != nomeWebhook)
                {
                    _logger.LogInformation(
                        "Atualizando nome do cliente {ClienteId}: '{NomeAntigo}' → '{NomeNovo}'",
                        cliente.Id,
                        cliente.Nome,
                        nomeWebhook
                    );

                    cliente.Nome = nomeWebhook;
                    houveMudanca = true;
                }

                // 2. Atualizar NumeroTelefoneWaha se não estava preenchido
                if (string.IsNullOrWhiteSpace(cliente.NumeroTelefoneWaha))
                {
                    _logger.LogInformation(
                        "Adicionando NumeroTelefoneWaha ao cliente {ClienteId}: {NumeroWaha}",
                        cliente.Id,
                        numeroInfo.NumeroWaha
                    );

                    cliente.NumeroTelefoneWaha = numeroInfo.NumeroWaha;
                    houveMudanca = true;
                }

                // 3. Atualizar Numero formatado se não estava preenchido
                if (string.IsNullOrWhiteSpace(cliente.Numero))
                {
                    cliente.Numero = numeroInfo.NumeroFormatado;
                    houveMudanca = true;
                }

                // 4. Atualizar NumeroInterno (JID) se mudou
                if (cliente.NumeroInterno != numeroInfo.JidCompleto)
                {
                    _logger.LogInformation(
                        "Atualizando JID do cliente {ClienteId}: '{JidAntigo}' → '{JidNovo}'",
                        cliente.Id,
                        cliente.NumeroInterno,
                        numeroInfo.JidCompleto
                    );

                    cliente.NumeroInterno = numeroInfo.JidCompleto;
                    houveMudanca = true;
                }

                // 5. Atualizar última interação
                cliente.DtUltimaInteracao = DateTime.UtcNow;
                houveMudanca = true;

                // 6. Salvar se houve mudança
                if (houveMudanca)
                {
                    await _clienteRepositorio.AtualizarAsync(cliente);

                    _logger.LogInformation(
                        "Informações do cliente {ClienteId} atualizadas com sucesso",
                        cliente.Id
                    );

                    // Sincronizar mapeamento no banco Admin (pode ter mudado o nome ou número)
                    var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                    if (!string.IsNullOrWhiteSpace(empresaId))
                    {
                        await _clienteEmpresaMapService.SincronizarMapeamentoAsync(
                            cliente.Id,
                            empresaId,
                            cliente.NumeroTelefoneWaha,
                            cliente.Nome
                        );
                    }
                }

                return houveMudanca;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao atualizar informações do cliente {ClienteId}",
                    cliente?.Id
                );
                throw;
            }
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Garante que o índice único existe no banco do tenant atual
        /// Usa cache para evitar múltiplas chamadas ao MongoDB
        /// </summary>
        private async Task GarantirIndiceUnicoAsync()
        {
            var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
            if (string.IsNullOrWhiteSpace(empresaId))
            {
                _logger.LogWarning("EmpresaId não encontrada. Não foi possível garantir índice único.");
                return;
            }

            // Verifica se já criamos o índice para este tenant
            if (_indicesCriados.ContainsKey(empresaId))
            {
                return; // Índice já foi criado para este tenant
            }

            try
            {
                _logger.LogInformation("Criando índice único para NumeroTelefoneWaha - EmpresaId: {EmpresaId}", empresaId);
                await _clienteRepositorio.CriarIndiceUnicoNumeroWahaAsync();

                // Marca como criado para este tenant
                _indicesCriados.TryAdd(empresaId, true);

                _logger.LogInformation("✅ Índice único criado com sucesso - EmpresaId: {EmpresaId}", empresaId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Erro ao criar índice único para EmpresaId: {EmpresaId}. O método atômico ainda funcionará.",
                    empresaId
                );
            }
        }

        private async Task<Cliente> CriarNovoClienteAsync(
            StandardWhatsAppEvent webhookEvent,
            NumeroInfo numeroInfo,
            WahaContactInfo? contatoWaha = null)
        {
            string nome;
            string? fotoPerfil = null;

            // Usar informações da WAHA se disponíveis, senão usa webhook
            if (contatoWaha != null)
            {
                nome = contatoWaha.NomeExibicao; // Name tem prioridade, depois PushName

                _logger.LogInformation(
                    "Usando informações da API WAHA - Nome: {Nome} (Name: {Name}, PushName: {PushName})",
                    nome,
                    contatoWaha.Name,
                    contatoWaha.PushName
                );

                // Buscar foto de perfil
                try
                {
                    var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                    if (!string.IsNullOrWhiteSpace(empresaId))
                    {
                        var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);
                        if (empresa != null && !string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                        {
                            var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                            var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                            if (!string.IsNullOrWhiteSpace(wahaApiUrl) && !string.IsNullOrWhiteSpace(wahaApiKey))
                            {
                                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(95));

                                fotoPerfil = await _whatsAppService.GetProfileImageUrlAsync(
                                    empresa.WahaSessionName,
                                    numeroInfo.NumeroWaha,
                                    wahaApiUrl,
                                    wahaApiKey,
                                    cts.Token
                                );

                                if (!string.IsNullOrWhiteSpace(fotoPerfil))
                                {
                                    _logger.LogInformation(
                                        "✅ Foto de perfil obtida - Número: {Numero}",
                                        numeroInfo.NumeroWaha
                                    );
                                }
                            }
                        }
                    }
                }
                catch (Exception photoEx)
                {
                    _logger.LogWarning(
                        photoEx,
                        "Erro ao buscar foto de perfil - Número: {Numero}. Continuando sem foto.",
                        numeroInfo.NumeroWaha
                    );
                }
            }
            else
            {
                // Fallback: usar dados do webhook
                nome = ExtrairNomeDoContato(webhookEvent);

                _logger.LogInformation(
                    "Usando informações do webhook (WAHA indisponível) - Nome: {Nome}",
                    nome
                );
            }

            var cliente = new Cliente
            {
                Nome = nome,
                Numero = numeroInfo.NumeroFormatado,
                NumeroTelefoneWaha = numeroInfo.NumeroWaha,
                NumeroInterno = numeroInfo.JidCompleto,
                FotoPerfilUrl = fotoPerfil,

                // ✅ NOVO: Informações adicionais do WhatsApp (se disponíveis)
                PushName = contatoWaha?.PushName,
                WhatsAppId = contatoWaha?.Id,
                IsMyContact = contatoWaha?.IsMyContact,
                IsWAContact = contatoWaha?.IsWAContact,
                DtUltimaAtualizacaoWaha = contatoWaha != null ? DateTime.UtcNow : null,

                StatusConversa = StatusConversa.Ativa,
                DtPrimeiroContato = DateTime.UtcNow,
                DtUltimaInteracao = DateTime.UtcNow,
                DtaCadastro = DateTime.UtcNow,
                FlgAtivo = true,
                TotalMensagens = 0,
                Contexto = new ContextoAtual
                {
                    DtUltimaAtualizacao = DateTime.UtcNow,
                    EntidadesExtraidas = new Dictionary<string, string>()
                }
            };

            _logger.LogInformation(
                "Novo cliente criado - Nome: {Nome}, NumeroWaha: {NumeroWaha}, JID: {Jid}",
                cliente.Nome,
                cliente.NumeroTelefoneWaha,
                cliente.NumeroInterno
            );

            return cliente;
        }

        private string ExtrairNomeDoContato(StandardWhatsAppEvent webhookEvent)
        {
            // 1. Tentar obter nome do contato
            if (!string.IsNullOrWhiteSpace(webhookEvent.ContactInfo?.Name))
            {
                return webhookEvent.ContactInfo.Name.Trim();
            }

            // 2. Usar número como nome (último recurso)
            var numeroInfo = TelefoneHelper.ExtrairInformacoes(webhookEvent.ContactInfo?.PhoneNumber ?? string.Empty);
            return numeroInfo.NumeroFormatado;
        }

        #endregion
    }
}
