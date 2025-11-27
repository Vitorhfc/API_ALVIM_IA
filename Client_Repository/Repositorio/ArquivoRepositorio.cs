using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class ArquivoRepositorio : RepositorioGenerico<Arquivo>, IArquivoRepositorio
    {
        private readonly IContextoMultiTenantService _contextoMultiTenant;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ArquivoRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor
        ) : base(contextoMultiTenant, httpContextAccessor, "Arquivo")
        {
            _contextoMultiTenant = contextoMultiTenant;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Arquivo?> BuscarPorHashAsync(string hash)
        {
            return await BuscarPrimeiroPorFiltroAsync(a => a.Hash == hash && !a.FlgExcluido);
        }

        public async Task<IEnumerable<Arquivo>> BuscarExpiradosAsync()
        {
            var dataAtual = DateTime.UtcNow;
            return await BuscarPorFiltroAsync(a =>
                a.DtExpiracao.HasValue &&
                a.DtExpiracao.Value <= dataAtual &&
                !a.FlgExcluido
            );
        }

        public async Task MarcarComoProcessadoAsync(string arquivoId, string resultado)
        {

            var Arquivo = await BuscarPorIdAsync(arquivoId);
            Arquivo.FlgProcessado = true;
            Arquivo.DtProcessamento = DateTime.UtcNow;
            Arquivo.ResultadoProcessamento = resultado;

            await AtualizarAsync(Arquivo);
        }

        public async Task<IEnumerable<Arquivo>> BuscarPorClienteAsync(string clienteId)
        {
            return await BuscarPorFiltroAsync(a => a.ClienteId == clienteId && !a.FlgExcluido);
        }

        public async Task<Arquivo?> BuscarPorMensagemAsync(string mensagemId)
        {
            return await BuscarPrimeiroPorFiltroAsync(a => a.MensagemId == mensagemId && !a.FlgExcluido);
        }
    }
}