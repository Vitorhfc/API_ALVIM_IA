using Admin_Repository.Repositorio.Interface;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Services.Interface;
using Shared.Utils.Criptografia;

namespace Client_Service.Service
{
    /// <summary>
    /// Resolve e conecta ao tenant correto baseado no nome da session WAHA ou número do WhatsApp
    /// </summary>
    public class TenantResolverService : ITenantResolverService
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ILogger<TenantResolverService> _logger;
        private readonly IMongoClientFactory _mongoClientFactory;
        private readonly IDatabaseProvisioningService _databaseProvisioningService;

        // Cache do tenant atual (por requisição)
        private string? _tenantAtualId;
        private string? _connectionStringAtual;
        private IMongoDatabase? _databaseAtual;

        public TenantResolverService(
            IEmpresaRepository empresaRepository,
            IMongoClientFactory mongoClientFactory,
            IDatabaseProvisioningService databaseProvisioningService,
            ILogger<TenantResolverService> logger)
        {
            _empresaRepository = empresaRepository;
            _mongoClientFactory = mongoClientFactory;
            _databaseProvisioningService = databaseProvisioningService;
            _logger = logger;
        }

        public async Task<bool> ResolverPorSessionAsync(
            string sessionName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Resolvendo tenant para session: {Session}",
                    sessionName
                );

                // 1. Buscar empresa no banco ADM pelo nome da session WAHA
                var empresa = await _empresaRepository
                    .BuscarPrimeiroPorFiltroAsync(e => e.WahaSessionName == sessionName);

                if (empresa == null)
                {
                    _logger.LogWarning(
                        "Empresa não encontrada para session: {Session}",
                        sessionName
                    );
                    return false;
                }

                // 2. Verificar se empresa está ativa
                if (!empresa.FlgAtivo)
                {
                    _logger.LogWarning(
                        "Empresa inativa - Id: {EmpresaId}, Session: {Session}",
                        empresa.Id,
                        sessionName
                    );
                    return false;
                }

                // 3. Verificar se está suspensa
                if (empresa.FlgSuspensa)
                {
                    _logger.LogWarning(
                        "Empresa suspensa - Id: {EmpresaId}, Session: {Session}",
                        empresa.Id,
                        sessionName
                    );
                    return false;
                }

                // 4. Verificar se WAHA está ativo
                if (!empresa.FlgWahaAtivo)
                {
                    _logger.LogWarning(
                        "WAHA inativo para empresa - Id: {EmpresaId}",
                        empresa.Id
                    );
                    return false;
                }

                // 5. ✅ PROVISIONAMENTO AUTOMÁTICO se não tem banco configurado
                if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    _logger.LogWarning(
                        "Empresa {EmpresaId} sem banco configurado. Provisionando automaticamente...",
                        empresa.Id);

                    var resultadoProvisionamento = await _databaseProvisioningService
                        .ProvisionarBancoDadosEmpresaAsync(
                            empresa.Id,
                            empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                    if (!resultadoProvisionamento.Sucesso)
                    {
                        _logger.LogError(
                            "Falha ao provisionar banco para empresa {EmpresaId}: {Erro}",
                            empresa.Id,
                            resultadoProvisionamento.Erro);
                        return false;
                    }

                    _logger.LogInformation(
                        "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                        empresa.Id,
                        resultadoProvisionamento.NomeBaseDados);

                    // Buscar empresa atualizada com connection string criptografada
                    empresa = await _empresaRepository.BuscarPorIdAsync(empresa.Id);
                    if (empresa == null)
                    {
                        _logger.LogError("Erro ao buscar empresa após provisionamento");
                        return false;
                    }
                }

