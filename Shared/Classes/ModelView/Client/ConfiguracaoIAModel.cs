using static Shared.Enumeradores.Enumeradores;
using Shared.Classes.ModelView.Base;

namespace Shared.Classes.ModelView.Client
{
    public class ConfiguracaoIAModel : BaseModel
    {
        public string NomeIa { get; set; } = "Alvim";

        public TomVoz TomVoz { get; set; } = TomVoz.Masculino;

        public string Pergunta_FuncaoPrincipalSistema { get; set; } = "";
        public string Pergunta_ModulosFinanceiro { get; set; } = "";
        public string Pergunta_ProcessoUso { get; set; } = "";
        public string Pergunta_PerguntasFrequentes { get; set; } = "";
        public string Pergunta_PrincipaisDores { get; set; } = "";
        public string Pergunta_DiferenciaisVatagens { get; set; } = "";
        public string Pergunta_IntegracoesRecursos { get; set; } = "";
        public string Pergunta_SuporteAtendimento { get; set; } = "";
        public string Pergunta_PlanosPrecosCondicoes { get; set; } = "";
        public string Pergunta_CasosDeUso { get; set; } = "";

        public string InformacoesGerais { get; set; } = "";

        public int Temperatura_Criativade { get; set; }

    }
}
