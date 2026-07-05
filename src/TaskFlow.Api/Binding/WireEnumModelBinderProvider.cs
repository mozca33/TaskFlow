using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Binding;

/// <summary>
/// Aplica o <see cref="WireEnumModelBinder"/> apenas aos enums de domínio usados
/// como filtro (query string). Demais tipos seguem os binders padrão.
/// </summary>
public sealed class WireEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        if (type == typeof(ProjectStatus) ||
            type == typeof(TaskItemStatus) ||
            type == typeof(TaskItemPriority))
        {
            return new WireEnumModelBinder();
        }

        return null;
    }
}
