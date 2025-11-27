using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.Client
{
    /// <summary>
    /// Representa um plano de contexto do projeto para a IA
    /// </summary>
    public class PlanoContexto : BaseEntidade
    {
        [BsonElement("nome")]
        public string Nome { get; set; } = string.Empty;

        [BsonElement("descricao")]
        public string? Descricao { get; set; }

        [BsonElement("tipo")]
        public TipoPlanoContexto Tipo { get; set; }

        [BsonElement("flgAtivo")]
        public bool FlgAtivo { get; set; }

        [BsonElement("conteudo")]
        public string Conteudo { get; set; } = string.Empty;

        [BsonElement("ordem")]
        public int Ordem { get; set; }

        [BsonElement("flgPadrao")]
        public bool FlgPadrao { get; set; }

        [BsonElement("dtCriacao")]
        public DateTime DtCriacao { get; set; } = DateTime.UtcNow;

        [BsonElement("dtAlteracao")]
        public DateTime DtAlteracao { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Tipos de planos de contexto
    /// Cada tipo mapeia para um campo específico da ConfiguracaoIA
    /// </summary>
    public enum TipoPlanoContexto
    {
        /// <summary>
        /// Plano base - SEMPRE ATIVO
        /// Mapeia para: InformacoesGerais
        /// </summary>
        PlanoBase = 1,

        /// <summary>
        /// Função principal do produto
        /// Mapeia para: FuncaoPrincipalDoProduto
        /// </summary>
        FuncaoPrincipal = 2,

        /// <summary>
        /// Módulos e funcionalidades
        /// Mapeia para: ModulosFuncionalidadesDoProduto
        /// </summary>
        ModulosFuncionalidades = 3,

        /// <summary>
        /// Processo de uso
        /// Mapeia para: ProcessoDeUsoProduto
        /// </summary>
        ProcessoDeUso = 4,

        /// <summary>
        /// Perguntas frequentes
        /// Mapeia para: PerguntasFrequentesSobreProduto
        /// </summary>
        FAQ = 5,

        /// <summary>
        /// Dores atendidas
        /// Mapeia para: DoresAtendidasPeloProduto
        /// </summary>
        DoresAtendidas = 6,

        /// <summary>
        /// Diferenciais e vantagens
        /// Mapeia para: DiferencasVantagensDoProduto
        /// </summary>
        DiferenciaisVantagens = 7,

        /// <summary>
        /// Integrações e recursos extras
        /// Mapeia para: IntegracoesRecursosExtrasProduto
        /// </summary>
        IntegracoesRecursos = 8,

        /// <summary>
        /// Suporte e atendimento
        /// Mapeia para: SuporteEAtendimentoDoProduto
        /// </summary>
        SuporteAtendimento = 9,

        /// <summary>
        /// Planos, preços e condições comerciais
        /// Mapeia para: PlanosPrecosCondicoesComerciaisDoProduto
        /// </summary>
        PlanosPrecos = 10,

        /// <summary>
        /// Casos de uso e exemplos práticos
        /// Mapeia para: CasosDeUsoExemplosPraticosEValoresSistema
        /// </summary>
        CasosDeUso = 11,

        /// <summary>
        /// Instruções para documentos
        /// Mapeia para: InstrucoesDocumentos
        /// </summary>
        InstrucoesDocumentos = 12,

        /// <summary>
        /// Instruções para agendamento
        /// Mapeia para: InstrucoesAgendamento
        /// </summary>
        InstrucoesAgendamento = 13,

        /// <summary>
        /// Instruções para arquivos
        /// Mapeia para: InstrucoesArquivos
        /// </summary>
        InstrucoesArquivos = 14
    }
}
