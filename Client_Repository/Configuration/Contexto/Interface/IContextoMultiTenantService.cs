using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client_Repository.Configuration.Contexto.Interface
{
    public interface IContextoMultiTenantService
    {
        string? ObterIdUsuarioAtual();
        string? ObterIdEmpresaAtual();
        string? ObterConnectionStringTenantAtual();
        string? ObterNomeBaseDadosTenantAtual();

        void ConfigurarTenant(string usuarioId, string empresaId, string connectionString, string nomeBaseDados);

        Task<IMongoDatabase> ObterBaseDadosEmpresaAsync(string usuarioId, string empresaId);
        Task<IMongoDatabase> ObterBaseDadosAtualAsync();

        bool TemContextoUsuario(string usuarioId, string empresaId);
        void LimparContexto();

        Task ConfigurarContextoUsuarioAsync(string usuarioId, string empresaId);

        /// <summary>
        /// Busca o IdEmpresa associado a um ClienteId
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>ID da empresa ou null se não encontrado</returns>
        Task<string?> BuscarEmpresaPorClienteIdAsync(string clienteId);

        /// <summary>
        /// Busca um usuário ativo de uma empresa para usar em contextos anônimos
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <returns>ID do usuário ou null se não encontrado</returns>
        Task<string?> BuscarUsuarioAtivoDaEmpresaAsync(string empresaId);
    }
}
