using Client_Repository.Repositorio;
using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Microsoft.Extensions.Configuration;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;
using Shared.Services.Interface;
using Shared.Utils;


namespace Client_Service.Service
{
    public class ProcessamentoIAService : ServiceGenerico<ProcessamentoIA>, IProcessamentoIAService
    {
        private readonly IProcessamentoIARepositorio _processamentoIaRepositorio;
        private readonly IMensagemRepositorio _mensagemRepositorio;
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IN8NService _n8nService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IConfiguration _configuration;
        private readonly IArquivoRepositorio _arquivoRepositorio;
        private readonly IClienteEmpresaMapService _clienteEmpresaMapService;

        public ProcessamentoIAService(
            IProcessamentoIARepositorio repositorio,
            IMensagemRepositorio mensagemRepositorio,
            IClienteRepositorio clienteRepositorio,
            IN8NService n8nService,
            IWhatsAppService whatsAppService,
            IConfiguration configuration,
            IArquivoRepositorio arquivoRepositorio,
            IClienteEmpresaMapService clienteEmpresaMapService)
            : base(repositorio)
        {
            _processamentoIaRepositorio = repositorio;
            _mensagemRepositorio = mensagemRepositorio;
            _clienteRepositorio = clienteRepositorio;
            _n8nService = n8nService;
            _whatsAppService = whatsAppService;
            _configuration = configuration;
            _arquivoRepositorio = arquivoRepositorio;
            _clienteEmpresaMapService = clienteEmpresaMapService;
        }

        // ✅ CORRIGIDO: Agora busca por lista de IDs de mensagens
        public async Task<IEnumerable<ProcessamentoIA>> BuscarPorMensagemAsync(string mensagemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mensagemId))
                    throw new ArgumentException("ID da mensagem não pode ser vazio", nameof(mensagemId));

