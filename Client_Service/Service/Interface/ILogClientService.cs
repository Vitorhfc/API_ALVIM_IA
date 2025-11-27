using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface ILogClientService : IServiceGenerico<LogClient>
    {
        Task LogErroAsync(Exception ex, string metodo, string controller, string? variaveis = null);
        Task LogAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null);
        Task LogInfoAsync(string mensagem, string metodo, string controller);
        Task LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null);
        Task<IEnumerable<LogClient>> BuscarLogsPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<IEnumerable<LogClient>> BuscarLogsPorTipoAsync(string tipo);
        Task<IEnumerable<LogClient>> BuscarLogsPorUsuarioAsync(string usuarioId);

        Task RegistrarWebhook(string idLog, string origem, string evento, string payloadJson);
        Task RegistrarErro(string metodo, string mensagem, string payload, string stackTrace = null);
        Task RegistrarInfo(string origem, string mensagem, string dadosAdicionais = null);
    }
}