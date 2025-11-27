using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.Client
{
    public class ConfiguracaoIA : BaseEntidade
    {
        [BsonElement("nome")]
        public string Nome { get; set; }

        [BsonElement("funcaoPrincipalDoProduto")]
        public string FuncaoPrincipalDoProduto { get; set; }

        [BsonElement("modulosFuncionalidadesDoProduto")]
        public string ModulosFuncionalidadesDoProduto { get; set; }

        [BsonElement("processoDeUsoProduto")]
        public string ProcessoDeUsoProduto { get; set; }

        [BsonElement("perguntasFrequentesSobreProduto")]
        public string PerguntasFrequentesSobreProduto { get; set; }

        [BsonElement("doresAtendidasPeloProduto")]
        public string DoresAtendidasPeloProduto { get; set; }

        [BsonElement("diferencasVantagensDoProduto")]
        public string DiferencasVantagensDoProduto { get; set; }

        [BsonElement("integracoesRecursosExtrasProduto")]
        public string IntegracoesRecursosExtrasProduto { get; set; }

        [BsonElement("suporteEAtendimentoDoProduto")]
        public string SuporteEAtendimentoDoProduto { get; set; }

        [BsonElement("planosPrecosCondicoesComerciaisDoProduto")]
        public string PlanosPrecosCondicoesComerciaisDoProduto { get; set; }

        [BsonElement("casosDeUsoExemplosPraticosEValoresSistema")]
        public string CasosDeUsoExemplosPraticosEValoresSistema { get; set; }

        [BsonElement("informacoesGerais")]
        public string InformacoesGerais { get; set; }

        [BsonElement("limiteHistoricoMensagens")]
        public int LimiteHistoricoMensagens { get; set; } = 50;

    }

    public enum PreferenciaResposta
    {
        SomenteTexto = 1,
        PreferenciaAudio = 2,
        Misto = 3
    }

    public class ConfiguracaoArquivos
    {
        [BsonElement("tamanhoMaximoMB")]
        public long TamanhoMaximoMB { get; set; } = 50;

        [BsonElement("diasRetencao")]
        public int DiasRetencao { get; set; } = 90;

        [BsonElement("tiposPermitidos")]
        public List<string> TiposPermitidos { get; set; } = new()
            {
                "image/jpeg", "image/png", "image/gif",
                "audio/ogg", "audio/mpeg",
                "video/mp4",
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };

        [BsonElement("flgProcessarAutomaticamente")]
        public bool FlgProcessarAutomaticamente { get; set; } = true;
    }
}
