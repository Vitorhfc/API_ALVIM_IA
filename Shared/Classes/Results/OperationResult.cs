namespace Shared.Classes.Results
{
    /// <summary>
    /// Result Pattern genérico para operações
    /// </summary>
    /// <typeparam name="T">Tipo do dado de retorno</typeparam>
    public class OperationResult<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }

        /// <summary>
        /// Cria um resultado de sucesso
        /// </summary>
        public static OperationResult<T> Ok(T data, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult<T>
            {
                Success = true,
                Data = data,
                Metadata = metadata
            };
        }

        /// <summary>
        /// Cria um resultado de erro
        /// </summary>
        public static OperationResult<T> Fail(string errorCode, string errorMessage, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult<T>
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                Metadata = metadata
            };
        }

        /// <summary>
        /// Cria um resultado de erro com exceção
        /// </summary>
        public static OperationResult<T> Fail(string errorCode, Exception exception)
        {
            return new OperationResult<T>
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = exception.Message,
                Metadata = new Dictionary<string, object>
                {
                    { "ExceptionType", exception.GetType().Name },
                    { "StackTrace", exception.StackTrace ?? string.Empty }
                }
            };
        }
    }

    /// <summary>
    /// Result Pattern sem dados de retorno
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }

        public static OperationResult Ok(Dictionary<string, object>? metadata = null)
        {
            return new OperationResult
            {
                Success = true,
                Metadata = metadata
            };
        }

        public static OperationResult Fail(string errorCode, string errorMessage, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                Metadata = metadata
            };
        }

        public static OperationResult Fail(string errorCode, Exception exception)
        {
            return new OperationResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = exception.Message,
                Metadata = new Dictionary<string, object>
                {
                    { "ExceptionType", exception.GetType().Name },
                    { "StackTrace", exception.StackTrace ?? string.Empty }
                }
            };
        }
    }
}
