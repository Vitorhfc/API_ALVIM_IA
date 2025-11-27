using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class FuncionarioRepositorio : RepositorioGenerico<Funcionario>, IFuncionarioRepositorio
    {
        public FuncionarioRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor)
            : base(contextoMultiTenant, httpContextAccessor, "Funcionario")
        {
        }

        public async Task<Funcionario?> BuscarPorUsuarioIdAsync(string usuarioId)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(f => f.UsuarioId == usuarioId).FirstOrDefaultAsync();
        }

        public async Task AtualizarHorariosAtendimentoAsync(string funcionarioId, List<HorarioAtendimento> horarios)
        {
            var collection = await ObterColecaoAsync();
            var update = Builders<Funcionario>.Update
                .Set(f => f.HorariosAtendimento, horarios)
                .Set(f => f.DtaAlteracao, DateTime.UtcNow);

            await collection.UpdateOneAsync(f => f.Id == funcionarioId, update);
        }

        public async Task<IEnumerable<Funcionario>> BuscarFuncionariosAtivosAsync()
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(f => f.FlgAtivo == true).ToListAsync();
        }
    }
}
