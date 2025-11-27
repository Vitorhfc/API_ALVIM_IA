using Admin_Repository.Configuration;
using Admin_Repository.Repositorio.Interface;
using Admin_Repository.RepositorioGenerico;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;

namespace Admin_Repository.Repositorio
{
    public class RefreshTokenRepository : RepositorioGenerico<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(ContextBaseAdmin contextBaseAdmin)
    : base(contextBaseAdmin, "RefreshToken")
        {
        }

        public async Task<RefreshToken?> BuscarPorTokenAsync(string token)
        {
            var filter = Builders<RefreshToken>.Filter.Eq(x => x.Token, token);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<RefreshToken>> BuscarPorUsuarioIdAsync(string usuarioId)
        {
            var filter = Builders<RefreshToken>.Filter.Eq(x => x.UsuarioId, usuarioId);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<RefreshToken>> BuscarPorUsuarioEmpresaAsync(string usuarioId, string empresaId)
        {
            var filter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(x => x.UsuarioId, usuarioId),
                Builders<RefreshToken>.Filter.Eq(x => x.EmpresaId, empresaId)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task RevogarTokensUsuarioAsync(string usuarioId)
        {
            var filter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(x => x.UsuarioId, usuarioId),
                Builders<RefreshToken>.Filter.Eq(x => x.FlgRevogado, false),
                Builders<RefreshToken>.Filter.Eq(x => x.FlgUtilizado, false)
            );

            var update = Builders<RefreshToken>.Update.Set(x => x.FlgRevogado, true);

            await _collection.UpdateManyAsync(filter, update);
        }

        public async Task RevogarTokensUsuarioEmpresaAsync(string usuarioId, string empresaId)
        {
            var filter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(x => x.UsuarioId, usuarioId),
                Builders<RefreshToken>.Filter.Eq(x => x.EmpresaId, empresaId),
                Builders<RefreshToken>.Filter.Eq(x => x.FlgRevogado, false),
                Builders<RefreshToken>.Filter.Eq(x => x.FlgUtilizado, false)
            );

            var update = Builders<RefreshToken>.Update.Set(x => x.FlgRevogado, true);

            await _collection.UpdateManyAsync(filter, update);
        }

        public async Task LimparTokensExpiradosAsync()
        {
            var filter = Builders<RefreshToken>.Filter.Lt(x => x.DtaExpiracao, DateTime.Now);
            await _collection.DeleteManyAsync(filter);
        }
    }
}