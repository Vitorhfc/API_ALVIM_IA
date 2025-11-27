using Client_Repository.Cache.Interface;
using MongoDB.Driver;
using Shared.Classes.Model;
using System.Collections.Concurrent;

namespace Client_Repository.Cache
{
    public class TenantCache : ITenantCache, IDisposable
    {
        private readonly ConcurrentDictionary<string, TenantInfo> _tenants = new();
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        public TenantCache()
        {
            _cleanupTimer = new Timer(
                CleanupExpiredTenants,
                null,
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30));
        }

        public void SetTenant(string usuarioId, string empresaId, string connectionString, string nomeBaseDados)
        {
            var cacheKey = GerarChaveCache(usuarioId, empresaId);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            ConfigurarSettingsMongoClient(mongoClientSettings);

            var tenantInfo = new TenantInfo
            {
                UsuarioId = usuarioId,
                EmpresaId = empresaId,
                ConnectionString = connectionString,
                NomeBaseDados = nomeBaseDados,
                MongoClient = new MongoClient(mongoClientSettings),
                UltimoAcesso = DateTime.UtcNow
            };

            _tenants.AddOrUpdate(cacheKey, tenantInfo, (key, old) =>
            {
                old.UltimoAcesso = DateTime.UtcNow;
                return old;
            });
        }

        public TenantInfo? GetTenant(string usuarioId, string empresaId)
        {
            var cacheKey = GerarChaveCache(usuarioId, empresaId);

            if (_tenants.TryGetValue(cacheKey, out var tenant))
            {
                tenant.UltimoAcesso = DateTime.UtcNow;
                return tenant;
            }

            return null;
        }

        public bool HasTenant(string usuarioId, string empresaId)
        {
            var cacheKey = GerarChaveCache(usuarioId, empresaId);
            return _tenants.ContainsKey(cacheKey);
        }

        public void RemoveTenant(string usuarioId, string empresaId)
        {
            var cacheKey = GerarChaveCache(usuarioId, empresaId);
            _tenants.TryRemove(cacheKey, out _);
        }

        public void RemoveAllUserTenants(string usuarioId)
        {
            var chaves = _tenants.Keys
                .Where(k => k.StartsWith($"{usuarioId}:"))
                .ToList();

            foreach (var chave in chaves)
                _tenants.TryRemove(chave, out _);
        }

        private void CleanupExpiredTenants(object? state)
        {
            var limiteExpiracaoHoras = 2;
            var tempoExpirado = DateTime.UtcNow.AddHours(-limiteExpiracaoHoras);
            var tenantsExpirados = _tenants
                .Where(kvp => kvp.Value.UltimoAcesso < tempoExpirado)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var chave in tenantsExpirados)
                _tenants.TryRemove(chave, out _);
        }

        private static void ConfigurarSettingsMongoClient(MongoClientSettings settings)
        {
            settings.ConnectTimeout = TimeSpan.FromSeconds(30);
            settings.SocketTimeout = TimeSpan.FromSeconds(30);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
            settings.MaxConnectionPoolSize = 50;
            settings.MinConnectionPoolSize = 5;
            settings.WaitQueueTimeout = TimeSpan.FromSeconds(30);
            settings.RetryWrites = true;
            settings.RetryReads = true;
            settings.HeartbeatInterval = TimeSpan.FromSeconds(10);
            settings.HeartbeatTimeout = TimeSpan.FromSeconds(20);
        }

        private static string GerarChaveCache(string usuarioId, string empresaId)
            => $"{usuarioId}:{empresaId}";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                _cleanupTimer?.Dispose();

            _disposed = true;
        }
    }
}