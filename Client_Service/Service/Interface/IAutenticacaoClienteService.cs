using Shared.Classes.Model;

namespace Client_Service.Service.Interface
{
    public interface IAutenticacaoClientService
    {
        Task<LoginResponseModel> AutenticarUsuarioAsync(LoginModel model);
        Task<ValidacaoDuasEtapasResponseModel> SolicitarValidacaoDuasEtapasAsync(SolicitarValidacaoDuasEtapasModel model);
        Task<ValidarToken2FAResponseModel> ValidarToken2FAAsync(ValidarToken2FAModel model);
        Task<AutenticacaoClientCompletaResponseModel> SelecionarEmpresaAsync(SelecionarEmpresaModel model);
        Task<AutenticacaoClientCompletaResponseModel> LoginEmpresaAsync(LoginEmpresaModel model);
        Task<RefreshTokenResponseModel> RefreshTokenAsync(string refreshToken);
        Task InvalidarRefreshTokensAsync(string usuarioId);
    }
}