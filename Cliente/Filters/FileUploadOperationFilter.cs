using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Client.Filters
{
    /// <summary>
    /// Filtro para configurar upload de arquivos no Swagger
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Verifica se o método consome multipart/form-data
            var consumesAttribute = context.MethodInfo
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ConsumesAttribute), false)
                .Cast<Microsoft.AspNetCore.Mvc.ConsumesAttribute>()
                .FirstOrDefault();

            if (consumesAttribute == null || !consumesAttribute.ContentTypes.Contains("multipart/form-data"))
                return;

            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                            p.ParameterType == typeof(IFormFile[]) ||
                            p.ParameterType == typeof(IEnumerable<IFormFile>))
                .ToList();

            if (!fileParameters.Any())
                return;

            // Criar schema para multipart/form-data
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new HashSet<string>()
            };

            // Adicionar todos os parâmetros do método
            foreach (var parameter in context.MethodInfo.GetParameters())
            {
                if (parameter.ParameterType == typeof(IFormFile))
                {
                    schema.Properties[parameter.Name!] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = $"Arquivo: {parameter.Name}"
                    };

                    // Se não for opcional, marcar como required
                    if (!IsNullable(parameter))
                    {
                        schema.Required.Add(parameter.Name!);
                    }
                }
                else if (parameter.ParameterType == typeof(IFormFile[]) ||
                         parameter.ParameterType == typeof(IEnumerable<IFormFile>))
                {
                    schema.Properties[parameter.Name!] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        },
                        Description = $"Múltiplos arquivos: {parameter.Name}"
                    };

                    if (!IsNullable(parameter))
                    {
                        schema.Required.Add(parameter.Name!);
                    }
                }
                else
                {
                    // Outros parâmetros do form (como string, int, etc)
                    var propertySchema = new OpenApiSchema
                    {
                        Type = GetSchemaType(parameter.ParameterType),
                        Description = parameter.Name
                    };

                    schema.Properties[parameter.Name!] = propertySchema;

                    if (!IsNullable(parameter))
                    {
                        schema.Required.Add(parameter.Name!);
                    }
                }
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };

            // Limpar parâmetros porque agora estão no RequestBody
            operation.Parameters?.Clear();
        }

        private bool IsNullable(ParameterInfo parameter)
        {
            return parameter.IsOptional ||
                   parameter.HasDefaultValue ||
                   Nullable.GetUnderlyingType(parameter.ParameterType) != null ||
                   !parameter.ParameterType.IsValueType;
        }

        private string GetSchemaType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int) || underlyingType == typeof(long))
                return "integer";
            if (underlyingType == typeof(bool))
                return "boolean";
            if (underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal))
                return "number";
            if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
                return "string";

            return "string";
        }
    }
}
