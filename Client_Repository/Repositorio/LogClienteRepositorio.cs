using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class LogClientRepositorio : RepositorioGenerico<LogClient>, ILogClientRepositorio
    {
        public LogClientRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor)
            : base(contextoMultiTenant, httpContextAccessor, "LogClient")
        {
        }
    }
}