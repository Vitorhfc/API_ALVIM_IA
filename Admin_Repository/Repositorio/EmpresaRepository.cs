using Admin_Repository.Configuration;
using Admin_Repository.Repositorio.Interface;
using Admin_Repository.RepositorioGenerico;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio
{
    /// <summary>
    /// Repositório específico para operações com a entidade Empresa
    /// </summary>
    public class EmpresaRepository : RepositorioGenerico<Empresa>, IEmpresaRepository
    {
        public EmpresaRepository(ContextBaseAdmin contextBaseAdmin) 
            : base(contextBaseAdmin, "Empresa")
        {
        }

        #region Métodos Específicos

        /// <summary>
        /// Busca empresa por CNPJ
        /// </summary>
        /// <param name="cnpj">CNPJ da empresa</param>
        /// <returns>Empresa encontrada ou null</returns>
        public async Task<Empresa?> BuscarPorCnpjAsync(string cnpj)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cnpj))
                    throw new ArgumentException("CNPJ é obrigatório", nameof(cnpj));

                return await BuscarPrimeiroPorFiltroAsync(e => e.CNPJ == cnpj);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar empresa por CNPJ '{cnpj}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca empresa por email
        /// </summary>
        /// <param name="email">Email da empresa</param>
        /// <returns>Empresa encontrada ou null</returns>
        public async Task<Empresa?> BuscarPorEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Email é obrigatório", nameof(email));

                return await BuscarPrimeiroPorFiltroAsync(e => e.Email == email);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar empresa por email '{email}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca Numero por email
        /// </summary>
        /// <param name="numeroWhatsApp">Numero da empresa</param>
        /// <returns>Empresa encontrada ou null</returns>
        public async Task<Empresa?> BuscarPorNumeroWhatsAppAsync(string numeroWhatsApp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(numeroWhatsApp))
                throw new ArgumentException("O número esperado é obrigatório", nameof(numeroWhatsApp));

            return await BuscarPrimeiroPorFiltroAsync(e => e.WahaNumeroWhatsApp == numeroWhatsApp);
        }

        /// <summary>
        /// Busca empresa por nome de sessão WAHA
        /// </summary>
        /// <param name="sessionName">Nome da sessão WAHA</param>
        /// <returns>Empresa encontrada ou null</returns>
        public async Task<Empresa?> BuscarPorSessionNameAsync(string sessionName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
                throw new ArgumentException("O nome da sessão é obrigatório", nameof(sessionName));

            return await BuscarPrimeiroPorFiltroAsync(e => e.WahaSessionName == sessionName);
        }

        /// <summary>
        /// Verifica se existe empresa com o CNPJ informado
        /// </summary>
        /// <param name="cnpj">CNPJ a ser verificado</param>
        /// <param name="excluirId">ID a ser excluído da verificação (para edição)</param>
        /// <returns>True se existe, False caso contrário</returns>
        public async Task<bool> ExisteCnpjAsync(string cnpj, string? excluirId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cnpj))
                    return false;

                var empresas = await BuscarPorFiltroAsync(e => e.CNPJ == cnpj && 
                    (excluirId == null || e.Id != excluirId));
                
                return empresas.Any();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao verificar existência de CNPJ '{cnpj}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica se existe empresa com o email informado
        /// </summary>
        /// <param name="email">Email a ser verificado</param>
        /// <param name="excluirId">ID a ser excluído da verificação (para edição)</param>
        /// <returns>True se existe, False caso contrário</returns>
        public async Task<bool> ExisteEmailAsync(string email, string? excluirId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var empresas = await BuscarPorFiltroAsync(e => e.Email == email && 
                    (excluirId == null || e.Id != excluirId));
                
                return empresas.Any();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao verificar existência de email '{email}': {ex.Message}", ex);
            }
        }



        #endregion
    }
}
