using Shared.Classes.ModelView.Base;

namespace Shared.Classes.ModelView.ADM
{
    public class EmpresaModel : BaseModel
    {
        public string Cnpj { get; set; } = "";
        public string NomeEmpresa { get; set; } = "";
        public string NomeFantasia { get; set; } = "";
        public string Email { get; set; } = "";


        #region BaseDeDados
        public string ConnectionString { get; set; } = "";
        public string NomeBaseDados { get; set; } = "";
        #endregion
    }
}