                // ✅ MensagemId → MensagensIds (lista)
                return await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.MensagensIds != null && p.MensagensIds.Contains(mensagemId));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos por mensagem: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Agora busca por ClienteId (mais útil na v2.0)
        public async Task<IEnumerable<ProcessamentoIA>> BuscarPorClienteAsync(string clienteId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));

                return await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.ClienteId == clienteId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos por cliente: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Busca por GrupoProcessamentoId
        public async Task<ProcessamentoIA?> BuscarPorGrupoAsync(string grupoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(grupoId))
                    throw new ArgumentException("ID do grupo não pode ser vazio", nameof(grupoId));

                var processamentos = await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.GrupoProcessamentoId == grupoId);

                return processamentos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamento por grupo: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Uso correto do enum StatusProcessamento
        public async Task<IEnumerable<ProcessamentoIA>> BuscarPorStatusAsync(StatusProcessamento status)
        {
            try
            {
                return await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.Status == status);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos por status: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Sobrecarga para aceitar string e converter
        public async Task<IEnumerable<ProcessamentoIA>> BuscarPorStatusAsync(string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    throw new ArgumentException("Status não pode ser vazio", nameof(status));

                var statusEnum = EnumHelper.StringToEnum<StatusProcessamento>(status);
                return await BuscarPorStatusAsync(statusEnum);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos por status: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Busca último processamento por ClienteId
        public async Task<ProcessamentoIA?> BuscarUltimoProcessamentoAsync(string clienteId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));

                var processamentos = await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.ClienteId == clienteId);

                // ✅ DataCriacao → DtProcessamento
                return processamentos
                    .OrderByDescending(p => p.DtProcessamento)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar último processamento: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Uso correto do enum
        public async Task<IEnumerable<ProcessamentoIA>> BuscarProcessamentosFalhadosAsync()
        {
            try
            {
                return await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.Status == StatusProcessamento.Erro ||
                    p.Status == StatusProcessamento.Timeout);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos falhados: {ex.Message}", ex);
            }
        }

        // ✅ NOVO: Buscar processamentos bem-sucedidos
        public async Task<IEnumerable<ProcessamentoIA>> BuscarProcessamentosSucessoAsync(string clienteId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));

                return await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.ClienteId == clienteId &&
                    p.Status == StatusProcessamento.Sucesso);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos bem-sucedidos: {ex.Message}", ex);
            }
        }

        // ✅ NOVO: Buscar por período
        public async Task<IEnumerable<ProcessamentoIA>> BuscarPorPeriodoAsync(
            string clienteId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));

                return await _processamentoIaRepositorio.BuscarPorFiltroAsync(p =>
                    p.ClienteId == clienteId &&
                    p.DtProcessamento >= dataInicio &&
                    p.DtProcessamento <= dataFim);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar processamentos por período: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Envia a resposta da IA para o cliente via WhatsApp
        /// Retorna (qtdMensagensEnviadas, qtdReacoesEnviadas)
        /// </summary>
        private async Task<(int, int)> EnviarRespostaParaClienteAsync(string clienteId, RespostaIAN8N respostaIA, string grupoId)
        {
            int qtdMensagensEnviadas = 0;
            int qtdReacoesEnviadas = 0;

            try
            {
                // 1. Buscar dados do cliente
                var cliente = await _clienteRepositorio.BuscarPorIdAsync(clienteId);
                if (cliente == null)
                    throw new InvalidOperationException($"Cliente {clienteId} não encontrado");

                if (string.IsNullOrWhiteSpace(cliente.NumeroInterno))
                    throw new InvalidOperationException($"Cliente {clienteId} não possui número de WhatsApp configurado");

                // 2. Obter configurações WAHA do appsettings
                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];
                var wahaSession = _configuration["WAHASettings:SessionName"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                    throw new InvalidOperationException("Configuração WAHA não encontrada no appsettings");

                if (string.IsNullOrWhiteSpace(wahaSession))
                    throw new InvalidOperationException("Session WAHA não configurada no appsettings");

                // 3. Processar e enviar múltiplas mensagens
                if (respostaIA.mensagens != null && respostaIA.mensagens.Any())
                {
                    int ordem = 1;
                    foreach (var mensagemN8N in respostaIA.mensagens)
                    {
                        try
                        {
                            EnvioResponse resultadoEnvio = null;
                            TipoMensagem tipoMensagem = TipoMensagem.Texto;

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
                                    tipoMensagem = TipoMensagem.Texto;
                                    break;

                                case "audio":
                                    // Enviar áudio via WAHA (URL do áudio deve vir em mensagemN8N.conteudo)
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
                                        tipoMensagem = TipoMensagem.Audio;
                                    }
                                    else
                                    {
                                        // Se não tem URL de áudio, pula
                                        ordem++;
                                        continue;
                                    }
                                    break;

                                case "documento":
                                    // Buscar documento por ID e enviar
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
                                            tipoMensagem = TipoMensagem.Documento;
                                        }
                                        else
                                        {
                                            // Documento não encontrado ou sem URL
                                            ordem++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // ID do documento não fornecido
                                        ordem++;
                                        continue;
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
                                    tipoMensagem = TipoMensagem.Texto;
                                    break;
                            }

                            // Salvar mensagem no banco se enviada com sucesso
                            if (resultadoEnvio != null && resultadoEnvio.Sucesso)
                            {
                                var mensagemResposta = new Mensagem
                                {
                                    ClienteId = clienteId,
                                    IdMensagemWhatsApp = resultadoEnvio.IdEnvio ?? Guid.NewGuid().ToString(),
                                    TipoMensagem = tipoMensagem,
                                    Origem = OrigemMensagem.IA,
                                    FlgMensagemCliente = false,
                                    ConteudoTexto = mensagemN8N.conteudo,
                                    DtRecebido = DateTime.UtcNow,
                                    DtProcessamento = DateTime.UtcNow,
                                    TimestampWhatsApp = DateTime.UtcNow,
                                    StatusEntrega = StatusEntrega.Enviada,
                                    FlgEnviadaAoN8N = false,
                                    GrupoProcessamentoId = grupoId,
                                    Metadados = new Dictionary<string, object>
                                    {
                                        { "ordem", ordem },
                                        { "tipo_n8n", mensagemN8N.tipo ?? "texto" },
                                        { "tipo_esclarecimento", respostaIA.tipoEsclarecimento ?? "N/A" }
                                    }
                                };

                                await _mensagemRepositorio.AdicionarAsync(mensagemResposta);
                                qtdMensagensEnviadas++;
                            }

                            // Pequeno delay entre mensagens para não sobrecarregar
                            if (respostaIA.mensagens.Count > 1)
                                await Task.Delay(500);

                            ordem++;
                        }
                        catch (Exception ex)
                        {
                            // Log erro mas continua com as próximas mensagens
                            throw new ApplicationException($"Erro ao enviar mensagem {ordem}: {ex.Message}", ex);
                        }
                    }
                }

                // 4. Processar e enviar reações
                if (respostaIA.reacoes != null && respostaIA.reacoes.Any())
                {
                    foreach (var reacao in respostaIA.reacoes)
                    {
                        try
                        {
                            // Buscar mensagem para obter o IdMensagemWhatsApp
                            var mensagem = await _mensagemRepositorio.BuscarPorIdAsync(reacao.idMensagem);
                            if (mensagem == null)
                            {
                                // Log: Mensagem não encontrada para reação
                                continue;
                            }

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
                                qtdReacoesEnviadas++;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log erro mas continua com as próximas reações
                            throw new ApplicationException($"Erro ao enviar reação para mensagem {reacao.idMensagem}: {ex.Message}", ex);
                        }
                    }
                }

                // 5. TODO: Processar e enviar documentos
                if (respostaIA.documentosEnviados != null && respostaIA.documentosEnviados.Any())
                {
                    // TODO: Implementar envio de documentos
                    // 1. Buscar documento no banco pelo ID
                    // 2. Obter URL do documento (local ou cloud)
                    // 3. Enviar usando WhatsAppService.EnviarMidiaAsync com TipoMensagemWhatsApp.Documento
                    // 4. Salvar mensagem no banco
                }

                // 6. Atualizar última interação do cliente
                cliente.DtUltimaInteracao = DateTime.UtcNow;
                cliente.TotalMensagens += qtdMensagensEnviadas;
                await _clienteRepositorio.EditarAsync(cliente);

                return (qtdMensagensEnviadas, qtdReacoesEnviadas);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao enviar resposta para o cliente: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processa uma mensagem individual pelo ID, determinando automaticamente o cliente
        /// </summary>
        /// <param name="mensagemId">ID da mensagem a processar</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        public async Task ProcessarMensagemAsync(string mensagemId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mensagemId))
                    throw new ArgumentException("ID da mensagem não pode ser vazio", nameof(mensagemId));

                // Buscar a mensagem para obter o clienteId
                var mensagem = await _mensagemRepositorio.BuscarPorIdAsync(mensagemId);
                if (mensagem == null)
                    throw new InvalidOperationException($"Mensagem {mensagemId} não encontrada");

                // Chamar o método existente ProcessarMensagemIndividualAsync
                await ProcessarMensagemIndividualAsync(mensagem.ClienteId, mensagemId, carregarHistorico: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao processar mensagem - MensagemId: {mensagemId}, Erro: {ex.Message}");
                throw new ApplicationException($"Erro ao processar mensagem: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// [NOVO FLUXO ASSÍNCRONO] Processa UMA mensagem individual enviando diretamente para o N8N
        /// Não aguarda resposta - o N8N fará o agrupamento e enviará callback depois
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="mensagemId">ID da mensagem a processar</param>
        /// <param name="carregarHistorico">Se true, o N8N carregará histórico de mensagens. Se false, processa apenas esta mensagem</param>
        public async Task ProcessarMensagemIndividualAsync(string clienteId, string mensagemId, bool carregarHistorico = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));

                if (string.IsNullOrWhiteSpace(mensagemId))
                    throw new ArgumentException("ID da mensagem não pode ser vazio", nameof(mensagemId));

                // 1. Buscar mensagem do banco
                var mensagem = await _mensagemRepositorio.BuscarPorIdAsync(mensagemId);
                if (mensagem == null)
                    throw new InvalidOperationException($"Mensagem {mensagemId} não encontrada");

                // 2. Buscar cliente para validar
                var cliente = await _clienteRepositorio.BuscarPorIdAsync(clienteId);
                if (cliente == null)
                    throw new InvalidOperationException($"Cliente {clienteId} não encontrado");

                // 2.1. Buscar empresa do cliente
                var empresaId = await _clienteEmpresaMapService.ObterEmpresaIdPorClienteAsync(clienteId);
                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    throw new InvalidOperationException($"Empresa não encontrada para o cliente {clienteId}. Verifique o mapeamento ClienteEmpresaMap.");
                }

                // 3. Verificar última mensagem do cliente para determinar se deve carregar histórico
                bool deveCarregarHistorico = carregarHistorico;

                if (!carregarHistorico) // Só verifica se não foi explicitamente solicitado carregar histórico
                {
                    // Buscar última mensagem do cliente (antes desta)
                    var ultimasMensagens = await _mensagemRepositorio.BuscarPorFiltroAsync(m =>
                        m.ClienteId == clienteId &&
                        m.FlgMensagemCliente == true &&
                        m.Id != mensagemId // Excluir a mensagem atual
                    );

                    var ultimaMensagem = ultimasMensagens
                        .OrderByDescending(m => m.DtRecebido)
                        .FirstOrDefault();

                    if (ultimaMensagem != null)
                    {
                        var horasDesdeUltimaMensagem = (DateTime.UtcNow - ultimaMensagem.DtRecebido).TotalHours;

                        // Se passou mais de 12 horas desde a última mensagem, carregar histórico
                        if (horasDesdeUltimaMensagem > 12)
                        {
                            deveCarregarHistorico = true;
                            Console.WriteLine(
                                $"⏰ Última mensagem do cliente foi há {horasDesdeUltimaMensagem:F1} horas. Carregando histórico."
                            );
                        }
                    }
                    else
                    {
                        // Se não há mensagem anterior, é a primeira mensagem - carregar histórico
                        deveCarregarHistorico = true;
                        Console.WriteLine("📝 Primeira mensagem do cliente. Carregando histórico.");
                    }
                }

                // 4. Montar request para o N8N (mensagem única)
                var request = new N8NEnviarMensagemUnicaRequest
                {
                    idEmpresa = empresaId,
                    idCliente = clienteId,
                    mensagem = new MensagemN8N
                    {
                        IdMensagem = mensagem.Id,
                        FlgMensagemCliente = mensagem.FlgMensagemCliente,
                        DtRecebido = mensagem.DtRecebido,
                        Mensagem = mensagem.ConteudoTexto ?? string.Empty,
                        TipoMensagem = EnumHelper.EnumToString(mensagem.TipoMensagem).ToLower()
                    },
                    flgCarregarMensagems = deveCarregarHistorico
                };

                // 5. Enviar para o N8N (fire and forget - não aguarda resposta)
                await _n8nService.EnviarMensagemParaN8NAsync(request);

                // 6. Marcar mensagem como enviada ao N8N
                mensagem.FlgEnviadaAoN8N = true;
                mensagem.DtProcessamento = DateTime.UtcNow;
                await _mensagemRepositorio.EditarAsync(mensagem);

                // Log de sucesso
                Console.WriteLine(
                    $"✅ Mensagem enviada para N8N (assíncrono) - Empresa: {empresaId}, Cliente: {clienteId}, MensagemId: {mensagemId}, CarregarHistorico: {deveCarregarHistorico}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"❌ Erro ao processar mensagem individual - Cliente: {clienteId}, MensagemId: {mensagemId}, Erro: {ex.Message}"
                );
                throw new ApplicationException($"Erro ao processar mensagem individual: {ex.Message}", ex);
            }
        }

        // ✅ CORRIGIDO: Validação atualizada para v2.0
        protected override async Task ValidarEntidade(ProcessamentoIA entidade)
        {
            var erros = new List<string>();

            // ✅ ClienteId é obrigatório
            if (string.IsNullOrWhiteSpace(entidade.ClienteId))
                erros.Add("Cliente é obrigatório");

            // ✅ GrupoProcessamentoId é obrigatório
            if (string.IsNullOrWhiteSpace(entidade.GrupoProcessamentoId))
                erros.Add("Grupo de processamento é obrigatório");

            // ✅ MensagensIds deve ter pelo menos uma mensagem
            if (entidade.MensagensIds == null || !entidade.MensagensIds.Any())
                erros.Add("Pelo menos uma mensagem é obrigatória");

            // ✅ Status é obrigatório e deve ser válido
            if (!Enum.IsDefined(typeof(StatusProcessamento), entidade.Status))
                erros.Add("Status inválido");

            // ✅ Validações opcionais mas recomendadas
            if (entidade.Status == StatusProcessamento.Sucesso)
            {
                if (string.IsNullOrWhiteSpace(entidade.RespostaCompletaJson))
                    erros.Add("Resposta é obrigatória para processamentos bem-sucedidos");
            }

            if (entidade.Status == StatusProcessamento.Erro)
            {
                if (string.IsNullOrWhiteSpace(entidade.ErroDescricao))
                    erros.Add("Descrição do erro é obrigatória para processamentos com falha");
            }

            if (erros.Any())
                throw new ArgumentException($"Validação falhou: {string.Join(", ", erros)}");

            await Task.CompletedTask;
        }

    }
}