using Client_Repository.Configuration.Contexto.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;
using System;

namespace Client_Repository.Configuration.ContextBase
{
    public class ContextBase(
        DbContextOptions<ContextBase> options,
        IOptions<MongoDBSettings>? mongoSettings = null,
        IContextoMultiTenantService contextoMultiTenant = null,
        IHttpContextAccessor httpContextAccessor = null) : DbContext(options)
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IContextoMultiTenantService _contextoMultiTenant = contextoMultiTenant;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly MongoDBSettings _mongoSettings = mongoSettings?.Value;

        //public IMongoCollection<> Collection => _mongoDatabase.GetCollection<>("");
        public IMongoCollection<Cliente> ClienteCollection => _mongoDatabase.GetCollection<Cliente>("Cliente");
        public IMongoCollection<Arquivo> ArquivoCollection => _mongoDatabase.GetCollection<Arquivo>("Arquivo");
        public IMongoCollection<ConfiguracaoIA> ConfiguracaoIACollection => _mongoDatabase.GetCollection<ConfiguracaoIA>("ConfiguracaoIA");
        public IMongoCollection<LogClient> LogClientCollection => _mongoDatabase.GetCollection<LogClient>("LogClient");
        public IMongoCollection<Mensagem> MensagemCollection => _mongoDatabase.GetCollection<Mensagem>("Mensagem");
        public IMongoCollection<ProcessamentoIA> ProcessamentoIACollection => _mongoDatabase.GetCollection<ProcessamentoIA>("ProcessamentoIA");

        public IMongoDatabase Database => _mongoDatabase;

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _mongoDatabase.GetCollection<T>(collectionName);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.Entity<>().ToCollection("");
            builder.Entity<Arquivo>().ToCollection("Arquivo");
            builder.Entity<Cliente>().ToCollection("Cliente");
            builder.Entity<ConfiguracaoIA>().ToCollection("ConfiguracaoIA");
            builder.Entity<LogClient>().ToCollection("LogClient");
            builder.Entity<Mensagem>().ToCollection("Mensagem");
            builder.Entity<ProcessamentoIA>().ToCollection("ProcessamentoIA");

        }
    }
}
