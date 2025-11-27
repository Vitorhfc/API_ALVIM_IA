using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class ProcessamentoIARepositorio : RepositorioGenerico<ProcessamentoIA>, IProcessamentoIARepositorio
    {
        public ProcessamentoIARepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor)
            : base(contextoMultiTenant, httpContextAccessor, "ProcessamentoIA")
        {
        }
    }
}