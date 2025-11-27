using Client_Repository.Cache.Interface;
using Client_Repository.Configuration.Contexto.Interface;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;
using Shared.Utils.Criptografia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Client_Repository.Configuration.Contexto
{
    /// <summary>
    /// ContextoMultiTenantService refatorado que resolve a dependência circular
    /// </summary>
    public class ContextoMultiTenantService : IContextoMultiTenantService, IDisposable
    {
        #region Campos Privados

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantCache _tenantCache;
        private readonly IMongoDatabase _adminDatabase;

        private static string? _currentUsuarioId;
        private static string? _currentEmpresaId;
        private static string? _currentConnectionString;
        private static string? _currentNomeBaseDados;

        private bool _disposed;

        #endregion

        #region Construtor

        public ContextoMultiTenantService(
            IHttpContextAccessor httpContextAccessor,
            ITenantCache tenantCache,
            IMongoDatabase adminDatabase)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenantCache = tenantCache;
            _adminDatabase = adminDatabase;
        }

        #endregion

        #region Métodos Públicos

        public string? ObterIdUsuarioAtual()
        {
            if (TentarObterIdUsuarioDoToken(out var usuarioId))
                return usuarioId;

            var contextId = _httpContextAccessor.HttpContext?.Items["UsuarioId"]?.ToString();
            if (!string.IsNullOrEmpty(contextId))
                return contextId;

            return _currentUsuarioId;
        }

        public string? ObterIdEmpresaAtual()
        {
            var contextId = _httpContextAccessor.HttpContext?.Items["EmpresaId"]?.ToString();
            if (!string.IsNullOrEmpty(contextId))
                return contextId;

            return _currentEmpresaId;
        }

        public string? ObterConnectionStringTenantAtual()
        {
            var contextString = _httpContextAccessor.HttpContext?.Items["ConnectionStringTenant"]?.ToString();
            if (!string.IsNullOrEmpty(contextString))
                return contextString;

            return _currentConnectionString;
        }

        public string? ObterNomeBaseDadosTenantAtual()
        {
            var contextName = _httpContextAccessor.HttpContext?.Items["NomeBaseDadosTenant"]?.ToString();
            if (!string.IsNullOrEmpty(contextName))
                return contextName;

            return _currentNomeBaseDados;
        }

        public void ConfigurarTenant(string usuarioId, string empresaId, string connectionString, string nomeBaseDados)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Items["UsuarioId"] = usuarioId;
                _httpContextAccessor.HttpContext.Items["EmpresaId"] = empresaId;
                _httpContextAccessor.HttpContext.Items["ConnectionStringTenant"] = connectionString;
                _httpContextAccessor.HttpContext.Items["NomeBaseDadosTenant"] = nomeBaseDados;
            }

            _currentUsuarioId = usuarioId;
            _currentEmpresaId = empresaId;
            _currentConnectionString = connectionString;
            _currentNomeBaseDados = nomeBaseDados;

            _tenantCache.SetTenant(usuarioId, empresaId, connectionString, nomeBaseDados);
        }

        public async Task<IMongoDatabase> ObterBaseDadosEmpresaAsync(string usuarioId, string empresaId)
        {
            var empresa = await BuscarEmpresaPorIdAsync(empresaId);
            if (empresa == null)
                throw new ApplicationException($"Empresa com ID {empresaId} não encontrada");

            if (string.IsNullOrEmpty(empresa.ConnectionString))
                throw new ApplicationException("Connection string da empresa não configurada");

            try
            {
                var connectionString = Criptografia.Desencripitar(empresa.ConnectionString);
                var mongoClient = new MongoClient(connectionString);
                return mongoClient.GetDatabase(empresa.NomeBaseDados);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao obter base de dados: {ex.Message}", ex);
            }
        }

        public async Task<IMongoDatabase> ObterBaseDadosAtualAsync()
        {
            await ValidarContextoTenantAsync();

            var usuarioId = ObterIdUsuarioAtual();
            var empresaId = ObterIdEmpresaAtual();

            if (string.IsNullOrEmpty(usuarioId) || string.IsNullOrEmpty(empresaId))
                throw new ApplicationException("Contexto de usuário e empresa não configurado");

            var tenantInfo = _tenantCache.GetTenant(usuarioId, empresaId);
            if (tenantInfo?.MongoClient != null)
                return tenantInfo.MongoClient.GetDatabase(tenantInfo.NomeBaseDados);

            return await ObterBaseDadosEmpresaAsync(usuarioId, empresaId);
        }

        public bool TemContextoUsuario(string usuarioId, string empresaId)
        {
            if (string.IsNullOrEmpty(usuarioId) || string.IsNullOrEmpty(empresaId))
                return false;

            return _tenantCache.HasTenant(usuarioId, empresaId) ||
                   (!string.IsNullOrEmpty(_currentUsuarioId) && !string.IsNullOrEmpty(_currentEmpresaId));
        }

        public void LimparContexto()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Items.Remove("UsuarioId");
                _httpContextAccessor.HttpContext.Items.Remove("EmpresaId");
                _httpContextAccessor.HttpContext.Items.Remove("ConnectionStringTenant");
                _httpContextAccessor.HttpContext.Items.Remove("NomeBaseDadosTenant");
            }

            _currentUsuarioId = null;
            _currentEmpresaId = null;
            _currentConnectionString = null;
            _currentNomeBaseDados = null;
        }

        public async Task ConfigurarContextoUsuarioAsync(string usuarioId, string empresaId)
        {
            await ValidarUsuarioEmpresaAsync(usuarioId, empresaId);

            var empresa = await BuscarEmpresaPorIdAsync(empresaId);
            if (empresa == null)
                throw new ApplicationException($"Empresa com ID {empresaId} não encontrada");

            try
            {
                var connectionString = Criptografia.Desencripitar(empresa.ConnectionString);
                ConfigurarTenant(usuarioId, empresaId, connectionString, empresa.NomeBaseDados);
                await TestarConexaoAsync(usuarioId, empresaId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao configurar contexto: {ex.Message}", ex);
            }
        }

        #endregion

        #region Métodos Privados

        private bool TentarObterIdUsuarioDoToken(out string? usuarioId)
        {
            usuarioId = null;

            if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
                return false;

            var claims = new[] { "UsuarioId", "IdUsuario", ClaimTypes.NameIdentifier, "sub" };

            foreach (var claimType in claims)
            {
                var claim = _httpContextAccessor.HttpContext.User.FindFirst(claimType);
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                {
                    usuarioId = claim.Value;
                    return true;
                }
            }

            return false;
        }

        private async Task ValidarContextoTenantAsync()
        {
            var usuarioId = ObterIdUsuarioAtual();
            var empresaId = ObterIdEmpresaAtual();

            if (string.IsNullOrEmpty(usuarioId) || string.IsNullOrEmpty(empresaId))
            {
                var isAuthenticated = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated;
                throw new UnauthorizedAccessException(
                    $"Contexto incompleto. UsuarioId: {usuarioId}, EmpresaId: {empresaId}, Autenticado: {isAuthenticated}");
            }

            if (!_tenantCache.HasTenant(usuarioId, empresaId))
                await ConfigurarContextoUsuarioAsync(usuarioId, empresaId);
        }

        private async Task ValidarUsuarioEmpresaAsync(string usuarioId, string empresaId)
        {
            var vinculo = await BuscarUsuarioEmpresaAsync(usuarioId, empresaId);
            if (vinculo == null)
                throw new UnauthorizedAccessException(
                    $"Usuário {usuarioId} não possui acesso à empresa {empresaId}");
        }

        private async Task TestarConexaoAsync(string usuarioId, string empresaId)
        {
            var tenantInfo = _tenantCache.GetTenant(usuarioId, empresaId);

            if (tenantInfo?.MongoClient == null)
                throw new ApplicationException("Cliente MongoDB não configurado");

            try
            {
                var database = tenantInfo.MongoClient.GetDatabase(tenantInfo.NomeBaseDados);
                await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                    new MongoDB.Bson.BsonDocument("ping", 1));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Falha ao conectar com base de dados: {ex.Message}", ex);
            }
        }

        public async Task<string?> BuscarEmpresaPorClienteIdAsync(string clienteId)
        {
            try
            {
                Console.WriteLine($"🔍 Buscando empresa para cliente: {clienteId}");

                // 1. Tentar buscar no mapeamento (método rápido)
                var collection = _adminDatabase.GetCollection<Shared.Classes.Entidades.ADM.ClienteEmpresaMap>("ClienteEmpresaMap");
                var mapeamento = await collection.Find(m => m.ClienteId == clienteId && m.FlgAtivo).FirstOrDefaultAsync();

                if (mapeamento != null)
                {
                    Console.WriteLine($"✅ Mapeamento encontrado! EmpresaId: {mapeamento.EmpresaId}");
                    return mapeamento.EmpresaId;
                }

                Console.WriteLine("⚠️ Mapeamento não encontrado. Buscando em todas as empresas...");

                // 2. Fallback: Buscar cliente em todas as empresas ativas (método lento)
                var empresasCollection = _adminDatabase.GetCollection<Shared.Classes.Entidades.ADM.Empresa>("Empresa");
                var empresas = await empresasCollection.Find(e => e.FlgAtivo).ToListAsync();

                Console.WriteLine($"📋 Total de empresas ativas para verificar: {empresas.Count}");

                foreach (var empresa in empresas)
                {
                    try
                    {
                        Console.WriteLine($"🔎 Verificando empresa: {empresa.Nome} (ID: {empresa.Id})");

                        // Conectar ao banco tenant da empresa
                        var connectionString = Criptografia.Desencripitar(empresa.ConnectionString);
                        var tenantClient = new MongoClient(connectionString);
                        var tenantDatabase = tenantClient.GetDatabase(empresa.NomeBaseDados);
                        var clientesCollection = tenantDatabase.GetCollection<BsonDocument>("Cliente");

                        // Tentar buscar cliente por _id como string primeiro (padrão da aplicação)
                        FilterDefinition<BsonDocument> clienteFilter;

                        // Tentar como string primeiro
                        clienteFilter = Builders<BsonDocument>.Filter.Eq("_id", clienteId);
                        var clienteExiste = await clientesCollection.Find(clienteFilter).AnyAsync();

                        // Se não encontrar como string, tentar como ObjectId
                        if (!clienteExiste && ObjectId.TryParse(clienteId, out ObjectId objectId))
                        {
                            Console.WriteLine($"   Tentando buscar como ObjectId...");
                            clienteFilter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                            clienteExiste = await clientesCollection.Find(clienteFilter).AnyAsync();
                        }

                        if (clienteExiste)
                        {
                            Console.WriteLine($"✅ Cliente encontrado na empresa: {empresa.Nome}");

                            // Cliente encontrado! Criar mapeamento para próximas consultas
                            var novoMap = new Shared.Classes.Entidades.ADM.ClienteEmpresaMap
                            {
                                ClienteId = clienteId,
                                EmpresaId = empresa.Id,
                                FlgAtivo = true,
                                DtaUltimaAtualizacao = DateTime.UtcNow,
                                DtaCadastro = DateTime.UtcNow
                            };
                            await collection.InsertOneAsync(novoMap);

                            Console.WriteLine($"💾 Mapeamento criado para futuras consultas");

                            return empresa.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erro ao verificar empresa {empresa.Nome}: {ex.Message}");
                        // Ignorar erros de empresas específicas e continuar buscando
                        continue;
                    }
                }

                Console.WriteLine($"❌ Cliente {clienteId} não encontrado em nenhuma empresa");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro crítico ao buscar empresa por cliente: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<string?> BuscarUsuarioAtivoDaEmpresaAsync(string empresaId)
        {
            try
            {
                Console.WriteLine($"👤 Buscando usuário ativo da empresa: {empresaId}");

                var collection = _adminDatabase.GetCollection<Shared.Classes.Entidades.ADM.UsuarioEmpresa>("UsuarioEmpresa");

                // Buscar primeiro por administrador ativo
                var usuarioEmpresa = await collection.Find(u =>
                    u.EmpresaId == empresaId
                ).FirstOrDefaultAsync();

                // Se não encontrar admin, buscar qualquer usuário ativo
                if (usuarioEmpresa == null)
                {
                    Console.WriteLine("   ⚠️ Admin não encontrado, buscando qualquer usuário ativo...");
                    usuarioEmpresa = await collection.Find(u =>
                        u.EmpresaId == empresaId &&
                        u.FlgAtivo
                    ).FirstOrDefaultAsync();
                }

                if (usuarioEmpresa != null)
                {
                    Console.WriteLine($"✅ Usuário encontrado: {usuarioEmpresa.UsuarioId} (Admin: {usuarioEmpresa.FlgAdministrador})");
                    return usuarioEmpresa.UsuarioId;
                }

                Console.WriteLine($"❌ Nenhum usuário ativo encontrado para a empresa {empresaId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao buscar usuário da empresa: {ex.Message}");
                return null;
            }
        }

        // Métodos para consultar diretamente o banco Admin
        private async Task<Shared.Classes.Entidades.ADM.Empresa?> BuscarEmpresaPorIdAsync(string empresaId)
        {
            var collection = _adminDatabase.GetCollection<Shared.Classes.Entidades.ADM.Empresa>("Empresa");
            return await collection.Find(e => e.Id == empresaId).FirstOrDefaultAsync();
        }

        private async Task<Shared.Classes.Entidades.ADM.UsuarioEmpresa?> BuscarUsuarioEmpresaAsync(string usuarioId, string empresaId)
        {
            var collection = _adminDatabase.GetCollection<UsuarioEmpresa>("UsuarioEmpresa");
            return await collection.Find(ue =>
                ue.UsuarioId == usuarioId &&
                ue.EmpresaId == empresaId
            ).FirstOrDefaultAsync();

        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        #endregion
    }
}
