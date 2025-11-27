using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Classes.Model
{
    /// <summary>
    /// Informações sobre mídia da mensagem
    /// </summary>
    public class MidiaInfo
    {
        /// <summary>
        /// ID do arquivo na coleção Arquivo
        /// </summary>
        public string? ArquivoId { get; set; }

        /// <summary>
        /// URL de download da mídia no WAHA
        /// </summary>
        public string? UrlDownload { get; set; }

        /// <summary>
        /// Caminho local do arquivo salvo
        /// </summary>
        public string? UrlLocal { get; set; }

        /// <summary>
        /// Nome do arquivo
        /// </summary>
        public string? NomeArquivo { get; set; }

        /// <summary>
        /// Tipo MIME (ex: image/jpeg, audio/ogg)
        /// </summary>
        public string? MimeType { get; set; }

        /// <summary>
        /// Tamanho em bytes
        /// </summary>
        public long? TamanhoBytes { get; set; }

        /// <summary>
        /// Duração em segundos (para áudios e vídeos)
        /// </summary>
        public int? DuracaoSegundos { get; set; }

        /// <summary>
        /// Largura (para imagens e vídeos)
        /// </summary>
        public int? Largura { get; set; }

        /// <summary>
        /// Altura (para imagens e vídeos)
        /// </summary>
        public int? Altura { get; set; }

        /// <summary>
        /// Legenda/caption da mídia
        /// </summary>
        public string? Caption { get; set; }

        /// <summary>
        /// Indica se a mídia foi baixada
        /// </summary>
        public bool FlgBaixada { get; set; }

        /// <summary>
        /// Data do download
        /// </summary>
        public DateTime? DtDownload { get; set; }
    }
}
