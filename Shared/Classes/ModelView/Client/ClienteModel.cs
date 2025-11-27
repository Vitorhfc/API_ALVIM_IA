using Shared.Classes.ModelView.Base;

namespace Shared.Classes.ModelView.Client
{
    public class ClienteModel : BaseModel
    {
        public string nome { get; set; } = string.Empty;
        public string numero { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string cpf { get; set; } = string.Empty;
        public DateTime dtPrimeiroContato { get; set; }

    }
}
