namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço de cache para autenticação de webhook
    /// </summary>
    public interface IWebhookAuthCacheService
    {
        /// <summary>
        /// Obtém dados de autenticação do cache ou do banco de dados
        /// </summary>
        Task<WebhookAuthResult?> GetOrCreateAuthDataAsync(string empresaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalida o cache para uma empresa específica
        /// </summary>
        void InvalidateCache(string empresaId);

        /// <summary>
        /// Limpa todo o cache
        /// </summary>
        void ClearCache();
    }
}
