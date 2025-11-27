using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Classes.Model
{
    /// <summary>
    /// Request ENVIADO para o N8N processar as mensagens (FORMATO ANTIGO - DEPRECATED)
    /// </summary>
    [Obsolete("Use N8NEnviarMensagemUnicaRequest para o novo fluxo assíncrono")]
    public class N8NEnviarMensagensRequest
    {
        public string IdCliente { get; set; }
        public MensagemN8N UltimaResposta { get; set; }
        public List<MensagemN8N> ListaUltimasMensagens { get; set; }
        public bool FlgPrimeiraMensagemDoDia { get; set; }
        public string TipoMensagem { get; set; }
    }

    /// <summary>
    /// Request para enviar UMA ÚNICA mensagem para o N8N (NOVO FLUXO ASSÍNCRONO)
    /// Cada mensagem recebida é enviada individualmente para o N8N
    /// O N8N fará o agrupamento e processamento, enviando callback depois
    /// </summary>
    public class N8NEnviarMensagemUnicaRequest
    {
        public string idEmpresa { get; set; }
        public string idCliente { get; set; }
        public MensagemN8N mensagem { get; set; }

        /// <summary>
        /// Define se o N8N deve carregar histórico de mensagens
        /// false = apenas processar esta mensagem
        /// true = carregar histórico completo antes de processar
        /// </summary>
        public bool flgCarregarMensagems { get; set; } = false;
    }

    /// <summary>
    /// Request recebido do N8N com a resposta processada pela IA (FORMATO ANTIGO - DEPRECATED)
    /// </summary>
    [Obsolete("Use N8NCallbackRespostaRequest para o novo fluxo assíncrono")]
    public class N8NProcessarMensagensRequest
    {
        public string IdEmpresa { get; set; }
        public string IdCliente { get; set; }
        public MensagemN8N UltimaResposta { get; set; }
        public List<MensagemN8N> ListaUltimasMensagens { get; set; }
        public bool FlgPrimeiraMensagemDoDia { get; set; }
        public string TipoMensagem { get; set; }

        // Resposta gerada pela IA no N8N
        public RespostaIAN8N RespostaIA { get; set; }
    }

    /// <summary>
    /// Request recebido do N8N via callback com a resposta processada (NOVO FLUXO ASSÍNCRONO)
    /// Este é o formato que o N8N enviará após processar e agrupar as mensagens
    /// </summary>
    public class N8NCallbackRespostaRequest
    {
        public string? idEmpresa { get; set; }

        public string idCliente { get; set; }
        public RespostaIAN8N output { get; set; }
    }

    public class MensagemN8N
    {
        public string IdMensagem { get; set; }
        public bool FlgMensagemCliente { get; set; }
        public DateTime DtRecebido { get; set; }
        public string Mensagem { get; set; }
        public string TipoMensagem { get; set; }
    }

    public class TesteRetornoObj
    {
        public RespostaIAN8N output { get; set; }
    }

    /// <summary>
    /// Resposta gerada pela IA no N8N - NOVO FORMATO
    /// </summary>
    public class RespostaIAN8N
    {
        public List<MensagemRespostaN8N> mensagens { get; set; } = new();
        public List<ReacaoN8N>? reacoes { get; set; }
        public List<string>? documentosEnviados { get; set; }
        public string? tipoEsclarecimento { get; set; } = "";
    }

    /// <summary>
    /// Mensagem individual retornada pelo N8N
    /// </summary>
    public class MensagemRespostaN8N
    {
        public string tipo { get; set; }
        public string conteudo { get; set; }
        public string? idDocumento { get; set; }
    }

    /// <summary>
    /// Reação a ser enviada para uma mensagem do cliente
    /// </summary>
    public class ReacaoN8N
    {
        public string idMensagem { get; set; }

        public string emoji { get; set; }
    }

    public class PlanoClienteResponse
    {
        public string PlanoDeAudio { get; set; } = "";
        public string PlanoDeDocumentos { get; set; } = "";
        public string PlanoAgendamentoDeAtendimentos { get; set; } = "";
    }

    public class ContextoProjetoResponse
    {
        public string ContextoProjeto { get; set; }
    }

    public class DocumentoResponse
    {
        public string Id { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public string Url { get; set; }
        public long TamanhoBytes { get; set; }
        public string? MimeType { get; set; }
        public DateTime DtUpload { get; set; }
        public bool FlgProcessado { get; set; }
        public string? ResultadoProcessamento { get; set; }
    }

    public class DocumentosResponse
    {
        public List<DocumentoResponse> Documentos { get; set; }
    }

    /// <summary>
    /// Converter customizado para deserializar RespostaIAN8N que pode vir como string JSON ou objeto
    /// Lida com serialização dupla do N8N
    /// </summary>
    public class RespostaIAN8NConverter : JsonConverter<RespostaIAN8N>
    {
        public override RespostaIAN8N Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Se for uma string, pode ser serialização simples ou dupla
            if (reader.TokenType == JsonTokenType.String)
            {
                var jsonString = reader.GetString();
                if (string.IsNullOrEmpty(jsonString))
                    return null;

                // Tentar deserializar primeiro (pode ter sido serializado 2x)
                try
                {
                    // Primeiro, verificar se a string começa com aspas (serialização dupla)
                    if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                    {
                        // Deserializar uma vez para remover a primeira camada de escape
                        jsonString = JsonSerializer.Deserialize<string>(jsonString);
                    }

                    // Agora deserializar o JSON real
                    return JsonSerializer.Deserialize<RespostaIAN8N>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception)
                {
                    // Se falhar, tentar deserializar direto
                    return JsonSerializer.Deserialize<RespostaIAN8N>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }

            // Se já for um objeto, deserializar normalmente
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return JsonSerializer.Deserialize<RespostaIAN8N>(ref reader, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, RespostaIAN8N value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
