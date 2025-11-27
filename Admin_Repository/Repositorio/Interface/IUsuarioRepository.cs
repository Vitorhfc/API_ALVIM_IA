using Shared.Classes.Entidades.ADM;
using Admin_Repository.RepositorioGenerico.Interface;

namespace Admin_Repository.Repositorio.Interface
{
    public interface IUsuarioRepository : IRepositorioGenerico<Usuario>
    {
        Task<Usuario> BuscarPorEmailAsync(string email);
    }
}