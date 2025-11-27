using Admin_Repository.RepositorioGenerico;
using Admin_Repository.RepositorioGenerico.Interface;
using Microsoft.Extensions.DependencyInjection;
using Shared.Utils.LoaderAssembler;

namespace Admin_Repository.Injection
{
    public static class Injection
    {
        public static IServiceCollection AddInjectionRepositoryAdmin(this IServiceCollection services)
        {
            try
            {
                // Registrar repositório genérico
                services.AddScoped(typeof(IRepositorioGenerico<>), typeof(RepositorioGenerico<>));

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var types = LoaderAssemblies.BuscarClassesEInterfaces(
                    assemblies,
                    "Admin_Repository",
                    "Repository"
                );

                foreach (var type in types)
                {
                    var existingService = services.FirstOrDefault(s => s.ServiceType == type.Value);
                    if (existingService == null)
                        services.AddScoped(type.Value, type.Key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao registrar repositórios admin: {ex.Message}");
                throw;
            }
            return services;
        }
    }
}
