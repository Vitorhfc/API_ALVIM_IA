using System.Text.Json;

namespace Shared.Helpers
{
    public static class JsonCleanerHelper
    {
        public static JsonElement LimparCampos(string json, IEnumerable<string> jsonPaths)
        {
            return LimparCampos(json, jsonPaths, false);
        }

        public static JsonElement RemoverCampos(string json, IEnumerable<string> jsonPaths)
        {
            return LimparCampos(json, jsonPaths, true);
        }

        private static JsonElement LimparCampos(string json, IEnumerable<string> jsonPaths, bool removerPropriedade)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var paths = jsonPaths
                .Select(ParsePath)
                .ToList();

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                ProcessarLimpezaPropriedade(root, writer, [], paths, removerPropriedade);
            }

            stream.Position = 0;
            using var result = JsonDocument.Parse(stream);
            return result.RootElement.Clone();
        }

        private static void ProcessarLimpezaPropriedade(JsonElement element, Utf8JsonWriter writer, string[] pathSoFar, List<string[]> pathsParaZerar, bool removerPropriedade)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();

                    foreach (var prop in element.EnumerateObject())
                    {
                        var novoPath = pathSoFar.Append(prop.Name).ToArray();

                        if (ValidaDeveRemoverPropriedade(novoPath, pathsParaZerar))
                        {
                            if (removerPropriedade)
                            {
                                continue;
                            }
                            else
                            {
                                writer.WritePropertyName(prop.Name);
                                writer.WriteNullValue();
                                continue;
                            }
                        }

                        writer.WritePropertyName(prop.Name);
                        ProcessarLimpezaPropriedade(prop.Value, writer, novoPath, pathsParaZerar, removerPropriedade);
                    }

                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();

                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var novoPath = pathSoFar.Append(index.ToString()).ToArray();
                        ProcessarLimpezaPropriedade(item, writer, novoPath, pathsParaZerar, removerPropriedade);
                        index++;
                    }

                    writer.WriteEndArray();
                    break;

                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        private static bool ValidaDeveRemoverPropriedade(string[] pathAtual, List<string[]> pathsParaZerar)
        {
            foreach (var pathTarget in pathsParaZerar)
            {
                if (pathAtual.Length != pathTarget.Length)
                    continue;

                bool coincide = true;
                for (int i = 0; i < pathTarget.Length; i++)
                {
                    if (pathTarget[i] != "*" && pathTarget[i] != pathAtual[i])
                    {
                        coincide = false;
                        break;
                    }
                }

                if (coincide)
                    return true;
            }

            return false;
        }

        private static string[] ParsePath(string rawPath)
        {
            var result = new List<string>();
            var parts = rawPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                // Divide "teste[*]" ou "itens[3]" em "teste" e "*"
                var openBracketIndex = part.IndexOf('[');
                if (openBracketIndex >= 0 && part.EndsWith(']'))
                {
                    var propName = part[..openBracketIndex];
                    var indexOrWildcard = part[(openBracketIndex + 1)..^1]; // remove [ e ]

                    if (!string.IsNullOrEmpty(propName))
                        result.Add(propName);

                    result.Add(indexOrWildcard);
                }
                else
                {
                    result.Add(part);
                }
            }

            return [.. result];
        }

    }
}