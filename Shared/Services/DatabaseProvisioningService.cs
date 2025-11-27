using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;
using Shared.Services.Interface;
using Shared.Utils.Criptografia;
using System.Text.RegularExpressions;

namespace Shared.Services
{
    /// <summary>
    /// Serviço de provisionamento automático de banco de dados para empresas
    /// </summary>
    public class DatabaseProvisioningService : IDatabaseProvisioningService
    {
        private readonly IMongoDatabase _adminDatabase;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseProvisioningService> _logger;
        private readonly string _baseConnectionString;

        public DatabaseProvisioningService(
            IMongoDatabase adminDatabase,
            IConfiguration configuration,
            ILogger<DatabaseProvisioningService> logger)
        {
            _adminDatabase = adminDatabase;
            _configuration = configuration;
            _logger = logger;

            // Obter connection string base do appsettings
            _baseConnectionString = _configuration["MongoDBSettings:ConnectionString"]
                ?? throw new InvalidOperationException("Connection string base não configurada no appsettings");
        }

        public async Task<DatabaseProvisioningResult> ProvisionarBancoDadosEmpresaAsync(string empresaId, string empresaNome)
        {
            try
            {
                _logger.LogInformation(
                    "Iniciando provisionamento de banco de dados - EmpresaId: {EmpresaId}, Nome: {Nome}",
                    empresaId,
                    empresaNome);

                // 1. Buscar empresa
                var empresaCollection = _adminDatabase.GetCollection<Empresa>("Empresa");
                var filter = Builders<Empresa>.Filter.Eq("Id", empresaId);
                var empresa = await empresaCollection.Find(filter).FirstOrDefaultAsync();

                if (empresa == null)
                {
                    return new DatabaseProvisioningResult
                    {
                        Sucesso = false,
                        Erro = $"Empresa com ID {empresaId} não encontrada",
                        DataProvisionamento = DateTime.UtcNow
                    };
                }

                // 2. Verificar se já tem banco provisionado
                if (!string.IsNullOrWhiteSpace(empresa.ConnectionString) &&
                    !string.IsNullOrWhiteSpace(empresa.NomeBaseDados))
                {
                    _logger.LogInformation(
                        "Empresa {EmpresaId} já possui banco provisionado: {Database}",
                        empresaId,
                        empresa.NomeBaseDados);

                    // Descriptografar para retornar
                    var connectionStringExistente = Criptografia.Desencripitar(empresa.ConnectionString);

                    return new DatabaseProvisioningResult
                    {
                        Sucesso = true,
                        ConnectionString = connectionStringExistente,
                        NomeBaseDados = empresa.NomeBaseDados,
                        Mensagem = "Banco de dados já estava provisionado",
                        DataProvisionamento = DateTime.UtcNow
                    };
                }

                // 3. Gerar nome do banco de dados
                var nomeBaseDados = GerarNomeBaseDados(empresaId, empresaNome);

                // 4. Gerar connection string
                var connectionString = GerarConnectionString(nomeBaseDados);

                // 5. Testar connection string
                var testeConexao = await TestarConnectionStringAsync(connectionString);
                if (!testeConexao)
                {
                    return new DatabaseProvisioningResult
                    {
                        Sucesso = false,
                        Erro = "Falha ao conectar com o MongoDB usando a connection string gerada",
                        DataProvisionamento = DateTime.UtcNow
                    };
                }

                // 6. Criar banco de dados com log inicial
                var criouBanco = await CriarBancoDadosComLogAsync(connectionString, nomeBaseDados, empresaId);
                if (!criouBanco)
                {
                    return new DatabaseProvisioningResult
                    {
                        Sucesso = false,
                        Erro = "Falha ao criar banco de dados e coleção de log",
                        DataProvisionamento = DateTime.UtcNow
                    };
                }

                // 7. Criptografar connection string
                var connectionStringCriptografada = Criptografia.Encriptar(connectionString);
                var teste = Criptografia.Desencripitar(connectionStringCriptografada);

                // 8. Atualizar empresa no banco
                empresa.ConnectionString = connectionStringCriptografada;
                empresa.NomeBaseDados = nomeBaseDados;
                empresa.DtaAlteracao = DateTime.UtcNow;

                var update = Builders<Empresa>.Update
                    .Set(e => e.ConnectionString, connectionStringCriptografada)
                    .Set(e => e.NomeBaseDados, nomeBaseDados)
                    .Set(e => e.DtaAlteracao, DateTime.UtcNow);

                await empresaCollection.UpdateOneAsync(filter, update);

                _logger.LogInformation(
                    "Provisionamento concluído com sucesso - EmpresaId: {EmpresaId}, Database: {Database}",
                    empresaId,
                    nomeBaseDados);

                return new DatabaseProvisioningResult
                {
                    Sucesso = true,
                    ConnectionString = connectionString, // Retorna descriptografada
                    NomeBaseDados = nomeBaseDados,
                    Mensagem = "Banco de dados provisionado com sucesso",
                    DataProvisionamento = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao provisionar banco de dados para empresa {EmpresaId}", empresaId);

                return new DatabaseProvisioningResult
                {
                    Sucesso = false,
                    Erro = $"Erro ao provisionar banco: {ex.Message}",
                    DataProvisionamento = DateTime.UtcNow
                };
            }
        }

        public string GerarConnectionString(string nomeBaseDados)
        {
            try
            {
                // Extrair partes da connection string base
                // Exemplo: mongodb+srv://user:pass@cluster.mongodb.net/Alvim_Admin?options
                var match = Regex.Match(
                    _baseConnectionString,
                    @"^(mongodb(?:\+srv)?://[^/]+/)([^?]+)(\?.+)?$");

                if (!match.Success)
                {
                    throw new InvalidOperationException("Connection string base em formato inválido");
                }

                var baseUrl = match.Groups[1].Value;  // mongodb+srv://user:pass@cluster.mongodb.net/
                var options = match.Groups[3].Value;  // ?retryWrites=true&...

                // Montar nova connection string com o nome do banco do cliente
                var novaConnectionString = $"{baseUrl}{nomeBaseDados}";

                _logger.LogInformation(
                    "Connection string gerada para banco: {Database}",
                    nomeBaseDados);

                return novaConnectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar connection string para banco: {Database}", nomeBaseDados);
                throw;
            }
        }

        public string GerarNomeBaseDados(string empresaId, string empresaNome)
        {
            try
            {
                // Limpar nome da empresa (remover caracteres especiais)
                var nomeLimpo = Regex.Replace(empresaNome, @"[^a-zA-Z0-9]", "");

                // Limitar tamanho
                if (nomeLimpo.Length > 20)
                {
                    nomeLimpo = nomeLimpo.Substring(0, 20);
                }

                // Pegar primeiros 8 caracteres do ID da empresa
                var empresaIdCurto = empresaId.Length > 8 ? empresaId.Substring(0, 8) : empresaId;

                // Formato: Alvim_Client_{NomeEmpresa}_{IdCurto}
                var nomeBaseDados = $"Alvim-{empresaIdCurto}_{nomeLimpo}";

                _logger.LogInformation(
                    "Nome do banco gerado: {Database} para empresa {EmpresaId}",
                    nomeBaseDados,
                    empresaId);

                return nomeBaseDados;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao gerar nome do banco para empresa {EmpresaId}",
                    empresaId);
                throw;
            }
        }

