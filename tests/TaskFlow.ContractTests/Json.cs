using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskFlow.ContractTests;

/// <summary>Utilitários de JSON para os testes, alinhados ao wire da API.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    /// <summary>Corpo application/json a partir de uma string JSON literal.</summary>
    public static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");
}
