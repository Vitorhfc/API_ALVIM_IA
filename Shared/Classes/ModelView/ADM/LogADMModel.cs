using Shared.Classes.ModelView.Base;

namespace Shared.Classes.ModelView.ADM
{
    public class LogADMModel : BaseModel
    {
        public string UsuarioId { get; set; } = "";
        public string EmpresaId { get; set; } = "";
        public string Acao { get; set; } = "";
        public bool Sucesso { get; set; } = true;
        public string Descricao { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public DateTime DtaCadastro { get; set; } = DateTime.Now;
    }
}
