using Shared.Classes.ModelView.Base;

namespace Shared.Classes.ModelView.ADM
{
    public class UsuarioModel : BaseModel
    {
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string Cpf { get; set; } = "";
        public string Celular { get; set; } = "";
        public string Senha { get; set; } = "";
        public DateTime DtaNascimento { get; set; }
        public DateTime? DtaUltimoAcesso { get; set; } = null;
        public bool FlgInterno { get; set; } = false;
    }
}