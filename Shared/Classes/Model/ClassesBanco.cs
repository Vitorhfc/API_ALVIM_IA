using MongoDB.Driver;

namespace Shared.Classes.Model
{
    public class TenantInfo
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string EmpresaId { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string NomeBaseDados { get; set; } = string.Empty;
        public IMongoClient MongoClient { get; set; } = null!;
        public DateTime UltimoAcesso { get; set; } = DateTime.UtcNow;
    }

    public class MongoDBSettings
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseNameClient { get; set; }
        public string? DatabaseNameAdmin { get; set; }

        public void ValidateAdmin()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("ConnectionString é obrigatório");
            if (string.IsNullOrEmpty(DatabaseNameAdmin))
                throw new InvalidOperationException("DatabaseNameAdmin é obrigatório para contexto Admin");
        }

        public void ValidateClient()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("ConnectionString é obrigatório");
            if (string.IsNullOrEmpty(DatabaseNameClient))
                throw new InvalidOperationException("DatabaseNameClient é obrigatório para contexto Client");
        }
    }
}
