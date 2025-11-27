
using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface IArquivoService : IServiceGenerico<Arquivo>
    {
        /// <summary>
        /// Upload de arquivo para IA (documentos de referência)
        /// </summary>
        Task<Arquivo> UploadArquivoParaIAAsync(Microsoft.AspNetCore.Http.IFormFile arquivo, string? descricao = null);

        /// <summary>
        /// ✅ MÉTODO CORRETO (com Async)
        /// Salva arquivo fazendo download do WAHA
        /// </summary>
        Task<Arquivo> SalvarArquivoAsync(
            string clienteId,
            string mensagemId,
            string urlDownload,
            string nomeArquivo,
            string mimeType
        );

        /// <summary>
        /// Salva arquivo de um stream
        /// </summary>
        Task<Arquivo> SalvarArquivoDeStreamAsync(
            string clienteId,
            string mensagemId,
            Stream stream,
            string nomeArquivo,
            string mimeType
        );

        /// <summary>
        /// Processa arquivo (OCR, transcrição, extração de metadados)
        /// </summary>
        Task<bool> ProcessarArquivoAsync(string arquivoId);

        /// <summary>
        /// Obtém stream do arquivo
        /// </summary>
        Task<Stream> ObterArquivoAsync(string arquivoId);

        /// <summary>
        /// Limpa arquivos expirados
        /// </summary>
        Task LimparArquivosExpiradosAsync();

        /// <summary>
        /// Verifica se arquivo existe por hash
        /// </summary>
        Task<Arquivo?> BuscarPorHashAsync(string hash);

        /// <summary>
        /// Marca arquivo como excluído (soft delete)
        /// </summary>
        Task ExcluirArquivoAsync(string arquivoId);
    }
}