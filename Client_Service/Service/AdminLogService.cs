using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;

namespace Client_Service.Service
{
    /// <summary>
    /// Serviço para registrar logs diretamente no ambiente Admin (sem passar pelo contexto multi-tenant)
    /// </summary>
    public class AdminLogService : IAdminLogService
    {
        private readonly IMongoCollection<LogWaha> _logWahaCollection;
        private readonly ILogger<AdminLogService> _logger;

        public AdminLogService(
            IOptions<MongoDBSettings> mongoSettings,
            ILogger<AdminLogService> logger)
        {
            _logger = logger;

            try
            {
                var settings = mongoSettings.Value ?? throw new ArgumentNullException(nameof(mongoSettings));

                if (string.IsNullOrEmpty(settings.ConnectionString))
                    throw new InvalidOperationException("ConnectionString não configurada");

                if (string.IsNullOrEmpty(settings.DatabaseNameAdmin))
                    throw new InvalidOperationException("DatabaseNameAdmin não configurado");

                // Conecta DIRETAMENTE ao banco Admin (ignora multi-tenant)
                var mongoClient = new MongoClient(settings.ConnectionString);
                var database = mongoClient.GetDatabase(settings.DatabaseNameAdmin);
                _logWahaCollection = database.GetCollection<LogWaha>("LogWaha");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar AdminLogService");
                throw;
            }
        }

        /// <summary>
        /// Registra um log de webhook WAHA no ambiente Admin
        /// </summary>
        public async Task RegistrarWebhookWahaAsync(
            string idLog,
            string? empresaId,
            string tipoEvento,
            string acao,
            string payload,
            string? payloadLimpo = null,
            bool sucesso = true,
            string? mensagemErro = null,
            string? stackTrace = null,
            HttpContext? httpContext = null)
        {
            try
            {
                var log = new LogWaha
                {
                    IdLog = idLog,
                    EmpresaId = empresaId,
                    TipoEvento = tipoEvento,
                    Origem = "WAHA",
                    Acao = acao,
                    Payload = payload,
                    PayloadLimpo = payloadLimpo,
                    Sucesso = sucesso,
                    MensagemErro = mensagemErro,
                    StackTrace = stackTrace,
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    DtaCadastro = DateTime.UtcNow,
                    FlgAtivo = true
                };

                // Tenta extrair informações adicionais do payload
                try
                {
                    var payloadObj = JsonConvert.DeserializeObject<dynamic>(payload);
                    if (payloadObj != null)
                    {
                        log.SessionId = payloadObj.session?.ToString();
                        log.MessageId = payloadObj.payload?.id?.ToString();
                        log.TelefoneOrigem = payloadObj.payload?.from?.ToString();
                        log.TelefoneDestino = payloadObj.payload?.to?.ToString();
                    }
                }
                catch
                {
                    // Se falhar ao extrair, continua sem essas informações
                }

                // Insere diretamente na collection do MongoDB
                await _logWahaCollection.InsertOneAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log WAHA no Admin. IdLog: {IdLog}, TipoEvento: {TipoEvento}",
                    idLog, tipoEvento);
                // Não propaga exceção para não quebrar o fluxo principal
            }
        }

        /// <summary>
        /// Registra um log genérico no ambiente Admin
        /// </summary>
        public async Task RegistrarLogGenericoAsync(
            string origem,
            string acao,
            string descricao,
            bool sucesso = true,
            string? empresaId = null,
            Dictionary<string, string>? metadados = null)
        {
            try
            {
                var log = new LogWaha
                {
                    IdLog = Guid.NewGuid().ToString(),
                    EmpresaId = empresaId,
                    TipoEvento = "GENERICO",
                    Origem = origem,
                    Acao = acao,
                    Payload = descricao,
                    Sucesso = sucesso,
                    Metadados = metadados,
                    DtaCadastro = DateTime.UtcNow,
                    FlgAtivo = true
                };

                // Insere diretamente na collection do MongoDB
                await _logWahaCollection.InsertOneAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log genérico no Admin. Origem: {Origem}, Acao: {Acao}",
                    origem, acao);
                // Não propaga exceção para não quebrar o fluxo principal
            }
        }
    }
}
