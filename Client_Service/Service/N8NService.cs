using Client_Repository.Repositorio.Interface;
using Client_Repository.Configuration.Contexto.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;
using Shared.Services.Interface;
using System.Text;
using System.Text.Json;

namespace Client_Service.Service
{
    public class N8NService : IN8NService
    {
        private readonly IConfiguracaoIARepositorio _configuracaoRepositorio;
        private readonly IArquivoRepositorio _arquivoRepositorio;
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IMensagemRepositorio _mensagemRepositorio;
        private readonly IProcessamentoIARepositorio _processamentoIaRepositorio;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<N8NService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IConfiguration _configuration;
        private readonly IContextoMultiTenantService _contextoMultiTenant;
        private readonly Admin_Repository.Repositorio.Interface.IEmpresaRepository _empresaRepositorio;
        private const string N8N_WEBHOOK_URL = "http://65.21.61.206:5678/webhook/bjp2vmr9lpuq3oldet06xosu6msiqwt9z4ayge";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public N8NService(
            IConfiguracaoIARepositorio configuracaoRepositorio,
            IArquivoRepositorio arquivoRepositorio,
            IClienteRepositorio clienteRepositorio,
            IMensagemRepositorio mensagemRepositorio,
            IProcessamentoIARepositorio processamentoIaRepositorio,
            ILogClientService logClientService,
            ILogger<N8NService> logger,
            IHttpClientFactory httpClientFactory,
            IWhatsAppService whatsAppService,
            IConfiguration configuration,
            IContextoMultiTenantService contextoMultiTenant,
            Admin_Repository.Repositorio.Interface.IEmpresaRepository empresaRepositorio)
        {
            _configuracaoRepositorio = configuracaoRepositorio;
            _arquivoRepositorio = arquivoRepositorio;
            _clienteRepositorio = clienteRepositorio;
            _mensagemRepositorio = mensagemRepositorio;
            _processamentoIaRepositorio = processamentoIaRepositorio;
            _logClientService = logClientService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _whatsAppService = whatsAppService;
            _configuration = configuration;
            _contextoMultiTenant = contextoMultiTenant;
            _empresaRepositorio = empresaRepositorio;
        }