        public async Task<bool> CriarBancoDadosComLogAsync(
            string connectionString,
            string nomeBaseDados,
            string empresaId)
        {
            try
            {
                _logger.LogInformation(
                    "Criando banco de dados e coleção de log - Database: {Database}",
                    nomeBaseDados);

                // Conectar ao MongoDB
                var mongoClient = new MongoClient(connectionString);
                var database = mongoClient.GetDatabase(nomeBaseDados);

                // Criar coleção de log de provisionamento
                var collectionName = "LogProvisionamento";

                // Verificar se coleção já existe
                var collections = await database.ListCollectionNamesAsync();
                var collectionsList = await collections.ToListAsync();

                if (!collectionsList.Contains(collectionName))
                {
                    await database.CreateCollectionAsync(collectionName);
                    _logger.LogInformation("Coleção {Collection} criada no banco {Database}", collectionName, nomeBaseDados);
                }

                // Inserir documento inicial de log
                var logCollection = database.GetCollection<BsonDocument>(collectionName);

                var logDocument = new BsonDocument
                {
                    { "EmpresaId", empresaId },
                    { "Evento", "DatabaseProvisioned" },
                    { "Mensagem", "Banco de dados provisionado com sucesso" },
                    { "DataCriacao", DateTime.UtcNow },
                    { "Versao", "1.0" },
                    { "NomeBaseDados", nomeBaseDados }
                };

                await logCollection.InsertOneAsync(logDocument);

                _logger.LogInformation(
                    "Log de provisionamento inserido no banco {Database}",
                    nomeBaseDados);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao criar banco de dados {Database}",
                    nomeBaseDados);
                return false;
            }
        }

        public async Task<bool> TestarConnectionStringAsync(string connectionString)
        {
            try
            {
                _logger.LogInformation("Testando connection string...");

                var mongoClient = new MongoClient(connectionString);

                // Tentar executar comando ping
                var database = mongoClient.GetDatabase("admin");
                await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

                _logger.LogInformation("Connection string testada com sucesso");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao testar connection string");
                return false;
            }
        }
    }
}
