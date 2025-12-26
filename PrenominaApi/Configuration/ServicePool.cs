using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;
using PrenominaApi.Services;
using PrenominaApi.Services.Prenomina;
using System.Reflection;

namespace PrenominaApi.Configuration
{
    public class ServicePool
    {
        public static void RegistryService(WebApplicationBuilder webApplicationBuilder)
        {
            string pathDll = webApplicationBuilder.Configuration.GetValue<string>("PathDll") ?? "bin/Debug/net8.0/PrenominaApi.dll";
            string contentRootPath = Path.Combine(webApplicationBuilder.Environment.ContentRootPath, pathDll);

            RegistryDefault(webApplicationBuilder.Services);
            RegisterRepositories(contentRootPath, webApplicationBuilder.Services);
            RegisterServices(contentRootPath, webApplicationBuilder.Services);
        }

        private static void RegisterServices(string path, IServiceCollection services)
        {
            Assembly assembly = Assembly.LoadFrom(path);

            Type typeService = typeof(IBaseService<>);
            Type[] serviceTypes = GetTypesByInterface(typeService, assembly).Where(item => !(item.FullName ?? "").StartsWith("PrenominaApi.Services.Service")).ToArray();

            foreach (Type serviceType in serviceTypes)
            {
                Type? implementedInterface = serviceType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeService);

                if (implementedInterface != null)
                {
                    services.AddScoped(implementedInterface, serviceType);
                }
            }

            //Prenomina Api
            Type typeServicePrenominaApi = typeof(IBaseServicePrenomina<>);
            Type[] serviceTypesPrenominaApi = GetTypesByInterface(typeServicePrenominaApi, assembly).Where(item => !(item.FullName ?? "").StartsWith("PrenominaApi.Services.Prenomina.ServicePrenomina")).ToArray();

            foreach (Type serviceType in serviceTypesPrenominaApi)
            {
                Type? implementedInterface = serviceType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeServicePrenominaApi);

                if (implementedInterface != null)
                {
                    services.AddScoped(implementedInterface, serviceType);
                }
            }
        }

        private static void RegisterRepositories(string path, IServiceCollection services)
        {
            Assembly assembly = Assembly.LoadFrom(path);

            Type typeRepository = typeof(IBaseRepository<>);
            Type[] repositoryTypes = GetTypesByInterface(typeRepository, assembly).Where(item => !(item.FullName ?? "").StartsWith("PrenominaApi.Repositories.Repository")).ToArray();

            foreach (Type repoType in repositoryTypes)
            {
                Type? implementedInterface = repoType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeRepository);

                if (implementedInterface != null)
                {
                    services.AddScoped(implementedInterface, repoType);
                }
            }
        }

        private static Type[] GetTypesByInterface(Type interfaceType, Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType))
                .ToArray();
        }

        public static void RegistryDefault(IServiceCollection services)
        {
            services.AddScoped(typeof(IBaseRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IBaseRepositoryPrenomina<>), typeof(RepositoryPrenomina<>));
            services.AddScoped(typeof(IBaseService<>), typeof(Service<>));
            services.AddScoped(typeof(IBaseServicePrenomina<>), typeof(ServicePrenomina<>));
        }
    }
}
