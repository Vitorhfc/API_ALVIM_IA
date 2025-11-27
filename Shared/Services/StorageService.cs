using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Services.Interface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Shared.Services
{
    /// <summary>
    /// Serviço de armazenamento usando Wasabi S3
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageService> _logger;
        private readonly string _bucketName = "diana-ia";

        public StorageService(IConfiguration configuration, ILogger<StorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private AmazonS3Client GetS3Client()
        {
            var accessKey = _configuration["WasabiStorage:AccessKey"];
            var secretKey = _configuration["WasabiStorage:SecretKey"];
            var serviceUrl = _configuration["WasabiStorage:ServiceUrl"] ?? "https://s3.us-east-1.wasabisys.com";

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("Credenciais do Wasabi não configuradas no appsettings.json");
            }

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };

            return new AmazonS3Client(accessKey, secretKey, config);
        }

        private string GetPresignedUrl(string key, TimeSpan? expiry = null)
        {
            using var s3Client = GetS3Client();

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(365)),
                Verb = HttpVerb.GET
            };

            return s3Client.GetPreSignedURL(request);
        }

        public async Task<string> UploadArquivoAsync(byte[] fileData, string nomeArquivo, string container)
        {
            try
            {
                nomeArquivo = SanitizeFileName(nomeArquivo);
                string key = $"{container}/{nomeArquivo}";

                using var s3Client = GetS3Client();

                using (var stream = new MemoryStream(fileData))
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = GetContentType(nomeArquivo),
                        ServerSideEncryptionMethod = ServerSideEncryptionMethod.None
                    };

                    await s3Client.PutObjectAsync(request);
                }

                _logger.LogInformation("Arquivo {NomeArquivo} enviado com sucesso para {Container}", nomeArquivo, container);

                return GetPresignedUrl(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do arquivo {NomeArquivo}", nomeArquivo);
                throw;
            }
        }

        public async Task<string> UploadArquivoAsync(IFormFile arquivo, string container)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    throw new ArgumentException("Arquivo vazio ou nulo");

                using var memoryStream = new MemoryStream();
                await arquivo.CopyToAsync(memoryStream);

                var nomeArquivo = $"{Guid.NewGuid()}_{arquivo.FileName}";

                return await UploadArquivoAsync(memoryStream.ToArray(), nomeArquivo, container);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do arquivo via IFormFile");
                throw;
            }
        }

        public async Task<string> UploadImagemAsync(byte[] imageData, string imageType, string container)
        {
            try
            {
                imageType = imageType.Replace("image/", "");
                string filename = $"{Guid.NewGuid()}.{imageType.ToLower()}";
                filename = SanitizeFileName(filename);
                string key = $"{container}/{filename}";

                using var s3Client = GetS3Client();

                using (var image = Image.Load(imageData))
                {
                    // Redimensionar se maior que 1920px
                    int maxWidth = 1920;
                    if (image.Width > maxWidth)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth, 0)
                        }));
                    }

                    using (var stream = new MemoryStream())
                    {
                        switch (imageType.ToLower())
                        {
                            case "jpeg":
                            case "jpg":
                                var jpegEncoder = new JpegEncoder { Quality = 85 };
                                image.Save(stream, jpegEncoder);
                                break;
                            case "png":
                                var pngEncoder = new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression };
                                image.Save(stream, pngEncoder);
                                break;
                            case "webp":
                                var webpEncoder = new WebpEncoder { Quality = 80 };
                                image.Save(stream, webpEncoder);
                                break;
                            default:
                                throw new NotSupportedException($"Tipo de imagem '{imageType}' não suportado");
                        }

                        stream.Seek(0, SeekOrigin.Begin);

                        var request = new PutObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = key,
                            InputStream = stream,
                            ContentType = $"image/{imageType.ToLower()}",
                            ServerSideEncryptionMethod = ServerSideEncryptionMethod.None
                        };

                        await s3Client.PutObjectAsync(request);
                    }
                }

                _logger.LogInformation("Imagem enviada com sucesso: {Filename}", filename);

                return GetPresignedUrl(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload da imagem");
                throw;
            }
        }

        public async Task<string> UploadDocumentoAsync(IFormFile documento, string container)
        {
            try
            {
                if (documento == null || documento.Length == 0)
                    throw new ArgumentException("Documento vazio ou nulo");

                // Validar tipo de documento
                var extensao = Path.GetExtension(documento.FileName).ToLower();
                var extensoesPermitidas = new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".txt", ".csv" };

                if (!extensoesPermitidas.Contains(extensao))
                {
                    throw new ArgumentException($"Tipo de arquivo '{extensao}' não permitido. Permitidos: {string.Join(", ", extensoesPermitidas)}");
                }

                // Limitar tamanho (50MB)
                if (documento.Length > 50 * 1024 * 1024)
                {
                    throw new ArgumentException("Arquivo muito grande. Tamanho máximo: 50MB");
                }

                using var memoryStream = new MemoryStream();
                await documento.CopyToAsync(memoryStream);

                var nomeArquivo = $"{Guid.NewGuid()}_{documento.FileName}";

                return await UploadArquivoAsync(memoryStream.ToArray(), nomeArquivo, container);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do documento");
                throw;
            }
        }

        public async Task<byte[]> DownloadArquivoAsync(string url)
        {
            try
            {
                var key = ExtrairKeyDaUrl(url);

                using var s3Client = GetS3Client();

                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                using var response = await s3Client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);

                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException("Arquivo não encontrado no storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer download do arquivo");
                throw;
            }
        }

        public async Task DeletarArquivoAsync(string url, string container)
        {
            try
            {
                var key = ExtrairKeyDaUrl(url);

                using var s3Client = GetS3Client();

                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                await s3Client.DeleteObjectAsync(request);

                _logger.LogInformation("Arquivo deletado com sucesso: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar arquivo");
                throw;
            }
        }

        public string GerarUrlPreAssinada(string url, TimeSpan? expiracao = null)
        {
            try
            {
                var key = ExtrairKeyDaUrl(url);
                return GetPresignedUrl(key, expiracao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar URL pré-assinada");
                throw;
            }
        }

        #region Métodos Auxiliares

        private string SanitizeFileName(string filename)
        {
            return filename
                .Replace(" ", "-")
                .Replace("..", ".")
                .Replace("/", "-")
                .Replace("\\", "-");
        }

        private string GetContentType(string filename)
        {
            var extensao = Path.GetExtension(filename).ToLower();

            return extensao switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                _ => "application/octet-stream"
            };
        }

        private string ExtrairKeyDaUrl(string url)
        {
            // Extrair a chave do URI (funciona com URL pública ou pré-assinada)
            if (url.Contains("?"))
            {
                // URL pré-assinada - remover parâmetros
                url = url.Split('?')[0];
            }

            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimStart('/');

            // Remover o nome do bucket se estiver no path
            if (path.StartsWith(_bucketName + "/"))
            {
                path = path.Substring(_bucketName.Length + 1);
            }

            return path;
        }

        #endregion
    }
}
