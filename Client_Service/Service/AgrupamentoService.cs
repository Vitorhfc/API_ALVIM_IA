using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.Client;
using System.Collections.Concurrent;

namespace Client_Service.Service
{
    /// <summary>
    /// Serviço para agrupamento de mensagens em memória antes de envio ao N8N
    /// Agrupa mensagens do mesmo cliente por um período configurável
    /// Funciona como um sistema de PACOTES - acumula mensagens em uma única lista por cliente
    /// </summary>
    public class AgrupamentoService : IAgrupamentoService
    {
        private readonly IMensagemRepositorio _mensagemRepositorio;
        private readonly IConfiguracaoIARepositorio _configuracaoRepositorio;
        private readonly IProcessamentoIAService _processamentoIaService;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<AgrupamentoService> _logger;
        private readonly int _tempoAgrupamentoPadraoSegundos;

        // Dicionário para armazenar grupos ativos (ClienteId -> Grupo)
        // Um cliente tem apenas UM grupo ativo por vez
        private readonly ConcurrentDictionary<string, GrupoMensagens> _gruposAtivos = new();

        // Dicionário para armazenar tokens de cancelamento (ClienteId -> CancellationTokenSource)
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _timersAgrupamento = new();

        // Lock para garantir thread-safety ao adicionar mensagens
        private readonly object _lockObj = new object();

        public AgrupamentoService(
            IMensagemRepositorio mensagemRepositorio,
            IConfiguracaoIARepositorio configuracaoRepositorio,
            IProcessamentoIAService processamentoIaService,
            ILogClientService logClientService,
            ILogger<AgrupamentoService> logger,
            IConfiguration configuration)
        {
            _mensagemRepositorio = mensagemRepositorio;
            _configuracaoRepositorio = configuracaoRepositorio;
            _processamentoIaService = processamentoIaService;
            _logClientService = logClientService;
            _logger = logger;

            // Tempo padrão de agrupamento (pode ser sobrescrito pela configuração do tenant)
            _tempoAgrupamentoPadraoSegundos = int.Parse(
                configuration["Agrupamento:TempoSegundos"] ?? "10"
            );

            _logger.LogInformation(
                "AgrupamentoService inicializado com tempo de agrupamento: {Tempo} segundos",
                _tempoAgrupamentoPadraoSegundos
            );
        }


        #region Classes Internas

        private class GrupoMensagens
        {
            public string ClienteId { get; set; } = string.Empty;
            public List<string> MensagensIds { get; set; } = new();
            public DateTime DtInicio { get; set; }
            public DateTime DtExpiracao { get; set; }
            public int TempoAgrupamentoSegundos { get; set; }
        }

        #endregion
    }
}
