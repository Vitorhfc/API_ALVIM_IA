using Admin_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio.Interface
{
    public interface IEmpresaRepository : IRepositorioGenerico<Empresa>
    {
        Task<Empresa?> BuscarPorCnpjAsync(string cnpj);
        Task<Empresa?> BuscarPorEmailAsync(string email);
        Task<bool> ExisteCnpjAsync(string cnpj, string? excluirId = null);
        Task<bool> ExisteEmailAsync(string email, string? excluirId = null);

        Task<Empresa?> BuscarPorNumeroWhatsAppAsync(
            string numeroWhatsApp,
            CancellationToken cancellationToken = default);

        Task<Empresa?> BuscarPorSessionNameAsync(
            string sessionName,
            CancellationToken cancellationToken = default);
    }
}
