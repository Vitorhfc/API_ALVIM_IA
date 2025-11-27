using Client_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio.Interface
{
    public interface IArquivoRepositorio : IRepositorioGenerico<Arquivo>
    {
        /// <summary>
        /// Busca arquivo por hash (detectar duplicatas)
        /// </summary>
        Task<Arquivo?> BuscarPorHashAsync(string hash);

        /// <summary>
        /// Busca arquivos expirados para limpeza
        /// </summary>
        Task<IEnumerable<Arquivo>> BuscarExpiradosAsync();

        /// <summary>
        /// Marca arquivo como processado
        /// </summary>
        Task MarcarComoProcessadoAsync(string arquivoId, string resultado);

        /// <summary>
        /// Busca arquivos por cliente
        /// </summary>
        Task<IEnumerable<Arquivo>> BuscarPorClienteAsync(string clienteId);

        /// <summary>
        /// Busca arquivo por mensagem
        /// </summary>
        Task<Arquivo?> BuscarPorMensagemAsync(string mensagemId);
    }
}