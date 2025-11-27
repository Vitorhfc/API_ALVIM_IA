
using Shared.Classes.Model;
using Shared.Classes.ModelView;

namespace Admin_Service.Service.Interface
{
    public interface IAutenticacaoService
    {
        Task<LoginResponseModel> AutenticarUsuarioAsync(LoginModel model);
        Task<ValidacaoDuasEtapasResponseModel> SolicitarValidacaoDuasEtapasAsync(SolicitarValidacaoDuasEtapasModel model);
        Task<ValidarToken2FAResponseModel> ValidarToken2FAAsync(ValidarToken2FAModel model);
        Task<AutenticacaoCompletaResponseModel> SelecionarEmpresaAsync(SelecionarEmpresaModel model);
        Task<AutenticacaoCompletaResponseModel> LoginUsuarioAsync(LoginUsuarioModel model);
        Task<AutenticacaoCompletaResponseModel> LoginEmpresaAsync(LoginEmpresaModel model);
    }
}