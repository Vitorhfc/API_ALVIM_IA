using Client_Repository.Repositorio;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Client_Repository.RepositorioGenerico.Interface;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Client_Repository.Injection
{
    public static class InjectionDependency
    {
        public static IServiceCollection AddInjectionRepositoryClient(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepositorioGenerico<>), typeof(RepositorioGenerico<>));

            var assembly = Assembly.GetExecutingAssembly();
            var repositorios = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repositorio"))
                .ToList();

            foreach (var repositorio in repositorios)
            {
                var interfaces = repositorio.GetInterfaces()
                    .Where(i => i.Name.EndsWith("Repositorio") || i.Name.EndsWith("Repository"))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, repositorio);
                }
            }

            return services;
        }
    }
}