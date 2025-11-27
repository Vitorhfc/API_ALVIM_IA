using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Entidades.Base;

namespace Admin_Repository.Configuration
{
    /// <summary>
    /// Configuração de mapeamento MongoDB exclusiva para entidades Admin
    /// </summary>
    public class MongoDBClassMapConfiguration
    {
        private static bool _isConfigured = false;
        private static readonly object _lock = new object();

        public static void Configure()
        {
            if (_isConfigured) return;

            lock (_lock)
            {
                if (_isConfigured) return;

                try
                {
                    ConfigureConventions();
                    ConfigureBaseEntity();
                    ConfigureAdminEntities();
                    _isConfigured = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao configurar MongoDB ClassMap (Admin): {ex.Message}");
                    throw;
                }
            }
        }

        private static void ConfigureConventions()
        {
            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new CamelCaseElementNameConvention()
            };

            ConventionRegistry.Register("AdminConventions", conventionPack,
                type => type.Namespace?.StartsWith("Shared.Classes.Entidades.ADM") == true ||
                        type == typeof(BaseEntidade));
        }

        private static void ConfigureBaseEntity()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntidade)))
            {
                BsonClassMap.RegisterClassMap<BaseEntidade>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(b => b.Id)
                        .SetIdGenerator(StringObjectIdGenerator.Instance)
                        .SetSerializer(new MongoDB.Bson.Serialization.Serializers.StringSerializer(BsonType.ObjectId));
                    cm.SetIgnoreExtraElements(true);
                    cm.SetIsRootClass(true);
                });
            }
        }

        private static void ConfigureAdminEntities()
        {
            // Empresa
            if (!BsonClassMap.IsClassMapRegistered(typeof(Empresa)))
            {
                BsonClassMap.RegisterClassMap<Empresa>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // Usuario
            if (!BsonClassMap.IsClassMapRegistered(typeof(Usuario)))
            {
                BsonClassMap.RegisterClassMap<Usuario>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // UsuarioEmpresa
            if (!BsonClassMap.IsClassMapRegistered(typeof(UsuarioEmpresa)))
            {
                BsonClassMap.RegisterClassMap<UsuarioEmpresa>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // LogADM
            if (!BsonClassMap.IsClassMapRegistered(typeof(LogADM)))
            {
                BsonClassMap.RegisterClassMap<LogADM>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }
    }
}