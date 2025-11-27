using Shared.Classes.Entidades.ADM;
using Admin_Service.ServiceGenerico.Interface;
using Shared.Classes.ModelView.ADM;

namespace Admin_Service.Service.Interface
{
    public interface IUsuarioService : IServiceGenerico<Usuario>
    {
        Task<UsuarioModel> CadastrarUsuarioAsync(UsuarioModel model);
        Task<UsuarioModel> AtualizarUsuarioAsync(string id, UsuarioModel model);
    }
}