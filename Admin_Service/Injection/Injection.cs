using Admin_Service.ServiceGenerico;
using Admin_Service.ServiceGenerico.Interface;
using Microsoft.Extensions.DependencyInjection;
using Shared.Utils.LoaderAssembler;

namespace Admin_Service.Injection
{
    public static class Injection
    {
        public static IServiceCollection AddInjectionServiceAdmin(this IServiceCollection services)
        {
            try
            {
                // Registrar serviço genérico
                services.AddScoped(typeof(IServiceGenerico<>), typeof(ServiceGenerico<>));

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var types = LoaderAssemblies.BuscarClassesEInterfaces(
                    assemblies,
                    "Admin_Service",
                    "Service"
                );

                foreach (var (serviceType, interfaceType) in types)
                {
                    if (!services.Any(s => s.ServiceType == interfaceType))
                        services.AddScoped(interfaceType, serviceType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao registrar serviços admin: {ex.Message}");
                throw;
            }

            return services;
        }
    }
}