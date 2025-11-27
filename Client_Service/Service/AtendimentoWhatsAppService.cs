using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;
using Shared.Classes.ModelView.Client;
using Shared.Services.Interface;

namespace Client_Service.Service
{
    /// <summary>
    /// Implementação do serviço de atendimento via WhatsApp
    /// </summary>
    public class AtendimentoWhatsAppService : IAtendimentoWhatsAppService
    {
        private readonly IClienteRepositorio _clienteRepository;
        private readonly IMensagemRepositorio _mensagemRepository;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IContextoMultiTenantService _contextoMultiTenant;
        private readonly Admin_Repository.Repositorio.Interface.IEmpresaRepository _empresaRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AtendimentoWhatsAppService> _logger;

        public AtendimentoWhatsAppService(
            IClienteRepositorio clienteRepository,
            IMensagemRepositorio mensagemRepository,
            IWhatsAppService whatsAppService,
            IContextoMultiTenantService contextoMultiTenant,
            Admin_Repository.Repositorio.Interface.IEmpresaRepository empresaRepository,
            IConfiguration configuration,
            ILogger<AtendimentoWhatsAppService> logger)
        {
            _clienteRepository = clienteRepository;
            _mensagemRepository = mensagemRepository;
            _whatsAppService = whatsAppService;
            _contextoMultiTenant = contextoMultiTenant;
            _empresaRepository = empresaRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AtendimentoWhatsAppResponse> EnviarMensagemTextoAsync(
            EnviarMensagemTextoRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.ClienteId))
                    return CriarRespostaErro("ClienteId é obrigatório");

                if (string.IsNullOrWhiteSpace(request.Mensagem))
                    return CriarRespostaErro("Mensagem é obrigatória");

                // Buscar cliente
                var cliente = await _clienteRepository.BuscarPorIdAsync(request.ClienteId);
                if (cliente == null)
                    return CriarRespostaErro($"Cliente não encontrado: {request.ClienteId}");

                // Obter configurações WAHA
                var (wahaConfig, erro) = await ObterConfiguracaoWahaAsync();
                if (wahaConfig == null)
                    return CriarRespostaErro(erro ?? "Erro ao obter configuração WAHA");

                _logger.LogInformation(
                    "Enviando mensagem de texto - Cliente: {ClienteId}, Session: {Session}",
                    cliente.Id,
                    wahaConfig.SessionName
                );

                // Enviar mensagem via WAHA
                var envioRequest = new EnviarWhatsAppRequest
                {
                    NumeroDestino = cliente.NumeroTelefoneWaha,
                    Mensagem = request.Mensagem,
                    Session = wahaConfig.SessionName,
                    Tipo = TipoMensagemWhatsApp.Texto,
                    QuotedMessageId = request.QuotedMessageId
                };

                var resultado = await _whatsAppService.EnviarMensagemAsync(
                    envioRequest,
                    wahaConfig.ApiUrl,
                    wahaConfig.ApiKey
                );

                if (!resultado.Sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao enviar mensagem - Cliente: {ClienteId}, Erro: {Erro}",
                        cliente.Id,
                        resultado.Erro
                    );
                    return CriarRespostaErro($"Erro ao enviar mensagem: {resultado.Erro}");
                }

                // Salvar mensagem no banco
                await SalvarMensagemEnviadaAsync(
                    cliente.Id,
                    resultado.IdEnvio,
                    request.Mensagem,
                    TipoMensagem.Texto,
                    null
                );