        public async Task<PlanoClienteResponse> ObterPlanoClienteAsync(string idCliente)
        {
            try
            {
                _logger.LogInformation("Obtendo plano do cliente: {IdCliente}", idCliente);

                var configuracao = await _configuracaoRepositorio.BuscarPrimeiroPorFiltroAsync(c => c.FlgAtivo == true);

                if (configuracao == null)
                {
                    _logger.LogWarning("Configuração de IA não encontrada para o cliente: {IdCliente}", idCliente);

                    // Retorna plano padrão caso não exista configuração
                    return new PlanoClienteResponse
                    {
                        PlanoDeAudio = "não implementado",
                        PlanoDeDocumentos = "não implementado",
                        PlanoAgendamentoDeAtendimentos = "não implementado"
                    };
                }

                var plano = new PlanoClienteResponse
                {
                    PlanoDeAudio = "não implementado",
                    PlanoDeDocumentos = "não implementado",
                    PlanoAgendamentoDeAtendimentos = "não implementado"
                };

                await _logClientService.RegistrarInfo(
                    "N8NService.ObterPlanoClienteAsync",
                    $"Plano obtido para cliente: {idCliente}"
                );

                return plano;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter plano do cliente: {IdCliente}", idCliente);
                await _logClientService.RegistrarErro(
                    "N8NService.ObterPlanoClienteAsync",
                    ex.Message,
                    idCliente,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<ContextoProjetoResponse> ObterContextoProjetoAsync(string idCliente)
        {
            try
            {
                _logger.LogInformation("Obtendo contexto do projeto para cliente: {IdCliente}", idCliente);

                var configuracao = await _configuracaoRepositorio.BuscarPrimeiroPorFiltroAsync(c => c.FlgAtivo == true);

                if (configuracao == null)
                {
                    _logger.LogWarning("Configuração de IA não encontrada para o cliente: {IdCliente}", idCliente);
                    return new ContextoProjetoResponse
                    {
                        ContextoProjeto = "Nenhuma configuração de contexto disponível."
                    };
                }

                // Montar contexto completo do projeto
                var contextoBuilder = new System.Text.StringBuilder();

                contextoBuilder.AppendLine("=== CONTEXTO DO PROJETO ===");
                contextoBuilder.AppendLine();

                if (!string.IsNullOrEmpty(configuracao.Nome))
                {
                    contextoBuilder.AppendLine("Nome do projeto/produto/serviço:");
                    contextoBuilder.AppendLine(configuracao.Nome);
                    contextoBuilder.AppendLine();
                }


                if (!string.IsNullOrEmpty(configuracao.FuncaoPrincipalDoProduto))
                {
                    contextoBuilder.AppendLine("FUNÇÃO PRINCIPAL DO SISTEMA:");
                    contextoBuilder.AppendLine(configuracao.FuncaoPrincipalDoProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.ModulosFuncionalidadesDoProduto))
                {
                    contextoBuilder.AppendLine("MODULOS FUNCIONALIDADES DO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.ModulosFuncionalidadesDoProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.ProcessoDeUsoProduto))
                {
                    contextoBuilder.AppendLine("PROCESSO DE USO DO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.ProcessoDeUsoProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.PerguntasFrequentesSobreProduto))
                {
                    contextoBuilder.AppendLine("PERGUNTAS FREQUENTES SOBRE O PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.PerguntasFrequentesSobreProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.DoresAtendidasPeloProduto))
                {
                    contextoBuilder.AppendLine("DORES ATENDIDAS PELO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.DoresAtendidasPeloProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.DiferencasVantagensDoProduto))
                {
                    contextoBuilder.AppendLine("DIFERENCAS E VANTAGENS DO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.DiferencasVantagensDoProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.IntegracoesRecursosExtrasProduto))
                {
                    contextoBuilder.AppendLine("INFORMAÇÕES GERAIS:");
                    contextoBuilder.AppendLine(configuracao.IntegracoesRecursosExtrasProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.SuporteEAtendimentoDoProduto))
                {
                    contextoBuilder.AppendLine("SUPORTE E ATENDIMENTO DO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.SuporteEAtendimentoDoProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.IntegracoesRecursosExtrasProduto))
                {
                    contextoBuilder.AppendLine("INTEGRACOES RECUROS E EXTRAS DO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.IntegracoesRecursosExtrasProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.PlanosPrecosCondicoesComerciaisDoProduto))
                {
                    contextoBuilder.AppendLine("PLANOS PRECOES E CONDICIOES COMERCIAIS DO PRODUTO:");
                    contextoBuilder.AppendLine(configuracao.PlanosPrecosCondicoesComerciaisDoProduto);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.CasosDeUsoExemplosPraticosEValoresSistema))
                {
                    contextoBuilder.AppendLine("cASO DE USO E EXEMPLOS PRATICOS. VALORES DO SISTEMA:");
                    contextoBuilder.AppendLine(configuracao.CasosDeUsoExemplosPraticosEValoresSistema);
                    contextoBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(configuracao.InformacoesGerais))
                {
                    contextoBuilder.AppendLine("INFORMACOES GERAIS:");
                    contextoBuilder.AppendLine(configuracao.InformacoesGerais);
                    contextoBuilder.AppendLine();
                }

                var response = new ContextoProjetoResponse
                {
                    ContextoProjeto = contextoBuilder.ToString()
                };

                await _logClientService.RegistrarInfo(
                    "N8NService.ObterContextoProjetoAsync",
                    $"Contexto do projeto obtido para cliente: {idCliente}"
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter contexto do projeto para cliente: {IdCliente}", idCliente);
                await _logClientService.RegistrarErro(
                    "N8NService.ObterContextoProjetoAsync",
                    ex.Message,
                    idCliente,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<DocumentosResponse> ObterDocumentosAsync(string idCliente)
        {
            try
            {
                _logger.LogInformation("Obtendo documentos/arquivos do cliente: {IdCliente}", idCliente);

                // Buscar TODOS os arquivos não excluídos (incluindo áudio, imagem, vídeo, etc)
                var arquivos = await _arquivoRepositorio.BuscarPorFiltroAsync(a =>
                    a.FlgExcluido == false);

                var documentos = arquivos.Select(a => new DocumentoResponse
                {
                    Id = a.Id,
                    Nome = a.NomeOriginal,
                    Tipo = ObterTipoArquivo(a.Tipo, a.MimeType),
                    Url = !string.IsNullOrEmpty(a.UrlWasabi) ? a.UrlWasabi : a.CaminhoCompleto,
                    TamanhoBytes = a.TamanhoBytes,
                    MimeType = a.MimeType,
                    DtUpload = a.DtUpload,
                    FlgProcessado = a.FlgProcessado,
                    ResultadoProcessamento = a.ResultadoProcessamento
                }).ToList();

                var response = new DocumentosResponse
                {
                    Documentos = documentos
                };

                await _logClientService.RegistrarInfo(
                    "N8NService.ObterDocumentosAsync",
                    $"Arquivos obtidos para cliente: {idCliente}, Total: {documentos.Count}"
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter arquivos do cliente: {IdCliente}", idCliente);
                await _logClientService.RegistrarErro(
                    "N8NService.ObterDocumentosAsync",
                    ex.Message,
                    idCliente,
                    ex.StackTrace
                );
                throw;
            }
        }


        /// <summary>
        /// Envia UMA mensagem para o N8N sem esperar resposta (fire and forget)
        /// O N8N processará a mensagem de forma assíncrona e enviará callback quando concluir
        /// </summary>
        public async Task EnviarMensagemParaN8NAsync(N8NEnviarMensagemUnicaRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Enviando mensagem para N8N - Cliente: {IdCliente}, MensagemId: {MensagemId}",
                    request.idCliente,
                    request.mensagem?.IdMensagem
                );

                // Serializar request com camelCase
                var jsonContent = JsonSerializer.Serialize(request, JsonOptions);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Payload N8N: {Payload}", jsonContent);

                // Enviar para N8N
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(1800); // 2 minutos para dar tempo ao N8N processar com IA
                var response = await httpClient.PostAsync(N8N_WEBHOOK_URL, content);

                // Ler e logar resposta
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Resposta N8N - Status: {StatusCode}, Body: {Response}",
                    response.StatusCode,
                    responseContent
                );

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao enviar para N8N - Status: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"N8N retornou status {response.StatusCode}");
                }

                _logger.LogInformation("Mensagem enviada para N8N com sucesso - Cliente: {IdCliente}", request.idCliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem para N8N - Cliente: {IdCliente}", request?.idCliente);
                throw;
            }
        }

        /// <summary>
        /// Processa callback recebido do N8N com a resposta da IA
        /// </summary>
        public async Task ProcessarCallbackRespostaAsync(N8NCallbackRespostaRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Processando callback do N8N - Cliente: {IdCliente}, Empresa: {IdEmpresa}",
                    request.idCliente,
                    request.idEmpresa
                );

                // Validações básicas
                // Nota: IdEmpresa já foi validado/preenchido no Controller através do mapeamento ClienteEmpresaMap
                if (string.IsNullOrEmpty(request.idCliente))
                    throw new ArgumentException("IdCliente é obrigatório");

                if (string.IsNullOrEmpty(request.idEmpresa))
                    throw new ArgumentException("IdEmpresa é obrigatório (deve ter sido preenchido pelo Controller)");

                if (request.output == null)
                    throw new ArgumentException("Output é obrigatório");

                if (request.output.mensagens == null || !request.output.mensagens.Any())
                    throw new ArgumentException("Lista de mensagens é obrigatória");

                // IMPORTANTE: Configurar contexto multi-tenant para endpoints anônimos
                // Buscar um usuário real ativo da empresa
                var usuarioId = await _contextoMultiTenant.BuscarUsuarioAtivoDaEmpresaAsync(request.idEmpresa);

                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    _logger.LogError(
                        "Não foi possível encontrar usuário ativo para a empresa: {EmpresaId}",
                        request.idEmpresa
                    );
                    throw new InvalidOperationException($"Não foi possível encontrar usuário ativo para a empresa {request.idEmpresa}");
                }

                await _contextoMultiTenant.ConfigurarContextoUsuarioAsync(usuarioId, request.idEmpresa);

                _logger.LogDebug(
                    "Contexto multi-tenant configurado - UsuarioId: {UsuarioId}, EmpresaId: {EmpresaId}",
                    usuarioId,
                    request.idEmpresa
                );

                // Verificar se o cliente existe
                var cliente = await _clienteRepositorio.BuscarPorIdAsync(request.idCliente);
                if (cliente == null)
                {
                    _logger.LogWarning("Cliente não encontrado: {IdCliente}", request.idCliente);
                    throw new KeyNotFoundException($"Cliente {request.idCliente} não encontrado");
                }

                // Gerar ID único para o grupo de processamento
                var grupoProcessamentoId = Guid.NewGuid().ToString();

                // 1. Salvar registro de processamento da IA
                var processamento = new ProcessamentoIA
                {
                    ClienteId = request.idCliente,
                    GrupoProcessamentoId = grupoProcessamentoId,
                    MensagensIds = new List<string>(), // Não temos os IDs das mensagens originais no callback
                    QtdMensagensProcessadas = 0,
                    FlgPrimeiraMensagemDoDia = false,
                    PayloadEnviadoJson = string.Empty, // Não temos o payload original
                    RespostaCompletaJson = System.Text.Json.JsonSerializer.Serialize(request.output),
                    QtdMensagensEnviadas = request.output.mensagens?.Count ?? 0,
                    QtdReacoesEnviadas = request.output.reacoes?.Count ?? 0,
                    DocumentosEnviados = request.output.documentosEnviados ?? new List<string>(),
                    TipoEsclarecimento = request.output.tipoEsclarecimento ?? string.Empty,
                    Status = StatusProcessamento.Sucesso,
                    DtProcessamento = DateTime.UtcNow
                };

                await _processamentoIaRepositorio.AdicionarAsync(processamento);

                // 2. Processar e salvar mensagens no banco
                int mensagensEnviadas = 0;
                if (request.output.mensagens != null && request.output.mensagens.Any())
                {
                    foreach (var msgN8N in request.output.mensagens)
                    {
                        var mensagemResposta = new Mensagem
                        {
                            ClienteId = request.idCliente,
                            IdMensagemWhatsApp = Guid.NewGuid().ToString(),
                            TipoMensagem = ConverterTipoMensagem(msgN8N.tipo),
                            Origem = OrigemMensagem.IA,
                            FlgMensagemCliente = false,
                            ConteudoTexto = msgN8N.conteudo,
                            DtRecebido = DateTime.UtcNow,
                            DtProcessamento = DateTime.UtcNow,
                            TimestampWhatsApp = DateTime.UtcNow,
                            StatusEntrega = StatusEntrega.Enviada,
                            FlgEnviadaAoN8N = false,
                            GrupoProcessamentoId = grupoProcessamentoId,
                            Metadados = new Dictionary<string, object>
                            {
                                { "tipo_n8n", msgN8N.tipo ?? "texto" },
                                { "tipo_esclarecimento", request.output.tipoEsclarecimento ?? "N/A" },
                                { "callback_asyncrono", true }
                            }
                        };

                        await _mensagemRepositorio.AdicionarAsync(mensagemResposta);
                        mensagensEnviadas++;
                    }
                }

                // 3. Enviar mensagens para o WhatsApp
                await EnviarMensagensParaWhatsAppAsync(
                    cliente,
                    request.output,
                    grupoProcessamentoId,
                    request.idEmpresa
                );

                // 4. Atualizar cliente com última interação
                cliente.DtUltimaInteracao = DateTime.UtcNow;
                cliente.TotalMensagens += mensagensEnviadas;
                await _clienteRepositorio.EditarAsync(cliente);

                await _logClientService.RegistrarInfo(
                    "N8NService.ProcessarCallbackRespostaAsync",
                    $"Callback do N8N processado com sucesso - Cliente: {request.idCliente}, " +
                    $"Mensagens: {mensagensEnviadas}, Grupo: {grupoProcessamentoId}"
                );

                _logger.LogInformation(
                    "Callback do N8N processado com sucesso - Cliente: {IdCliente}, Mensagens: {Total}",
                    request.idCliente,
                    mensagensEnviadas
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar callback do N8N - Cliente: {IdCliente}",
                    request?.idCliente
                );

                await _logClientService.RegistrarErro(
                    "N8NService.ProcessarCallbackRespostaAsync",
                    ex.Message,
                    request?.idCliente ?? "N/A",
                    ex.StackTrace
                );

                throw;
            }
        }

        /// <summary>
        /// Envia as mensagens recebidas do N8N para o WhatsApp do cliente
        /// </summary>
        private async Task EnviarMensagensParaWhatsAppAsync(Cliente cliente, RespostaIAN8N respostaIA, string grupoProcessamentoId, string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cliente.NumeroInterno))
                {
                    _logger.LogWarning("Cliente {ClienteId} não possui número de WhatsApp configurado", cliente.Id);
                    return;
                }

                // Buscar empresa para obter configurações WAHA específicas
                var empresa = await _empresaRepositorio.BuscarPorIdAsync(empresaId);
                if (empresa == null)
                {
                    _logger.LogError("Empresa {EmpresaId} não encontrada", empresaId);
                    return;
                }

                // Obter wahaSessionName da empresa
                var wahaSession = empresa.WahaSessionName;
                if (string.IsNullOrWhiteSpace(wahaSession))
                {
                    _logger.LogError("Empresa {EmpresaId} não possui WahaSessionName configurado", empresaId);
                    return;
                }

                // Obter outras configurações WAHA do appsettings (que são compartilhadas)
                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    _logger.LogError("Configuração WAHASettings:ApiUrl não encontrada no appsettings");
                    return;
                }

                // Processar e enviar mensagens
                if (respostaIA.mensagens != null && respostaIA.mensagens.Count > 0)
                {
                    foreach (var mensagemN8N in respostaIA.mensagens)
                    {
                        try
                        {
                            EnvioResponse resultadoEnvio = null;

                            // Determinar tipo e enviar mensagem via API WAHA
                            switch (mensagemN8N.tipo?.ToLower())
                            {
                                case "texto":
                                    resultadoEnvio = await _whatsAppService.EnviarTextoAsync(
                                        cliente.NumeroInterno,
                                        mensagemN8N.conteudo ?? string.Empty,
                                        wahaSession,
                                        wahaApiUrl,
                                        wahaApiKey
                                    );
                                    break;

                                case "audio":
                                    if (!string.IsNullOrWhiteSpace(mensagemN8N.conteudo))
                                    {
                                        resultadoEnvio = await _whatsAppService.EnviarMidiaAsync(
                                            cliente.NumeroInterno,
                                            "", // Caption vazio para áudio
                                            mensagemN8N.conteudo, // URL do áudio
                                            wahaSession,
                                            TipoMensagemWhatsApp.Audio,
                                            wahaApiUrl,
                                            wahaApiKey
                                        );
                                    }
                                    break;

                                case "documento":
                                    if (!string.IsNullOrWhiteSpace(mensagemN8N.idDocumento))
                                    {
                                        var documento = await _arquivoRepositorio.BuscarPorIdAsync(mensagemN8N.idDocumento);
                                        if (documento != null && !string.IsNullOrWhiteSpace(documento.UrlWasabi))
                                        {
                                            resultadoEnvio = await _whatsAppService.EnviarMidiaAsync(
                                                cliente.NumeroInterno,
                                                mensagemN8N.conteudo ?? documento.NomeOriginal,
                                                documento.UrlWasabi,
                                                wahaSession,
                                                TipoMensagemWhatsApp.Documento,
                                                wahaApiUrl,
                                                wahaApiKey
                                            );
                                        }
                                    }
                                    break;

                                default:
                                    resultadoEnvio = await _whatsAppService.EnviarTextoAsync(
                                        cliente.NumeroInterno,
                                        mensagemN8N.conteudo ?? string.Empty,
                                        wahaSession,
                                        wahaApiUrl,
                                        wahaApiKey
                                    );
                                    break;
                            }

                            if (resultadoEnvio != null && resultadoEnvio.Sucesso)
                            {
                                _logger.LogInformation(
                                    "Mensagem enviada para WhatsApp - Cliente: {ClienteId}, Tipo: {Tipo}",
                                    cliente.Id,
                                    mensagemN8N.tipo
                                );
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Falha ao enviar mensagem para WhatsApp - Cliente: {ClienteId}, Erro: {Erro}",
                                    cliente.Id,
                                    resultadoEnvio?.Erro ?? "Resposta nula"
                                );
                            }

                            // Delay entre mensagens
                            if (respostaIA.mensagens.Count > 1)
                                await Task.Delay(500);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Erro ao enviar mensagem individual para WhatsApp - Cliente: {ClienteId}",
                                cliente.Id
                            );
                        }
                    }
                }

                // Processar reações se houver
                if (respostaIA.reacoes != null && respostaIA.reacoes.Count > 0)
                {
                    foreach (var reacao in respostaIA.reacoes)
                    {
                        try
                        {
                            var mensagem = await _mensagemRepositorio.BuscarPorIdAsync(reacao.idMensagem);
                            if (mensagem != null)
                            {
                                var resultadoReacao = await _whatsAppService.EnviarReacaoAsync(
                                    mensagem.IdMensagemWhatsApp,
                                    reacao.emoji,
                                    wahaSession,
                                    wahaApiUrl,
                                    wahaApiKey
                                );

                                if (resultadoReacao.Sucesso)
                                {
                                    // Atualizar mensagem com a reação
                                    if (mensagem.Reacoes == null)
                                        mensagem.Reacoes = new List<Reacao>();

                                    mensagem.Reacoes.Add(new Reacao
                                    {
                                        Emoji = reacao.emoji,
                                        DtReacao = DateTime.UtcNow,
                                        FlgEnviada = true,
                                        DtEnvio = DateTime.UtcNow
                                    });

                                    await _mensagemRepositorio.EditarAsync(mensagem);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Erro ao enviar reação para WhatsApp - MensagemId: {MensagemId}",
                                reacao.idMensagem
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao enviar mensagens para WhatsApp - Cliente: {ClienteId}",
                    cliente.Id
                );
                throw;
            }
        }

        /// <summary>
        /// Obtém as mensagens dos últimos 2 dias de um cliente específico
        /// </summary>
        public async Task<List<MensagemN8N>> ObterMensagensRecentesAsync(string idCliente)
        {
            try
            {
                _logger.LogInformation("Obtendo mensagens recentes para cliente: {IdCliente}", idCliente);

                // Calcular data limite (2 dias atrás)
                var dataLimite = DateTime.UtcNow.AddDays(-2);

                // Buscar mensagens do cliente dos últimos 2 dias
                var mensagens = await _mensagemRepositorio.BuscarPorFiltroAsync(m =>
                    m.ClienteId == idCliente &&
                    m.DtRecebido >= dataLimite);

                // Ordenar por data de recebimento
                var mensagensOrdenadas = mensagens
                    .OrderBy(m => m.DtRecebido)
                    .ToList();

                // Converter para formato N8N
                var mensagensN8N = mensagensOrdenadas.Select(m => new MensagemN8N
                {
                    IdMensagem = m.Id,
                    FlgMensagemCliente = m.FlgMensagemCliente,
                    DtRecebido = m.DtRecebido,
                    Mensagem = m.ConteudoTexto ?? string.Empty,
                    TipoMensagem = Shared.Utils.EnumHelper.EnumToString(m.TipoMensagem).ToLower()
                }).ToList();

                _logger.LogInformation(
                    "Mensagens recentes obtidas - Cliente: {IdCliente}, Total: {Total}",
                    idCliente,
                    mensagensN8N.Count
                );

                await _logClientService.RegistrarInfo(
                    "N8NService.ObterMensagensRecentesAsync",
                    $"Mensagens recentes obtidas para cliente: {idCliente}, Total: {mensagensN8N.Count}"
                );

                return mensagensN8N;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter mensagens recentes do cliente: {IdCliente}", idCliente);
                await _logClientService.RegistrarErro(
                    "N8NService.ObterMensagensRecentesAsync",
                    ex.Message,
                    idCliente,
                    ex.StackTrace
                );
                throw;
            }
        }

        #region Métodos Auxiliares

        private TipoMensagem ConverterTipoMensagem(string tipoResposta)
        {
            return tipoResposta?.ToLower() switch
            {
                "audio" => TipoMensagem.Audio,
                "texto" => TipoMensagem.Texto,
                "imagem" => TipoMensagem.Imagem,
                "video" => TipoMensagem.Video,
                "documento" => TipoMensagem.Documento,
                _ => TipoMensagem.Texto
            };
        }

        private string ObterTipoArquivo(TipoArquivo tipoArquivo, string mimeType)
        {
            if (tipoArquivo == TipoArquivo.PDF || mimeType?.Contains("pdf") == true)
                return "PDF";

            if (tipoArquivo == TipoArquivo.Planilha || mimeType?.Contains("excel") == true || mimeType?.Contains("xlsx") == true)
                return "XLSX";

            if (tipoArquivo == TipoArquivo.Audio || mimeType?.Contains("audio") == true)
                return "AUDIO";

            if (tipoArquivo == TipoArquivo.Imagem || mimeType?.Contains("image") == true)
                return "IMAGEM";

            if (tipoArquivo == TipoArquivo.Video || mimeType?.Contains("video") == true)
                return "VIDEO";

            if (tipoArquivo == TipoArquivo.Documento)
            {
                if (mimeType?.Contains("word") == true || mimeType?.Contains("docx") == true)
                    return "DOCX";

                if (mimeType?.Contains("text") == true)
                    return "TXT";

                return "DOCUMENTO";
            }

            return "OUTRO";
        }

        #endregion
    }
}