                // 6. Descriptografar ConnectionString
                var connectionString = Criptografia.Desencripitar(
                    empresa.ConnectionString
                );

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError(
                        "ConnectionString vazia após descriptografia - EmpresaId: {Id}",
                        empresa.Id
                    );
                    return false;
                }

                // 7. Conectar ao banco do tenant
                var database = ConectarAoBanco(
                    connectionString,
                    empresa.NomeBaseDados
                );

                // 8. Armazenar contexto do tenant
                _tenantAtualId = empresa.Id;
                _connectionStringAtual = connectionString;
                _databaseAtual = database;

                _logger.LogInformation(
                    "Tenant resolvido com sucesso - EmpresaId: {EmpresaId}, Database: {Database}, Session: {Session}",
                    empresa.Id,
                    empresa.NomeBaseDados,
                    sessionName
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao resolver tenant - Session: {Session}",
                    sessionName
                );
                return false;
            }
        }

        public async Task<bool> ResolverPorNumeroWhatsAppAsync(
            string numeroWhatsApp,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Resolvendo tenant para número: {Numero}",
                    numeroWhatsApp
                );

                // 1. Buscar empresa no banco ADM pelo número do WhatsApp
                var empresa = await _empresaRepository
                    .BuscarPorNumeroWhatsAppAsync(numeroWhatsApp, cancellationToken);

                if (empresa == null)
                {
                    _logger.LogWarning(
                        "Empresa não encontrada para número: {Numero}",
                        numeroWhatsApp
                    );
                    return false;
                }

                // 2. Verificar se empresa está ativa
                if (!empresa.FlgAtivo)
                {
                    _logger.LogWarning(
                        "Empresa inativa - Id: {EmpresaId}, Numero: {Numero}",
                        empresa.Id,
                        numeroWhatsApp
                    );
                    return false;
                }

                // 3. Verificar se está suspensa
                if (empresa.FlgSuspensa)
                {
                    _logger.LogWarning(
                        "Empresa suspensa - Id: {EmpresaId}, Numero: {Numero}",
                        empresa.Id,
                        numeroWhatsApp
                    );
                    return false;
                }

                // 4. Verificar se WAHA está ativo
                if (!empresa.FlgWahaAtivo)
                {
                    _logger.LogWarning(
                        "WAHA inativo para empresa - Id: {EmpresaId}",
                        empresa.Id
                    );
                    return false;
                }

                // 5. ✅ PROVISIONAMENTO AUTOMÁTICO se não tem banco configurado
                if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    _logger.LogWarning(
                        "Empresa {EmpresaId} sem banco configurado. Provisionando automaticamente...",
                        empresa.Id);

                    var resultadoProvisionamento = await _databaseProvisioningService
                        .ProvisionarBancoDadosEmpresaAsync(
                            empresa.Id,
                            empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                    if (!resultadoProvisionamento.Sucesso)
                    {
                        _logger.LogError(
                            "Falha ao provisionar banco para empresa {EmpresaId}: {Erro}",
                            empresa.Id,
                            resultadoProvisionamento.Erro);
                        return false;
                    }

                    _logger.LogInformation(
                        "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                        empresa.Id,
                        resultadoProvisionamento.NomeBaseDados);

                    // Buscar empresa atualizada com connection string criptografada
                    empresa = await _empresaRepository.BuscarPorIdAsync(empresa.Id);
                    if (empresa == null)
                    {
                        _logger.LogError("Erro ao buscar empresa após provisionamento");
                        return false;
                    }
                }

                // 6. Descriptografar ConnectionString
                var connectionString = Criptografia.Desencripitar(
                    empresa.ConnectionString
                );

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError(
                        "ConnectionString vazia após descriptografia - EmpresaId: {Id}",
                        empresa.Id
                    );
                    return false;
                }
                var x = 5 * 2;
                // 7. Conectar ao banco do tenant
                var database = ConectarAoBanco(
                    connectionString,  
                    empresa.NomeBaseDados
                );

                // 8. Armazenar contexto do tenant
                _tenantAtualId = empresa.Id;
                _connectionStringAtual = connectionString;
                _databaseAtual = database;

                _logger.LogInformation(
                    "Tenant resolvido com sucesso - EmpresaId: {EmpresaId}, Database: {Database}",
                    empresa.Id,
                    empresa.NomeBaseDados
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao resolver tenant - Numero: {Numero}",
                    numeroWhatsApp
                );
                return false;
            }
        }

        public async Task<bool> ResolverPorEmpresaIdAsync(
            string empresaId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Resolvendo tenant para empresaId: {EmpresaId}",
                    empresaId
                );

                // 1. Buscar empresa no banco ADM pelo ID
                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);

                if (empresa == null)
                {
                    _logger.LogWarning(
                        "Empresa não encontrada para empresaId: {EmpresaId}",
                        empresaId
                    );
                    return false;
                }

                // 2. Verificar se empresa está ativa
                if (!empresa.FlgAtivo)
                {
                    _logger.LogWarning(
                        "Empresa inativa - Id: {EmpresaId}",
                        empresa.Id
                    );
                    return false;
                }

                // 3. Verificar se está suspensa
                if (empresa.FlgSuspensa)
                {
                    _logger.LogWarning(
                        "Empresa suspensa - Id: {EmpresaId}",
                        empresa.Id
                    );
                    return false;
                }

                // 5. ✅ PROVISIONAMENTO AUTOMÁTICO se não tem banco configurado
                if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    _logger.LogWarning(
                        "Empresa {EmpresaId} sem banco configurado. Provisionando automaticamente...",
                        empresa.Id);

                    var resultadoProvisionamento = await _databaseProvisioningService
                        .ProvisionarBancoDadosEmpresaAsync(
                            empresa.Id,
                            empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                    if (!resultadoProvisionamento.Sucesso)
                    {
                        _logger.LogError(
                            "Falha ao provisionar banco para empresa {EmpresaId}: {Erro}",
                            empresa.Id,
                            resultadoProvisionamento.Erro);
                        return false;
                    }

                    _logger.LogInformation(
                        "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                        empresa.Id,
                        resultadoProvisionamento.NomeBaseDados);

                    // Buscar empresa atualizada com connection string criptografada
                    empresa = await _empresaRepository.BuscarPorIdAsync(empresa.Id);
                    if (empresa == null)
                    {
                        _logger.LogError("Erro ao buscar empresa após provisionamento");
                        return false;
                    }
                }

                // 6. Descriptografar ConnectionString
                var connectionString = Criptografia.Desencripitar(
                    empresa.ConnectionString
                );

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError(
                        "ConnectionString vazia após descriptografia - EmpresaId: {Id}",
                        empresa.Id
                    );
                    return false;
                }

                // 7. Conectar ao banco do tenant
                var database = ConectarAoBanco(
                    connectionString,
                    empresa.NomeBaseDados
                );

                // 8. Armazenar contexto do tenant
                _tenantAtualId = empresa.Id;
                _connectionStringAtual = connectionString;
                _databaseAtual = database;

                _logger.LogInformation(
                    "Tenant resolvido com sucesso - EmpresaId: {EmpresaId}, Database: {Database}",
                    empresa.Id,
                    empresa.NomeBaseDados
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao resolver tenant - EmpresaId: {EmpresaId}",
                    empresaId
                );
                return false;
            }
        }

        public string? ObterTenantAtualId()
        {
            return _tenantAtualId;
        }

        public IMongoDatabase? ObterDatabaseAtual()
        {
            return _databaseAtual;
        }

        public string? ObterConnectionStringAtual()
        {
            return _connectionStringAtual;
        }

        public void LimparContexto()
        {
            _tenantAtualId = null;
            _connectionStringAtual = null;
            _databaseAtual = null;

            _logger.LogDebug("Contexto do tenant limpo");
        }

        #region Métodos Privados

        private IMongoDatabase ConectarAoBanco(string connectionString, string nomeDatabase)
        {
            try
            {
                var client = _mongoClientFactory.CriarClient(connectionString);
                var database = client.GetDatabase(nomeDatabase);

                // Testar conexão
                database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}").Wait();

                _logger.LogInformation(
                    "Conectado ao banco do tenant - Database: {Database}",
                    nomeDatabase
                );

                return database;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao conectar ao banco - Database: {Database}",
                    nomeDatabase
                );
                throw;
            }
        }

        #endregion
    }

    #region Interfaces

    public interface ITenantResolverService
    {
        /// <summary>
        /// Resolve o tenant baseado no nome da session WAHA e conecta ao banco correto
        /// </summary>
        Task<bool> ResolverPorSessionAsync(
            string sessionName,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Resolve o tenant baseado no número do WhatsApp e conecta ao banco correto
        /// </summary>
        Task<bool> ResolverPorNumeroWhatsAppAsync(
            string numeroWhatsApp,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Resolve o tenant baseado no ID da empresa e conecta ao banco correto
        /// </summary>
        Task<bool> ResolverPorEmpresaIdAsync(
            string empresaId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Obtém o ID do tenant atual da requisição
        /// </summary>
        string? ObterTenantAtualId();

        /// <summary>
        /// Obtém o database MongoDB do tenant atual
        /// </summary>
        IMongoDatabase? ObterDatabaseAtual();

        /// <summary>
        /// Obtém a connection string do tenant atual
        /// </summary>
        string? ObterConnectionStringAtual();

        /// <summary>
        /// Limpa o contexto do tenant (fim da requisição)
        /// </summary>
        void LimparContexto();
    }

    public interface IMongoClientFactory
    {
        IMongoClient CriarClient(string connectionString);
    }

    public class MongoClientFactory : IMongoClientFactory
    {
        private readonly Dictionary<string, IMongoClient> _clientsCache = new();
        private readonly object _lock = new();

        public IMongoClient CriarClient(string connectionString)
        {
            lock (_lock)
            {
                // Usar cache para evitar criar múltiplos clients para mesma connection string
                if (!_clientsCache.ContainsKey(connectionString))
                {
                    var settings = MongoClientSettings.FromConnectionString(connectionString);
                    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
                    settings.ConnectTimeout = TimeSpan.FromSeconds(5);

                    _clientsCache[connectionString] = new MongoClient(settings);
                }

                return _clientsCache[connectionString];
            }
        }
    }

    #endregion
}