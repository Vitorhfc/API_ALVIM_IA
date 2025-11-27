using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Admin_Repository.Configuration
{
    public class ContextBaseAdmin : DbContext
    {
        private readonly IMongoDatabase _mongoDatabase;

        static ContextBaseAdmin()
        {
            try
            {
                MongoDBClassMapConfiguration.Configure();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao inicializar ContextBaseAdmin: {ex.Message}", ex);
            }
        }

        public ContextBaseAdmin(DbContextOptions<ContextBaseAdmin> options, IOptions<MongoDBSettings> mongoSettings)
            : base(options)
        {
            var settings = mongoSettings.Value ?? throw new ArgumentNullException(nameof(mongoSettings));
            var mongoClient = new MongoClient(settings.ConnectionString);
            _mongoDatabase = mongoClient.GetDatabase(settings.DatabaseNameAdmin)
                ?? throw new InvalidOperationException("Falha ao conectar ao banco MongoDB");
        }

        public IMongoCollection<Empresa> EmpresaCollection =>
            _mongoDatabase.GetCollection<Empresa>("Empresa");

        public IMongoCollection<Usuario> UsuarioCollection =>
            _mongoDatabase.GetCollection<Usuario>("Usuario");

        public IMongoCollection<UsuarioEmpresa> UsuarioEmpresaCollection =>
            _mongoDatabase.GetCollection<UsuarioEmpresa>("UsuarioEmpresa");

        public IMongoCollection<LogADM> LogADMCollection =>
            _mongoDatabase.GetCollection<LogADM>("LogADM");

        public IMongoCollection<LogWaha> LogWahaCollection =>
            _mongoDatabase.GetCollection<LogWaha>("LogWaha");

        public IMongoDatabase Database => _mongoDatabase;

        public IMongoCollection<T> GetCollection<T>(string collectionName) =>
            _mongoDatabase.GetCollection<T>(collectionName);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Empresa>().ToCollection("Empresa");
            builder.Entity<Usuario>().ToCollection("Usuario");
            builder.Entity<UsuarioEmpresa>().ToCollection("UsuarioEmpresa");
            builder.Entity<LogADM>().ToCollection("LogADM");
            builder.Entity<LogWaha>().ToCollection("LogWaha");
        }
    }
}