                _logger.LogInformation(
                    "Mensagem enviada com sucesso - Cliente: {ClienteId}, IdMensagem: {IdMensagem}",
                    cliente.Id,
                    resultado.IdEnvio
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = "Mensagem enviada com sucesso",
                    IdMensagem = resultado.IdEnvio
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem de texto - ClienteId: {ClienteId}", request.ClienteId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<AtendimentoWhatsAppResponse> EnviarMensagemMidiaAsync(
            EnviarMensagemMidiaRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.ClienteId))
                    return CriarRespostaErro("ClienteId é obrigatório");

                if (string.IsNullOrWhiteSpace(request.UrlMidia))
                    return CriarRespostaErro("URL da mídia é obrigatória");

                // Buscar cliente
                var cliente = await _clienteRepository.BuscarPorIdAsync(request.ClienteId);
                if (cliente == null)
                    return CriarRespostaErro($"Cliente não encontrado: {request.ClienteId}");

                // Obter configurações WAHA
                var (wahaConfig, erro) = await ObterConfiguracaoWahaAsync();
                if (wahaConfig == null)
                    return CriarRespostaErro(erro ?? "Erro ao obter configuração WAHA");

                _logger.LogInformation(
                    "Enviando mensagem com mídia - Cliente: {ClienteId}, Tipo: {Tipo}",
                    cliente.Id,
                    request.TipoMidia
                );

                // Converter tipo de mídia
                var tipoMensagem = request.TipoMidia switch
                {
                    TipoMidiaWhatsApp.Imagem => TipoMensagemWhatsApp.Imagem,
                    TipoMidiaWhatsApp.Video => TipoMensagemWhatsApp.Video,
                    TipoMidiaWhatsApp.Audio => TipoMensagemWhatsApp.Audio,
                    TipoMidiaWhatsApp.Documento => TipoMensagemWhatsApp.Documento,
                    _ => TipoMensagemWhatsApp.Documento
                };

                // Enviar mensagem via WAHA
                var resultado = await _whatsAppService.EnviarMidiaAsync(
                    cliente.NumeroTelefoneWaha,
                    request.Caption ?? string.Empty,
                    request.UrlMidia,
                    wahaConfig.SessionName,
                    tipoMensagem,
                    wahaConfig.ApiUrl,
                    wahaConfig.ApiKey
                );

                if (!resultado.Sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao enviar mídia - Cliente: {ClienteId}, Erro: {Erro}",
                        cliente.Id,
                        resultado.Erro
                    );
                    return CriarRespostaErro($"Erro ao enviar mídia: {resultado.Erro}");
                }

                // Salvar mensagem no banco
                var tipoMensagemBanco = request.TipoMidia switch
                {
                    TipoMidiaWhatsApp.Imagem => TipoMensagem.Imagem,
                    TipoMidiaWhatsApp.Video => TipoMensagem.Video,
                    TipoMidiaWhatsApp.Audio => TipoMensagem.Audio,
                    TipoMidiaWhatsApp.Documento => TipoMensagem.Documento,
                    _ => TipoMensagem.Documento
                };

                await SalvarMensagemEnviadaAsync(
                    cliente.Id,
                    resultado.IdEnvio,
                    request.Caption,
                    tipoMensagemBanco,
                    request.UrlMidia
                );

                _logger.LogInformation(
                    "Mídia enviada com sucesso - Cliente: {ClienteId}, IdMensagem: {IdMensagem}",
                    cliente.Id,
                    resultado.IdEnvio
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = "Mídia enviada com sucesso",
                    IdMensagem = resultado.IdEnvio
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mídia - ClienteId: {ClienteId}", request.ClienteId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<AtendimentoWhatsAppResponse> EnviarAudioAsync(
            EnviarAudioRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.ClienteId))
                    return CriarRespostaErro("ClienteId é obrigatório");

                if (string.IsNullOrWhiteSpace(request.UrlAudio))
                    return CriarRespostaErro("URL do áudio é obrigatória");

                // Buscar cliente
                var cliente = await _clienteRepository.BuscarPorIdAsync(request.ClienteId);
                if (cliente == null)
                    return CriarRespostaErro($"Cliente não encontrado: {request.ClienteId}");

                // Obter configurações WAHA
                var (wahaConfig, erro) = await ObterConfiguracaoWahaAsync();
                if (wahaConfig == null)
                    return CriarRespostaErro(erro ?? "Erro ao obter configuração WAHA");

                _logger.LogInformation(
                    "Enviando áudio - Cliente: {ClienteId}",
                    cliente.Id
                );

                // Enviar áudio via WAHA
                var resultado = await _whatsAppService.EnviarAudioAsync(
                    cliente.NumeroTelefoneWaha,
                    request.UrlAudio,
                    wahaConfig.SessionName,
                    wahaConfig.ApiUrl,
                    wahaConfig.ApiKey,
                    request.QuotedMessageId
                );

                if (!resultado.Sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao enviar áudio - Cliente: {ClienteId}, Erro: {Erro}",
                        cliente.Id,
                        resultado.Erro
                    );
                    return CriarRespostaErro($"Erro ao enviar áudio: {resultado.Erro}");
                }

                // Salvar mensagem no banco
                await SalvarMensagemEnviadaAsync(
                    cliente.Id,
                    resultado.IdEnvio,
                    null,
                    TipoMensagem.Audio,
                    request.UrlAudio
                );

                _logger.LogInformation(
                    "Áudio enviado com sucesso - Cliente: {ClienteId}, IdMensagem: {IdMensagem}",
                    cliente.Id,
                    resultado.IdEnvio
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = "Áudio enviado com sucesso",
                    IdMensagem = resultado.IdEnvio
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar áudio - ClienteId: {ClienteId}", request.ClienteId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<AtendimentoWhatsAppResponse> ReagirMensagemAsync(
            ReagirMensagemRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.MensagemId))
                    return CriarRespostaErro("MensagemId é obrigatório");

                if (string.IsNullOrWhiteSpace(request.Emoji))
                    return CriarRespostaErro("Emoji é obrigatório");

                // Buscar mensagem
                var mensagem = await _mensagemRepository.BuscarPorIdMensagemWhatsAppAsync(request.MensagemId);
                if (mensagem == null)
                    return CriarRespostaErro($"Mensagem não encontrada: {request.MensagemId}");

                // Obter configurações WAHA
                var (wahaConfig, erro) = await ObterConfiguracaoWahaAsync();
                if (wahaConfig == null)
                    return CriarRespostaErro(erro ?? "Erro ao obter configuração WAHA");

                _logger.LogInformation(
                    "Reagindo à mensagem - MensagemId: {MensagemId}, Emoji: {Emoji}",
                    request.MensagemId,
                    request.Emoji
                );

                // Enviar reação via WAHA
                var resultado = await _whatsAppService.EnviarReacaoAsync(
                    request.MensagemId,
                    request.Emoji,
                    wahaConfig.SessionName,
                    wahaConfig.ApiUrl,
                    wahaConfig.ApiKey
                );

                if (!resultado.Sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao reagir à mensagem - MensagemId: {MensagemId}, Erro: {Erro}",
                        request.MensagemId,
                        resultado.Erro
                    );
                    return CriarRespostaErro($"Erro ao enviar reação: {resultado.Erro}");
                }

                _logger.LogInformation(
                    "Reação enviada com sucesso - MensagemId: {MensagemId}",
                    request.MensagemId
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = "Reação enviada com sucesso"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reagir à mensagem - MensagemId: {MensagemId}", request.MensagemId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<AtendimentoWhatsAppResponse> RemoverMensagemAsync(
            RemoverMensagemRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.MensagemId))
                    return CriarRespostaErro("MensagemId é obrigatório");

                // Buscar mensagem
                var mensagem = await _mensagemRepository.BuscarPorIdMensagemWhatsAppAsync(request.MensagemId);
                if (mensagem == null)
                    return CriarRespostaErro($"Mensagem não encontrada: {request.MensagemId}");

                // Buscar cliente para obter número
                var cliente = await _clienteRepository.BuscarPorIdAsync(mensagem.ClienteId);
                if (cliente == null)
                    return CriarRespostaErro($"Cliente não encontrado: {mensagem.ClienteId}");

                // Obter configurações WAHA
                var (wahaConfig, erro) = await ObterConfiguracaoWahaAsync();
                if (wahaConfig == null)
                    return CriarRespostaErro(erro ?? "Erro ao obter configuração WAHA");

                _logger.LogInformation(
                    "Removendo mensagem - MensagemId: {MensagemId}",
                    request.MensagemId
                );

                // Remover mensagem via WAHA
                var sucesso = await _whatsAppService.RemoveMessageForAllAsync(
                    wahaConfig.SessionName,
                    cliente.NumeroTelefoneWaha,
                    request.MensagemId,
                    wahaConfig.ApiUrl,
                    wahaConfig.ApiKey,
                    cancellationToken
                );

                if (!sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao remover mensagem - MensagemId: {MensagemId}",
                        request.MensagemId
                    );
                    return CriarRespostaErro("Erro ao remover mensagem");
                }

                // Marcar como deletada no banco
                await _mensagemRepository.MarcarComoDeletadaPorIdWhatsAppAsync(request.MensagemId);

                _logger.LogInformation(
                    "Mensagem removida com sucesso - MensagemId: {MensagemId}",
                    request.MensagemId
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = "Mensagem removida com sucesso"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover mensagem - MensagemId: {MensagemId}", request.MensagemId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<AtendimentoWhatsAppResponse> EditarMensagemAsync(
            EditarMensagemRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.MensagemId))
                    return CriarRespostaErro("MensagemId é obrigatório");

                if (string.IsNullOrWhiteSpace(request.NovoTexto))
                    return CriarRespostaErro("Novo texto é obrigatório");

                // Buscar mensagem
                var mensagem = await _mensagemRepository.BuscarPorIdMensagemWhatsAppAsync(request.MensagemId);
                if (mensagem == null)
                    return CriarRespostaErro($"Mensagem não encontrada: {request.MensagemId}");

                // Buscar cliente para obter número
                var cliente = await _clienteRepository.BuscarPorIdAsync(mensagem.ClienteId);
                if (cliente == null)
                    return CriarRespostaErro($"Cliente não encontrado: {mensagem.ClienteId}");

                // Obter configurações WAHA
                var (wahaConfig, erro) = await ObterConfiguracaoWahaAsync();
                if (wahaConfig == null)
                    return CriarRespostaErro(erro ?? "Erro ao obter configuração WAHA");

                _logger.LogInformation(
                    "Editando mensagem - MensagemId: {MensagemId}",
                    request.MensagemId
                );

                // Editar mensagem via WAHA
                var sucesso = await _whatsAppService.EditMessageAsync(
                    wahaConfig.SessionName,
                    cliente.NumeroTelefoneWaha,
                    request.MensagemId,
                    request.NovoTexto,
                    wahaConfig.ApiUrl,
                    wahaConfig.ApiKey,
                    cancellationToken
                );

                if (!sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao editar mensagem - MensagemId: {MensagemId}",
                        request.MensagemId
                    );
                    return CriarRespostaErro("Erro ao editar mensagem");
                }

                // Atualizar conteúdo no banco
                await _mensagemRepository.AtualizarConteudoPorIdWhatsAppAsync(request.MensagemId, request.NovoTexto);

                _logger.LogInformation(
                    "Mensagem editada com sucesso - MensagemId: {MensagemId}",
                    request.MensagemId
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = "Mensagem editada com sucesso"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar mensagem - MensagemId: {MensagemId}", request.MensagemId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<AtendimentoWhatsAppResponse> AlternarModoRespostaAsync(
            AlternarModoRespostaRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request.ClienteId))
                    return CriarRespostaErro("ClienteId é obrigatório");

                // Buscar cliente
                var cliente = await _clienteRepository.BuscarPorIdAsync(request.ClienteId);
                if (cliente == null)
                    return CriarRespostaErro($"Cliente não encontrado: {request.ClienteId}");

                _logger.LogInformation(
                    "Alternando modo de resposta - Cliente: {ClienteId}, AtendimentoHumano: {AtendimentoHumano}",
                    cliente.Id,
                    request.AtendimentoHumano
                );

                // Atualizar flag
                cliente.FlgRespostaResponsavel = request.AtendimentoHumano;

                if (request.AtendimentoHumano)
                {
                    cliente.DtFlgResponsavelAtiva = DateTime.UtcNow;
                    cliente.StatusConversa = StatusConversa.EmAtendimentoHumano;
                }
                else
                {
                    cliente.DtFlgResponsavelDesativada = DateTime.UtcNow;
                    cliente.StatusConversa = StatusConversa.Ativa;
                }

                await _clienteRepository.AtualizarAsync(cliente);

                var modo = request.AtendimentoHumano ? "atendimento humano" : "resposta automática (IA)";

                _logger.LogInformation(
                    "Modo de resposta alterado - Cliente: {ClienteId}, Modo: {Modo}",
                    cliente.Id,
                    modo
                );

                return new AtendimentoWhatsAppResponse
                {
                    Sucesso = true,
                    Mensagem = $"Modo alterado para {modo}",
                    Dados = new
                    {
                        clienteId = cliente.Id,
                        nomeCliente = cliente.Nome,
                        atendimentoHumano = cliente.FlgRespostaResponsavel,
                        statusConversa = cliente.StatusConversa.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alternar modo de resposta - ClienteId: {ClienteId}", request.ClienteId);
                return CriarRespostaErro($"Erro interno: {ex.Message}");
            }
        }

        public async Task<StatusModoRespostaResponse?> ObterStatusModoRespostaAsync(
            string clienteId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    return null;

                var cliente = await _clienteRepository.BuscarPorIdAsync(clienteId);
                if (cliente == null)
                    return null;

                return new StatusModoRespostaResponse
                {
                    ClienteId = cliente.Id,
                    NomeCliente = cliente.Nome,
                    AtendimentoHumano = cliente.FlgRespostaResponsavel,
                    DataAtivacao = cliente.DtFlgResponsavelAtiva,
                    DataDesativacao = cliente.DtFlgResponsavelDesativada
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status do modo de resposta - ClienteId: {ClienteId}", clienteId);
                return null;
            }
        }

        #region Métodos Auxiliares

        private async Task<(WahaConfig? Config, string? Erro)> ObterConfiguracaoWahaAsync()
        {
            try
            {
                var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                if (string.IsNullOrWhiteSpace(empresaId))
                    return (null, "Empresa não identificada no contexto");

                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);
                if (empresa == null)
                    return (null, $"Empresa não encontrada: {empresaId}");

                if (string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                    return (null, "Sessão WAHA não configurada para esta empresa");

                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                    return (null, "URL da API WAHA não configurada");

                return (new WahaConfig
                {
                    SessionName = empresa.WahaSessionName,
                    ApiUrl = wahaApiUrl,
                    ApiKey = wahaApiKey ?? string.Empty
                }, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter configuração WAHA");
                return (null, $"Erro ao obter configuração: {ex.Message}");
            }
        }

        private async Task SalvarMensagemEnviadaAsync(
            string clienteId,
            string? idMensagemWhatsApp,
            string? conteudoTexto,
            TipoMensagem tipoMensagem,
            string? urlMidia)
        {
            try
            {
                var mensagem = new Mensagem
                {
                    ClienteId = clienteId,
                    IdMensagemWhatsApp = idMensagemWhatsApp,
                    TipoMensagem = tipoMensagem,
                    Origem = OrigemMensagem.Funcionario,
                    FlgMensagemCliente = false,
                    ConteudoTexto = conteudoTexto,
                    DtRecebido = DateTime.UtcNow,
                    TimestampWhatsApp = DateTime.UtcNow,
                    StatusEntrega = StatusEntrega.Enviada,
                    FlgEnviadaAoN8N = false
                };

                if (!string.IsNullOrWhiteSpace(urlMidia))
                {
                    mensagem.Midia = new MidiaInfo
                    {
                        UrlDownload = urlMidia,
                        FlgBaixada = false
                    };
                }

                await _mensagemRepository.AdicionarAsync(mensagem);

                _logger.LogDebug(
                    "Mensagem salva no banco - ClienteId: {ClienteId}, Tipo: {Tipo}",
                    clienteId,
                    tipoMensagem
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Erro ao salvar mensagem enviada no banco - ClienteId: {ClienteId}",
                    clienteId
                );
            }
        }

        private AtendimentoWhatsAppResponse CriarRespostaErro(string mensagemErro)
        {
            return new AtendimentoWhatsAppResponse
            {
                Sucesso = false,
                Mensagem = "Operação falhou",
                Erro = mensagemErro
            };
        }

        #endregion

        private class WahaConfig
        {
            public string SessionName { get; set; } = string.Empty;
            public string ApiUrl { get; set; } = string.Empty;
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}
