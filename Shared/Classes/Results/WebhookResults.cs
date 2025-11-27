using Shared.Classes.Model;

namespace Shared.Classes.Results
{
    /// <summary>
    /// Resultado da validação do webhook
    /// </summary>
    public class WebhookValidationResult
    {
        public bool IsValid { get; init; }
        public bool ShouldIgnore { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public WebhookWaHaRequest? WebhookRequest { get; init; }

        public static WebhookValidationResult Valid(WebhookWaHaRequest request)
        {
            return new WebhookValidationResult
            {
                IsValid = true,
                ShouldIgnore = false,
                WebhookRequest = request
            };
        }

        public static WebhookValidationResult Ignore(WebhookWaHaRequest request, string reason)
        {
            return new WebhookValidationResult
            {
                IsValid = true,
                ShouldIgnore = true,
                ErrorMessage = reason,
                WebhookRequest = request
            };
        }

        public static WebhookValidationResult Invalid(string errorCode, string errorMessage)
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ShouldIgnore = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Resultado da autenticação do webhook
    /// </summary>
    public class WebhookAuthenticationResult
    {
        public bool Success { get; init; }
        public string? EmpresaId { get; init; }
        public string? UsuarioId { get; init; }
        public string? ErrorMessage { get; init; }

        public static WebhookAuthenticationResult Ok(string empresaId, string usuarioId)
        {
            return new WebhookAuthenticationResult
            {
                Success = true,
                EmpresaId = empresaId,
                UsuarioId = usuarioId
            };
        }

        public static WebhookAuthenticationResult Fail(string errorMessage)
        {
            return new WebhookAuthenticationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Resultado do processamento do webhook
    /// </summary>
    public class WebhookProcessingResult
    {
        public bool Success { get; init; }
        public string? ProcessingId { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }

        public static WebhookProcessingResult Ok(string processingId, Dictionary<string, object>? metadata = null)
        {
            return new WebhookProcessingResult
            {
                Success = true,
                ProcessingId = processingId,
                Metadata = metadata
            };
        }

        public static WebhookProcessingResult Fail(string errorCode, string errorMessage)
        {
            return new WebhookProcessingResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Resultado completo do webhook (orquestração)
    /// </summary>
    public class WebhookOrchestrationResult
    {
        public bool Success { get; init; }
        public string? ResponseType { get; init; }
        public object? ResponseData { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public static WebhookOrchestrationResult Ok(string responseType, object responseData)
        {
            return new WebhookOrchestrationResult
            {
                Success = true,
                ResponseType = responseType,
                ResponseData = responseData
            };
        }

        public static WebhookOrchestrationResult Fail(string errorCode, string errorMessage)
        {
            return new WebhookOrchestrationResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                ResponseType = errorCode,
                ResponseData = new { mensagem = errorMessage, erro = errorCode }
            };
        }
    }
}
