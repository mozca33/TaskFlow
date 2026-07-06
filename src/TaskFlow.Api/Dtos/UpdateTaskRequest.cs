using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Corpo de atualização parcial de tarefa (D7). Cada campo é um
/// <see cref="Optional{T}"/>: omitido = inalterado; null explícito = limpar
/// (apenas onde o campo aceita null). Ao menos um campo deve ser informado.
/// completedAt não é aceito — é gerenciado pelo servidor (D4). A transição de
/// status segue a regra 5/D1, validada no serviço.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateTaskRequest : IValidatableObject
{
    public Optional<string?> Title { get; init; }

    public Optional<string?> Description { get; init; }

    public Optional<TaskItemStatus?> Status { get; init; }

    public Optional<TaskItemPriority?> Priority { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Title.IsSet && !Description.IsSet && !Status.IsSet && !Priority.IsSet)
        {
            yield return new ValidationResult(
                "Informe ao menos um campo para atualização.",
                new[] { nameof(Title), nameof(Description), nameof(Status), nameof(Priority) });
        }

        if (Title.IsSet)
        {
            if (Title.Value is null)
            {
                yield return new ValidationResult(
                    "O campo title não pode ser nulo.", new[] { nameof(Title) });
            }
            else if (string.IsNullOrWhiteSpace(Title.Value))
            {
                yield return new ValidationResult(
                    "O campo title não pode ser vazio ou conter apenas espaços.", new[] { nameof(Title) });
            }
            else if (Title.Value.Length > 200)
            {
                yield return new ValidationResult(
                    "O campo title deve ter no máximo 200 caracteres.", new[] { nameof(Title) });
            }
        }

        if (Description.IsSet && Description.Value is { Length: > 2000 })
        {
            yield return new ValidationResult(
                "O campo description deve ter no máximo 2000 caracteres.", new[] { nameof(Description) });
        }

        if (Status.IsSet && Status.Value is null)
        {
            yield return new ValidationResult(
                "O campo status não pode ser nulo.", new[] { nameof(Status) });
        }

        if (Priority.IsSet && Priority.Value is null)
        {
            yield return new ValidationResult(
                "O campo priority não pode ser nulo.", new[] { nameof(Priority) });
        }
    }
}
