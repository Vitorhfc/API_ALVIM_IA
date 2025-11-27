using Admin_Repository.Repositorio.Interface;
using Admin_Repository.Configuration;
using Admin_Repository.RepositorioGenerico;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio
{
    public class UsuarioRepository : RepositorioGenerico<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(ContextBaseAdmin contextBaseAdmin)
            : base(contextBaseAdmin, "Usuario")
        {
        }

        public async Task<Usuario> BuscarPorEmailAsync(string email) => await BuscarPrimeiroPorFiltroAsync(u => u.Email == email);
    }
}