using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.Client;
using Shared.Services.Interface;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace Client_Service.Service
{
    public class ArquivoService : ServiceGenerico<Arquivo>, IArquivoService
    {
        private readonly IArquivoRepositorio _arquivoRepositorio;
        private readonly ILogClientService _logClientService;
        private readonly IStorageService _storageService;
        private readonly ILogger<ArquivoService> _logger;
        private readonly string _diretorioBase;
        private readonly long _tamanhoMaximoBytes;

        public ArquivoService(
            IArquivoRepositorio arquivoRepositorio,
            ILogClientService logClientService,
            IStorageService storageService,
            ILogger<ArquivoService> logger,
            IConfiguration configuration
        ) : base(arquivoRepositorio)
        {
            _arquivoRepositorio = arquivoRepositorio;
            _logClientService = logClientService;
            _storageService = storageService;
            _logger = logger;

            // Configurações
            _diretorioBase = configuration["Arquivos:DiretorioBase"] ?? "/app/arquivos";
            var tamanhoMaxMB = int.Parse(configuration["Arquivos:TamanhoMaximoMB"] ?? "50");
            _tamanhoMaximoBytes = tamanhoMaxMB * 1024 * 1024;
        }

        /// <summary>
        /// Upload de arquivo para IA (documentos de referência)
        /// </summary>
        public async Task<Arquivo> UploadArquivoParaIAAsync(IFormFile arquivo, string? descricao = null)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    throw new ArgumentException("Arquivo vazio ou nulo");

                // Calcular hash do arquivo
                using var stream = arquivo.OpenReadStream();
                var hash = await CalcularHashSHA256Async(stream);

                // Verificar se já existe
                var arquivoExistente = await BuscarPorHashAsync(hash);
                if (arquivoExistente != null && !arquivoExistente.FlgExcluido)
                {
                    _logger.LogInformation("Arquivo já existe com hash {Hash}", hash);
                    await _logClientService.RegistrarInfo(
                        "ArquivoService",
                        $"Arquivo duplicado encontrado (hash: {hash}). Reutilizando arquivo existente."
                    );
                    return arquivoExistente;
                }

                // Fazer upload para Wasabi
                var container = "documentos-ia"; // Container específico para documentos da IA
                var urlWasabi = await _storageService.UploadDocumentoAsync(arquivo, container);

                // Determinar tipo de arquivo
                var tipoArquivo = DeterminarTipoArquivo(arquivo.ContentType ?? "");

                // Criar registro
                var novoArquivo = new Arquivo
                {
                    ClienteId = string.Empty, // Arquivos da IA não pertencem a um cliente específico
                    NomeOriginal = arquivo.FileName,
                    NomeArmazenado = Path.GetFileName(new Uri(urlWasabi).LocalPath),
                    UrlWasabi = urlWasabi,
                    MimeType = arquivo.ContentType,
                    TamanhoBytes = arquivo.Length,
                    Hash = hash,
                    Tipo = tipoArquivo,
                    FlgProcessado = false,
                    FlgExcluido = false,
                    DtUpload = DateTime.UtcNow,
                    ResultadoProcessamento = descricao // Usar descrição como texto inicial
                };

                await _arquivoRepositorio.AdicionarAsync(novoArquivo);

                _logger.LogInformation("Arquivo {NomeOriginal} enviado com sucesso para IA. ID: {Id}",
                    arquivo.FileName, novoArquivo.Id);

                await _logClientService.RegistrarInfo(
                    "ArquivoService",
                    $"Arquivo salvo para IA com sucesso: {arquivo.FileName} ({arquivo.Length} bytes)"
                );

                return novoArquivo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do arquivo {FileName}", arquivo?.FileName);
                await _logClientService.RegistrarErro(
                    "ArquivoService.UploadArquivoParaIAAsync",
                    $"Erro ao fazer upload do arquivo: {ex.Message}",
                    arquivo?.FileName ?? "",
                    ex.StackTrace ?? ""
                );
                throw;
            }
        }

        public async Task<Arquivo> SalvarArquivoAsync(
            string clienteId,
            string mensagemId,
            string urlDownload,
            string nomeArquivo,
            string mimeType)
        {
            try
            {
                // Download do arquivo
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                using var response = await httpClient.GetAsync(urlDownload);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();

                // Salvar usando stream
                return await SalvarArquivoDeStreamAsync(clienteId, mensagemId, stream, nomeArquivo, mimeType);
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.SalvarArquivoAsync",
                    $"Erro ao baixar arquivo: {ex.Message}",
                    urlDownload,
                    ex.StackTrace ?? ""
                );
                throw;
            }
        }

        public async Task<Arquivo> SalvarArquivoDeStreamAsync(
            string clienteId,
            string mensagemId,
            Stream stream,
            string nomeArquivo,
            string mimeType)
        {
            try
            {
                // Validar tamanho
                if (stream.Length > _tamanhoMaximoBytes)
                {
                    throw new InvalidOperationException($"Arquivo excede tamanho máximo permitido: {stream.Length} bytes");
                }

                // Calcular hash
                string hash;
                using (var sha256 = SHA256.Create())
                {
                    stream.Position = 0;
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                // Verificar se arquivo já existe
                var arquivoExistente = await _arquivoRepositorio.BuscarPorHashAsync(hash);
                if (arquivoExistente != null)
                {
                    await _logClientService.RegistrarInfo(
                        "ArquivoService",
                        $"Arquivo duplicado encontrado (hash: {hash}). Reutilizando arquivo existente."
                    );
                    return arquivoExistente;
                }

                // Criar diretório do cliente
                var diretorioCliente = Path.Combine(_diretorioBase, clienteId);
                Directory.CreateDirectory(diretorioCliente);

                // Nome do arquivo com hash
                var extensao = Path.GetExtension(nomeArquivo);
                var nomeArmazenado = $"{hash}{extensao}";
                var caminhoCompleto = Path.Combine(diretorioCliente, nomeArmazenado);

                // Salvar arquivo no disco
                stream.Position = 0;
                using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // Criar entidade
                var arquivo = new Arquivo
                {
                    ClienteId = clienteId,
                    MensagemId = mensagemId,
                    NomeOriginal = nomeArquivo,
                    NomeArmazenado = nomeArmazenado,
                    CaminhoCompleto = caminhoCompleto,
                    MimeType = mimeType,
                    TamanhoBytes = stream.Length,
                    Hash = hash,
                    Tipo = DeterminarTipoArquivo(mimeType),
                    DtUpload = DateTime.UtcNow,
                    DtExpiracao = DateTime.UtcNow.AddDays(90), // 90 dias padrão
                    FlgProcessado = false,
                    FlgExcluido = false
                };

                // Salvar no banco usando o método da classe base
                await AdicionarAsync(arquivo);

                await _logClientService.RegistrarInfo(
                    "ArquivoService",
                    $"Arquivo salvo com sucesso: {nomeArquivo} ({stream.Length} bytes)"
                );

                return arquivo;
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.SalvarArquivoDeStreamAsync",
                    $"Erro ao salvar arquivo: {ex.Message}",
                    nomeArquivo,
                    ex.StackTrace ?? ""
                );
                throw;
            }
        }

        public async Task<bool> ProcessarArquivoAsync(string arquivoId)
        {
            try
            {
                var arquivo = await _arquivoRepositorio.BuscarPorIdAsync(arquivoId);
                if (arquivo == null)
                {
                    throw new InvalidOperationException($"Arquivo não encontrado: {arquivoId}");
                }

                // TODO: Implementar processamento (OCR, transcrição, etc.)
                arquivo.FlgProcessado = true;
                arquivo.DtProcessamento = DateTime.UtcNow;
                arquivo.ResultadoProcessamento = "Processado com sucesso";

                await _arquivoRepositorio.EditarAsync(arquivo);

                await _logClientService.RegistrarInfo(
                    "ArquivoService",
                    $"Arquivo processado: {arquivo.NomeOriginal}"
                );

                return true;
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.ProcessarArquivoAsync",
                    $"Erro ao processar arquivo: {ex.Message}",
                    arquivoId,
                    ex.StackTrace ?? ""
                );
                return false;
            }
        }

        public async Task<Stream> ObterArquivoAsync(string arquivoId)
        {
            try
            {
                var arquivo = await _arquivoRepositorio.BuscarPorIdAsync(arquivoId);
                if (arquivo == null)
                {
                    throw new InvalidOperationException($"Arquivo não encontrado: {arquivoId}");
                }

                if (arquivo.FlgExcluido)
                {
                    throw new InvalidOperationException($"Arquivo foi excluído: {arquivoId}");
                }

                if (!File.Exists(arquivo.CaminhoCompleto))
                {
                    throw new FileNotFoundException($"Arquivo físico não encontrado: {arquivo.CaminhoCompleto}");
                }

                return File.OpenRead(arquivo.CaminhoCompleto);
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.ObterArquivoAsync",
                    $"Erro ao obter arquivo: {ex.Message}",
                    arquivoId,
                    ex.StackTrace ?? ""
                );
                throw;
            }
        }

        public async Task LimparArquivosExpiradosAsync()
        {
            try
            {
                var arquivosExpirados = await _arquivoRepositorio.BuscarPorFiltroAsync(
                    a => a.DtExpiracao < DateTime.UtcNow && !a.FlgExcluido
                );

                foreach (var arquivo in arquivosExpirados)
                {
                    await ExcluirArquivoAsync(arquivo.Id);
                }

                await _logClientService.RegistrarInfo(
                    "ArquivoService",
                    $"Limpeza de arquivos expirados concluída. Total: {arquivosExpirados.Count()}"
                );
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.LimparArquivosExpiradosAsync",
                    $"Erro ao limpar arquivos expirados: {ex.Message}",
                    "",
                    ex.StackTrace ?? ""
                );
            }
        }

        public async Task<Arquivo?> BuscarPorHashAsync(string hash)
        {
            try
            {
                return await _arquivoRepositorio.BuscarPorHashAsync(hash);
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.BuscarPorHashAsync",
                    $"Erro ao buscar arquivo por hash: {ex.Message}",
                    hash,
                    ex.StackTrace ?? ""
                );
                throw;
            }
        }

        public async Task ExcluirArquivoAsync(string arquivoId)
        {
            try
            {
                var arquivo = await _arquivoRepositorio.BuscarPorIdAsync(arquivoId);
                if (arquivo == null)
                {
                    throw new InvalidOperationException($"Arquivo não encontrado: {arquivoId}");
                }

                // Soft delete
                arquivo.FlgExcluido = true;
                arquivo.DtExclusao = DateTime.UtcNow;

                await _arquivoRepositorio.EditarAsync(arquivo);

                // Opcional: Deletar arquivo físico
                if (File.Exists(arquivo.CaminhoCompleto))
                {
                    File.Delete(arquivo.CaminhoCompleto);
                }

                await _logClientService.RegistrarInfo(
                    "ArquivoService",
                    $"Arquivo excluído: {arquivo.NomeOriginal}"
                );
            }
            catch (Exception ex)
            {
                await _logClientService.RegistrarErro(
                    "ArquivoService.ExcluirArquivoAsync",
                    $"Erro ao excluir arquivo: {ex.Message}",
                    arquivoId,
                    ex.StackTrace ?? ""
                );
                throw;
            }
        }

        #region Métodos Auxiliares

        private async Task<string> CalcularHashSHA256Async(Stream stream)
        {
            using var sha256 = SHA256.Create();
            stream.Position = 0;
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(hashBytes);
        }

        private TipoArquivo DeterminarTipoArquivo(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return TipoArquivo.Outro;

            var mime = mimeType.ToLowerInvariant();

            if (mime.StartsWith("image/"))
                return TipoArquivo.Imagem;

            if (mime.StartsWith("video/"))
                return TipoArquivo.Video;

            if (mime.StartsWith("audio/"))
                return TipoArquivo.Audio;

            if (mime.Contains("pdf"))
                return TipoArquivo.PDF;

            if (mime.Contains("word") || mime.Contains("document"))
                return TipoArquivo.Documento;

            if (mime.Contains("sheet") || mime.Contains("excel") || mime.Contains("csv"))
                return TipoArquivo.Planilha;

            return TipoArquivo.Outro;
        }

        #endregion
    }
}
