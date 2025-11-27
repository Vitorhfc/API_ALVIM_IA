namespace Shared.Classes.ModelView.Base
{
    public class BaseModel
    {
        public string? Id { get; set; }
        public DateTime? DtaCadastro { get; set; } = DateTime.Now;
        public DateTime? DtaAlteracao { get; set; } = null;
        public bool FlgAtivo { get; set; } = true;
    }
}
