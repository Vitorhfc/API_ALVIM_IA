using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Shared.Utils
{
    public static class Uteis
    {
        public static string GerarNomeBaseDados(string CPF)
        {
            if (string.IsNullOrEmpty(CPF))
                throw new ArgumentException("CPF é obrigatório");

            var cnpjCpfLimpo = CPF
                .Replace(".", "")
                .Replace("/", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim();

            return $"IaAtendimentoCliente_{cnpjCpfLimpo}";
        }

    }

    public static class EnumHelper
    {
        /// <summary>
        /// Converte um valor Enum em string. 
        /// Se o enum possuir o atributo [Description], retorna o texto da descrição.
        /// </summary>
        public static string EnumToString<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var field = typeof(TEnum).GetField(value.ToString());
            var description = field?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            return description ?? value.ToString();
        }

        /// <summary>
        /// Converte uma string em Enum, ignorando maiúsculas/minúsculas.
        /// Se a conversão falhar, retorna o valor padrão do enum (default).
        /// </summary>
        public static TEnum StringToEnum<TEnum>(string value) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            // tenta converter pelo nome do enum
            if (Enum.TryParse(value, true, out TEnum result))
                return result;

            // tenta converter pela descrição [Description]
            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (description != null && string.Equals(description, value, StringComparison.OrdinalIgnoreCase))
                {
                    return (TEnum)field.GetValue(null);
                }
            }

            return default;
        }
    }
}
