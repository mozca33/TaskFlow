using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Envelope para campos de PATCH parcial que distingue três estados:
/// <list type="bullet">
/// <item>ausente no JSON → <see cref="IsSet"/> = false (deixar inalterado);</item>
/// <item>presente com valor → IsSet = true, <see cref="Value"/> preenchido;</item>
/// <item>presente como null → IsSet = true, Value = null (intenção explícita de limpar).</item>
/// </list>
/// O conversor só é chamado quando a propriedade existe no corpo, então um campo
/// omitido mantém o default (IsSet = false) — é assim que separamos "ausente" de "null".
/// </summary>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public bool IsSet { get; }
    public T? Value { get; }

    private Optional(T? value)
    {
        IsSet = true;
        Value = value;
    }

    public static Optional<T> Set(T? value) => new(value);
}

/// <summary>Cria o conversor certo para cada <see cref="Optional{T}"/>.</summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Desserializa o valor interno reaproveitando os conversores já configurados
/// (ex.: enums como snake_case). Presença de token — inclusive null — marca IsSet.
/// </summary>
public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional<T>.Set(value);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
