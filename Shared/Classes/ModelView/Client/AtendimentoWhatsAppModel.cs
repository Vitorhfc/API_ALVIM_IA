namespace Shared.Classes.ModelView.Client
{
    /// <summary>
    /// Request para enviar mensagem de texto
    /// </summary>
    public class EnviarMensagemTextoRequest
    {
        public string ClienteId { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string? QuotedMessageId { get; set; }
    }

    /// <summary>
    /// Request para enviar mensagem com mídia
    /// </summary>
    public class EnviarMensagemMidiaRequest
    {
        public string ClienteId { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string UrlMidia { get; set; } = string.Empty;
        public string? NomeArquivo { get; set; }
        public TipoMidiaWhatsApp TipoMidia { get; set; }
        public string? QuotedMessageId { get; set; }
    }

    /// <summary>
    /// Request para enviar áudio/voice note
    /// </summary>
    public class EnviarAudioRequest
    {
        public string ClienteId { get; set; } = string.Empty;
        public string UrlAudio { get; set; } = string.Empty;
        public string? QuotedMessageId { get; set; }
    }

    /// <summary>
    /// Request para reagir a uma mensagem
    /// </summary>
    public class ReagirMensagemRequest
    {
        public string MensagemId { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para remover mensagem
    /// </summary>
    public class RemoverMensagemRequest
    {
        public string MensagemId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para editar mensagem
    /// </summary>
    public class EditarMensagemRequest
    {
        public string MensagemId { get; set; } = string.Empty;
        public string NovoTexto { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para alternar modo de resposta (IA ou atendente)
    /// </summary>
    public class AlternarModoRespostaRequest
    {
        public string ClienteId { get; set; } = string.Empty;
        public bool AtendimentoHumano { get; set; }
    }

    /// <summary>
    /// Tipo de mídia para WhatsApp
    /// </summary>
    public enum TipoMidiaWhatsApp
    {
        Imagem = 1,
        Video = 2,
        Audio = 3,
        Documento = 4
    }

    /// <summary>
    /// Response padrão para operações de atendimento
    /// </summary>
    public class AtendimentoWhatsAppResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string? IdMensagem { get; set; }
        public string? Erro { get; set; }
        public object? Dados { get; set; }
    }

    /// <summary>
    /// Response para status do modo de resposta
    /// </summary>
    public class StatusModoRespostaResponse
    {
        public string ClienteId { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public bool AtendimentoHumano { get; set; }
        public DateTime? DataAtivacao { get; set; }
        public DateTime? DataDesativacao { get; set; }
    }
}
