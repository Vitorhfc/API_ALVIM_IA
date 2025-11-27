using Shared.Classes.Model;

namespace Admin_Service.Service.Interface
{
    /// <summary>
    /// Interface do serviço de registro completo (Usuário + Empresa + Autenticação)
    /// </summary>
    public interface IRegistroCompletoService
    {
        /// <summary>
        /// Realiza o registro completo:
        /// 1. Cadastra o usuário
        /// 2. Cadastra a empresa
        /// 3. Vincula usuário como administrador da empresa
        /// 4. Autentica o usuário
        /// 5. Retorna o token de autenticação
        /// </summary>
        /// <param name="model">Dados do registro completo</param>
        /// <returns>Response com usuário, empresa e token de autenticação</returns>
        Task<RegistroCompletoResponseModel> RegistrarUsuarioEEmpresaAsync(RegistroCompletoModel model);
    }
}
