using RestSharp;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Utils
{
    /// <summary>
    /// Helper centralizado para requisições HTTP usando RestSharp
    /// Compatível com RestSharp 106.11.7
    /// </summary>
    public static class HttpClientHelper
    {
        private const int DefaultTimeoutMilliseconds = 30000; // 30 segundos

        /// <summary>
        /// Envia requisição HTTP e deserializa a resposta
        /// </summary>
        public static async Task<T?> SendRequestAsync<T>(
            Method method,
            string endpoint,
            object? data = null,
            Dictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync(
                method,
                endpoint,
                data,
                headers,
                cancellationToken
            );

            if (response?.Content == null || !response.IsSuccessful)
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(
                    response.Content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Envia requisição HTTP e retorna IRestResponse (compatível com RestSharp 106.x)
        /// </summary>
        public static async Task<IRestResponse> SendRequestAsync(
            Method method,
            string endpoint,
            object? data = null,
            Dictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // O RestClient 106.x precisa apenas da base URL
                // O endpoint completo vai no RestRequest
                var client = new RestClient
                {
                    Timeout = DefaultTimeoutMilliseconds
                };

                var request = new RestRequest(endpoint, method);

                // Adicionar headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }

                // Adicionar body se houver
                if (data != null)
                {
                    request.AddJsonBody(data);
                }

                var response = await client.ExecuteAsync(request, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                // Retornar resposta de erro (compatível com RestSharp 106.x)
                return new RestResponse
                {
                    ErrorMessage = ex.Message,
                    ErrorException = ex,
                    ResponseStatus = ResponseStatus.Error
                };
            }
        }

        /// <summary>
        /// Envia requisição HTTP e retorna buffer de bytes (para download de mídia)
        /// </summary>
        public static async Task<byte[]?> SendBufferRequestAsync(
            Method method,
            string endpoint,
            Dictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = new RestClient
                {
                    Timeout = DefaultTimeoutMilliseconds
                };

                var request = new RestRequest(endpoint, method);

                // Adicionar headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }

                var response = await client.ExecuteAsync(request, cancellationToken);

                if (response.IsSuccessful && response.RawBytes != null)
                {
                    return response.RawBytes;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
