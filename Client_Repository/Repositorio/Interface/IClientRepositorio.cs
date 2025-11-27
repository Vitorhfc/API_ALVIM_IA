using Client_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.Client;
using System;
using System.Threading.Tasks;

namespace Client_Repository.Repositorio.Interface
{
    /// <summary>
    /// Interface do repositório de Cliente - Métodos Adicionais
    /// </summary>
    public interface IClienteRepositorio : IRepositorioGenerico<Cliente>
    {
        /// <summary>
        /// Busca cliente por número de telefone (limpo, sem formatação)
        /// </summary>
        Task<Cliente?> BuscarPorNumeroAsync(string numero);

        /// <summary>
        /// Busca cliente por número interno do WhatsApp (JID)
        /// Exemplo: 5541999887766@c.us
        /// </summary>
        Task<Cliente?> BuscarPorNumeroInternoAsync(string numeroInterno);

        /// <summary>
        /// Busca cliente por número WAHA (sem formatação, apenas dígitos)
        /// Exemplo: 5541999887766
        /// </summary>
        Task<Cliente?> BuscarPorNumeroWahaAsync(string numeroTelefoneWaha);

        /// <summary>
        /// Atualiza a data da última interação do cliente
        /// </summary>
        Task AtualizarUltimaInteracaoAsync(string clienteId, DateTime dataInteracao);

        /// <summary>
        /// Atualiza o contexto atual do cliente
        /// </summary>
        Task AtualizarContextoAsync(string clienteId, ContextoAtual contexto);

        /// <summary>
        /// Atualiza o status da conversa do cliente
        /// </summary>
        Task AtualizarStatusConversaAsync(string clienteId, StatusConversa status);

        /// <summary>
        /// Insere um novo cliente
        /// </summary>
        Task InserirAsync(Cliente cliente);

        /// <summary>
        /// Atualiza um cliente existente
        /// </summary>
        Task AtualizarAsync(Cliente cliente);

        /// <summary>
        /// Busca ou cria cliente de forma atômica (upsert)
        /// Evita duplicação de clientes em condições de corrida
        /// </summary>
        Task<Cliente> BuscarOuCriarClienteAtomicoAsync(Cliente novoCliente);

        /// <summary>
        /// Cria índice único para NumeroTelefoneWaha
        /// Deve ser executado na inicialização da aplicação
        /// </summary>
        Task CriarIndiceUnicoNumeroWahaAsync();
    }
}