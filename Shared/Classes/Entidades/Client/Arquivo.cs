using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.Client
{
    public class Arquivo : BaseEntidade
    {
        [BsonElement("clienteId")]
        public string ClienteId { get; set; } = string.Empty;

        /// <summary>
        /// ID da mensagem relacionada
        /// </summary>
        [BsonElement("mensagemId")]
        public string? MensagemId { get; set; }

        /// <summary>
        /// Nome original do arquivo
        /// </summary>
        [BsonElement("nomeOriginal")]
        public string NomeOriginal { get; set; } = string.Empty;

        /// <summary>
        /// Nome armazenado (com hash para evitar duplicatas)
        /// </summary>
        [BsonElement("nomeArmazenado")]
        public string NomeArmazenado { get; set; } = string.Empty;

        /// <summary>
        /// Caminho completo no disco (DEPRECATED - usar UrlWasabi)
        /// </summary>
        [BsonElement("caminhoCompleto")]
        public string? CaminhoCompleto { get; set; }

        /// <summary>
        /// URL do arquivo no Wasabi S3
        /// </summary>
        [BsonElement("urlWasabi")]
        public string UrlWasabi { get; set; } = string.Empty;

        /// <summary>
        /// Tipo MIME
        /// </summary>
        [BsonElement("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Tamanho em bytes
        /// </summary>
        [BsonElement("tamanhoBytes")]
        public long TamanhoBytes { get; set; }

        /// <summary>
        /// Hash SHA256 do arquivo (para detectar duplicatas)
        /// </summary>
        [BsonElement("hash")]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Tipo do arquivo
        /// </summary>
        [BsonElement("tipo")]
        [BsonRepresentation(BsonType.String)]
        public TipoArquivo Tipo { get; set; }

        /// <summary>
        /// Metadados extraídos do arquivo
        /// </summary>
        [BsonElement("metadata")]
        public ArquivoMetadata? Metadata { get; set; }

        /// <summary>
        /// Indica se o arquivo foi processado (OCR, transcrição, etc)
        /// </summary>
        [BsonElement("flgProcessado")]
        public bool FlgProcessado { get; set; }

        /// <summary>
        /// Data do processamento
        /// </summary>
        [BsonElement("dtProcessamento")]
        public DateTime? DtProcessamento { get; set; }

        /// <summary>
        /// Resultado do processamento (texto extraído, etc)
        /// </summary>
        [BsonElement("resultadoProcessamento")]
        public string? ResultadoProcessamento { get; set; }

        /// <summary>
        /// Indica se o arquivo foi excluído (soft delete)
        /// </summary>
        [BsonElement("flgExcluido")]
        public bool FlgExcluido { get; set; }

        /// <summary>
        /// Data da exclusão
        /// </summary>
        [BsonElement("dtExclusao")]
        public DateTime? DtExclusao { get; set; }

        /// <summary>
        /// Data do upload
        /// </summary>
        [BsonElement("dtUpload")]
        public DateTime DtUpload { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data de expiração (para limpeza automática)
        /// </summary>
        [BsonElement("dtExpiracao")]
        public DateTime? DtExpiracao { get; set; }
    }

    /// <summary>
    /// Metadados extraídos do arquivo
    /// </summary>
    public class ArquivoMetadata
    {
        [BsonElement("duracaoSegundos")]
        public int? DuracaoSegundos { get; set; }

        [BsonElement("largura")]
        public int? Largura { get; set; }

        [BsonElement("altura")]
        public int? Altura { get; set; }

        [BsonElement("numeroPaginas")]
        public int? NumeroPaginas { get; set; }

        [BsonElement("textoExtraido")]
        public string? TextoExtraido { get; set; }

        [BsonElement("outrosMetadados")]
        public Dictionary<string, string>? OutrosMetadados { get; set; }
    }

    /// <summary>
    /// Tipos de arquivo
    /// </summary>
    public enum TipoArquivo
    {
        Audio = 1,
        Imagem = 2,
        Video = 3,
        Documento = 4,
        Planilha = 5,
        PDF = 6,
        Outro = 7
    }
}
