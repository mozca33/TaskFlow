using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskFlow.Api.Binding;

/// <summary>
/// Faz o binding de enums vindos da query string aceitando os valores do
/// contrato em snake_case (ex.: <c>?status=in_progress</c>). O model binder
/// padrão só reconhece o nome PascalCase ou o número, então reaproveitamos o
/// mesmo conversor JSON do wire para manter uma única fonte de verdade. Valor
/// inválido vira erro de ModelState → 400 (ValidationProblemDetails).
/// </summary>
public sealed class WireEnumModelBinder : IModelBinder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;
        var valueResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask; // filtro ausente → permanece null (opcional)
        }

        var raw = valueResult.FirstValue;
        if (string.IsNullOrEmpty(raw))
        {
            return Task.CompletedTask;
        }

        var enumType = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;

        if (TryParse(enumType, raw, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                modelName, $"Valor inválido para o filtro '{modelName}': '{raw}'.");
        }

        return Task.CompletedTask;
    }

    private static bool TryParse(Type enumType, string raw, out object? value)
    {
        try
        {
            // Serializa o texto como string JSON e desserializa via o conversor snake_case.
            value = JsonSerializer.Deserialize(JsonSerializer.Serialize(raw), enumType, Options);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
