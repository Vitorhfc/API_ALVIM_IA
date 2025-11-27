using System.Reflection;

namespace Shared.Utils.LoaderAssembler
{
    public static class LoaderAssemblies
    {
        public static Type[] BuscarClasses(Assembly assembly, string @namespace, string? sufix = null)
        {
            List<Type> @class = [];
            (from type in assembly.GetTypes()
             where type.Namespace != null && type.IsPublic && type.Namespace.StartsWith(@namespace) && (string.IsNullOrEmpty(sufix) || !string.IsNullOrEmpty(sufix) && type.Name.EndsWith(sufix))
             select type).ToList().ForEach(delegate (Type type)
             {
                 @class.Add(type);
             });
            return [.. @class];
        }

        public static Dictionary<Type, Type>  BuscarClassesEInterfaces(Assembly[] assembly, string @namespace, string sufix)
        {
            try
            {
                var lst = assembly.OrderBy(d => d.FullName).ToList();

                IEnumerable<Type> obj = lst
                    .Where(a => !string.IsNullOrEmpty(a.FullName) && a.FullName.Contains(@namespace))
                    .SelectMany(a => a.GetTypes())
                    .Where(type =>
                        type.Namespace != null &&
                        type.Namespace.StartsWith(@namespace) &&
                        type.Name.EndsWith(sufix) &&
                        type.IsClass);

                Dictionary<Type, Type> classesAndInterfaces = [];
                if (obj != null && obj.Any())
                {
                    foreach (var type in obj.ToList())
                    {
                        try
                        {
                            Type value = type.GetInterfaces()
                                .FirstOrDefault(i =>
                                    i.Name.StartsWith("I") &&
                                    (i.Name.Substring(1).Split('`')[0] == type.Name.Split('`')[0])
                                );
                            classesAndInterfaces.Add(type, value);
                        }
                        catch
                        {
                        }


                    }
                    ;
                }
                return classesAndInterfaces;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao Buscar Classes e ImplementationsType ", ex);
            }
        }
    }
}
