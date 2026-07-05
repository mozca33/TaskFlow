using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Corpo de atualização parcial de tarefa (D7): campo omitido (null) fica
/// inalterado. Ao menos um campo deve ser informado. completedAt não é aceito —
/// é gerenciado pelo servidor (D4). A transição de status segue a regra 5/D1,
/// validada no serviço.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateTaskRequest : IValidatableObject
{
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    public TaskItemStatus? Status { get; init; }

    public TaskItemPriority? Priority { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title is null && Description is null && Status is null && Priority is null)
        {
            yield return new ValidationResult(
                "Informe ao menos um campo para atualização.",
                new[] { nameof(Title), nameof(Description), nameof(Status), nameof(Priority) });
        }
    }
}
