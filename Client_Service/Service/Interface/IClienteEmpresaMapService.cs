using System.Threading.Tasks;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço para gerenciar o mapeamento entre Cliente e Empresa no banco Admin
    /// Permite buscar a empresa de um cliente sem precisar acessar o banco tenant
    /// </summary>
    public interface IClienteEmpresaMapService
    {
        /// <summary>
        /// Sincroniza o mapeamento ClienteEmpresaMap no banco Admin após criar ou atualizar um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="numeroWhatsApp">Número do WhatsApp do cliente (opcional)</param>
        /// <param name="nomeCliente">Nome do cliente (opcional)</param>
        Task SincronizarMapeamentoAsync(string clienteId, string empresaId, string? numeroWhatsApp = null, string? nomeCliente = null);

        /// <summary>
        /// Remove o mapeamento de um cliente (quando cliente for excluído)
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        Task RemoverMapeamentoAsync(string clienteId);

        /// <summary>
        /// Obtém o ID da empresa a partir do ID do cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>ID da empresa ou null se não encontrado</returns>
        Task<string?> ObterEmpresaIdPorClienteAsync(string clienteId);
    }
}
