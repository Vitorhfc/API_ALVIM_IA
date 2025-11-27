using Microsoft.AspNetCore.Http;

namespace Shared.Services.Interface
{
    /// <summary>
    /// Interface para serviço de armazenamento de arquivos (Wasabi S3)
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Upload de arquivo genérico
        /// </summary>
        Task<string> UploadArquivoAsync(byte[] fileData, string nomeArquivo, string container);

        /// <summary>
        /// Upload de arquivo a partir de IFormFile
        /// </summary>
        Task<string> UploadArquivoAsync(IFormFile arquivo, string container);

        /// <summary>
        /// Upload de imagem com redimensionamento
        /// </summary>
        Task<string> UploadImagemAsync(byte[] imageData, string imageType, string container);

        /// <summary>
        /// Upload de documento (PDF, DOCX, etc)
        /// </summary>
        Task<string> UploadDocumentoAsync(IFormFile documento, string container);

        /// <summary>
        /// Download de arquivo
        /// </summary>
        Task<byte[]> DownloadArquivoAsync(string url);

        /// <summary>
        /// Deletar arquivo
        /// </summary>
        Task DeletarArquivoAsync(string url, string container);

        /// <summary>
        /// Gerar URL pré-assinada para acesso temporário
        /// </summary>
        string GerarUrlPreAssinada(string url, TimeSpan? expiracao = null);
    }
}
