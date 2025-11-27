using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.Client;

namespace Client.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de arquivos da IA
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArquivoController : ControllerBaseClient<ArquivoController>
    {
        #region Campos

        private readonly IArquivoService _arquivoService;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<ArquivoController> _logger;

        #endregion

        #region Construtor

        public ArquivoController(
            IArquivoService arquivoService,
            ILogClientService logClientService,
            ILogger<ArquivoController> logger)
            : base(logClientService, logger)
        {
            _arquivoService = arquivoService;
            _logClientService = logClientService;
            _logger = logger;
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Upload de arquivo para IA
        /// </summary>
        /// <returns>Dados do arquivo cadastrado</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadArquivo(IFormFile arquivo, string? descricao = null)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    return Erro("Arquivo é obrigatório");

                // Validar tipo de arquivo
                var extensoesPermitidas = new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".txt", ".csv" };
                var extensao = Path.GetExtension(arquivo.FileName).ToLower();

                if (!extensoesPermitidas.Contains(extensao))
                {
                    return Erro($"Tipo de arquivo '{extensao}' não permitido. Permitidos: {string.Join(", ", extensoesPermitidas)}");
                }

                // Limitar tamanho (50MB)
                if (arquivo.Length > 50 * 1024 * 1024)
                {
                    return Erro("Arquivo muito grande. Tamanho máximo: 50MB");
                }

                var arquivoCadastrado = await _arquivoService.UploadArquivoParaIAAsync(arquivo, descricao);

                await LogInfoAsync(
                    $"Upload de arquivo realizado: {arquivo.FileName} (ID: {arquivoCadastrado.Id})",
                    nameof(UploadArquivo)
                );

                return Sucesso(new
                {
                    arquivoCadastrado.Id,
                    arquivoCadastrado.NomeOriginal,
                    arquivoCadastrado.UrlWasabi,
                    arquivoCadastrado.TamanhoBytes,
                    arquivoCadastrado.Tipo,
                    arquivoCadastrado.DtUpload,
                    arquivoCadastrado.ResultadoProcessamento
                }, "Arquivo enviado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(UploadArquivo));
            }
        }

        /// <summary>
        /// Listar todos os arquivos cadastrados para a IA
        /// </summary>
        /// <returns>Lista de arquivos</returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ListarArquivos()
        {
            try
            {
                // Buscar arquivos da IA (ClienteId vazio)
                var arquivos = await _arquivoService.BuscarPorFiltroAsync(a =>
                    a.ClienteId == string.Empty &&
                    !a.FlgExcluido);

                var arquivosFormatados = arquivos.Select(a => new
                {
                    a.Id,
                    a.NomeOriginal,
                    a.UrlWasabi,
                    a.TamanhoBytes,
                    a.Tipo,
                    a.MimeType,
                    a.DtUpload,
                    a.FlgProcessado,
                    a.ResultadoProcessamento
                });

                await LogInfoAsync($"Listou {arquivos.Count()} arquivos da IA", nameof(ListarArquivos));

                return Sucesso(arquivosFormatados, "Arquivos listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarArquivos));
            }
        }

        /// <summary>
        /// Obter informações de um arquivo específico
        /// </summary>
        /// <param name="id">ID do arquivo</param>
        /// <returns>Dados do arquivo</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObterArquivo(string id)
        {
            try
            {
                var arquivo = await _arquivoService.BuscarPorIdAsync(id);

                if (arquivo == null || arquivo.FlgExcluido)
                    return NaoEncontrado("Arquivo não encontrado");

                await LogInfoAsync($"Consultou arquivo {id}", nameof(ObterArquivo));

                return Sucesso(new
                {
                    arquivo.Id,
                    arquivo.NomeOriginal,
                    arquivo.UrlWasabi,
                    arquivo.TamanhoBytes,
                    arquivo.Tipo,
                    arquivo.MimeType,
                    arquivo.DtUpload,
                    arquivo.FlgProcessado,
                    arquivo.DtProcessamento,
                    arquivo.ResultadoProcessamento,
                    arquivo.Metadata
                }, "Arquivo encontrado");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterArquivo));
            }
        }

        /// <summary>
        /// Excluir arquivo
        /// </summary>
        /// <param name="id">ID do arquivo</param>
        /// <returns>Confirmação de exclusão</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExcluirArquivo(string id)
        {
            try
            {
                var arquivo = await _arquivoService.BuscarPorIdAsync(id);

                if (arquivo == null || arquivo.FlgExcluido)
                    return NaoEncontrado("Arquivo não encontrado");

                await _arquivoService.ExcluirArquivoAsync(id);

                await LogInfoAsync($"Excluiu arquivo {id} ({arquivo.NomeOriginal})", nameof(ExcluirArquivo));

                return Sucesso(null, "Arquivo excluído com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ExcluirArquivo));
            }
        }

        /// <summary>
        /// Processar arquivo (OCR, transcrição, etc)
        /// </summary>
        /// <param name="id">ID do arquivo</param>
        /// <returns>Resultado do processamento</returns>
        [HttpPost("{id}/processar")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessarArquivo(string id)
        {
            try
            {
                var arquivo = await _arquivoService.BuscarPorIdAsync(id);

                if (arquivo == null || arquivo.FlgExcluido)
                    return NaoEncontrado("Arquivo não encontrado");

                var sucesso = await _arquivoService.ProcessarArquivoAsync(id);

                if (!sucesso)
                    return Erro("Falha ao processar arquivo");

                await LogInfoAsync($"Processou arquivo {id}", nameof(ProcessarArquivo));

                return Sucesso(null, "Arquivo processado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ProcessarArquivo));
            }
        }

        #endregion
    }
}
