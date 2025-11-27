using Microsoft.Extensions.DependencyInjection;
using Shared.Utils.LoaderAssembler;

namespace Client_Service.Injection
{
    public static class Injection
    {
        public static IServiceCollection AddInjectionServiceClient(this IServiceCollection services)
        {
            try
            {
                var types = LoaderAssemblies.BuscarClassesEInterfaces(
                    AppDomain.CurrentDomain.GetAssemblies(),
                    "Client_Service",
                    "Service"
                );

                foreach (var type in types)
                {
                    var existingService = services.FirstOrDefault(s => s.ServiceType == type.Value);
                    if (existingService == null)
                    {
                        services.AddScoped(type.Value, type.Key);
                        System.Diagnostics.Debug.WriteLine($"✓ Registrado: {type.Value.Name} -> {type.Key.Name}");
                    }
                    else
                        System.Diagnostics.Debug.WriteLine($"⊘ Já existe: {type.Value.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao registrar (SERVICE): {ex.Message}");
                throw;
            }

            return services;
        }
    }
}