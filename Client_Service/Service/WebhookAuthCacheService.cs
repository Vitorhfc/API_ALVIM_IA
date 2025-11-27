using Client_Service.Service.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Client_Service.Service
{
    /// <summary>
    /// Serviço de cache em memória para autenticação de webhook
    /// Reduz consultas ao banco de dados para empresas que recebem webhooks frequentemente
    /// </summary>
    public class WebhookAuthCacheService : IWebhookAuthCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebhookAuthService _webhookAuthService;
        private readonly ILogger<WebhookAuthCacheService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public WebhookAuthCacheService(
            IMemoryCache cache,
            IWebhookAuthService webhookAuthService,
            ILogger<WebhookAuthCacheService> logger)
        {
            _cache = cache;
            _webhookAuthService = webhookAuthService;
            _logger = logger;
        }

        public async Task<WebhookAuthResult?> GetOrCreateAuthDataAsync(
            string empresaId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(empresaId))
            {
                _logger.LogWarning("EmpresaId vazio ao buscar cache de autenticação");
                return null;
            }

            var cacheKey = $"webhook_auth_{empresaId}";

            // Tentar obter do cache
            if (_cache.TryGetValue(cacheKey, out WebhookAuthResult? cachedResult))
            {
                _logger.LogDebug("Cache HIT para empresa: {EmpresaId}", empresaId);
                return cachedResult;
            }

            _logger.LogDebug("Cache MISS para empresa: {EmpresaId}. Buscando no banco...", empresaId);

            // Se não está no cache, buscar do serviço de autenticação
            var authResult = await _webhookAuthService.AutenticarPorEmpresaIdAsync(empresaId);

            if (authResult != null && authResult.Sucesso)
            {
                // Armazenar no cache com expiração
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_cacheExpiration)
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(cacheKey, authResult, cacheOptions);

                _logger.LogInformation(
                    "Dados de autenticação armazenados em cache - EmpresaId: {EmpresaId}, Expiração: {Expiracao}min",
                    empresaId,
                    _cacheExpiration.TotalMinutes
                );
            }

            return authResult;
        }

        public void InvalidateCache(string empresaId)
        {
            if (string.IsNullOrWhiteSpace(empresaId))
                return;

            var cacheKey = $"webhook_auth_{empresaId}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Cache invalidado para empresa: {EmpresaId}", empresaId);
        }

        public void ClearCache()
        {
            // MemoryCache não tem método Clear nativo
            // Uma alternativa seria manter lista de chaves, mas por simplicidade vamos apenas logar
            _logger.LogWarning("Solicitação de limpeza de cache. Nota: MemoryCache não suporta Clear global.");
        }
    }
}
