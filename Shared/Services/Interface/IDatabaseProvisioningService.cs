namespace Shared.Services.Interface
{
    /// <summary>
    /// Serviço responsável pelo provisionamento automático de banco de dados para empresas
    /// </summary>
    public interface IDatabaseProvisioningService
    {
        /// <summary>
        /// Provisiona banco de dados completo para uma empresa
        /// - Gera connection string
        /// - Cria banco de dados
        /// - Cria coleção de log inicial
        /// - Atualiza empresa com os dados
        /// </summary>
        Task<DatabaseProvisioningResult> ProvisionarBancoDadosEmpresaAsync(string empresaId, string empresaNome);

        /// <summary>
        /// Gera uma connection string MongoDB para o cliente
        /// </summary>
        string GerarConnectionString(string nomeBaseDados);

        /// <summary>
        /// Gera um nome único para o banco de dados baseado na empresa
        /// </summary>
        string GerarNomeBaseDados(string empresaId, string empresaNome);

        /// <summary>
        /// Cria o banco de dados e a coleção de log inicial
        /// </summary>
        Task<bool> CriarBancoDadosComLogAsync(string connectionString, string nomeBaseDados, string empresaId);

        /// <summary>
        /// Testa se a connection string é válida
        /// </summary>
        Task<bool> TestarConnectionStringAsync(string connectionString);
    }

    /// <summary>
    /// Resultado do provisionamento do banco de dados
    /// </summary>
    public class DatabaseProvisioningResult
    {
        public bool Sucesso { get; set; }
        public string? ConnectionString { get; set; }
        public string? NomeBaseDados { get; set; }
        public string? Mensagem { get; set; }
        public string? Erro { get; set; }
        public DateTime DataProvisionamento { get; set; }
    }
}
