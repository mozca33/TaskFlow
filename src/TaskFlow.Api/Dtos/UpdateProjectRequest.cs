using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Corpo de atualização parcial de projeto (D7). Cada campo é um
/// <see cref="Optional{T}"/>: omitido = inalterado; null explícito = limpar
/// (apenas onde o campo aceita null). Ao menos um campo deve ser informado.
/// Arquivar (status=archived) está sujeito à regra 1, validada no serviço.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateProjectRequest : IValidatableObject
{
    public Optional<string?> Name { get; init; }

    public Optional<string?> Description { get; init; }

    public Optional<ProjectStatus?> Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Name.IsSet && !Description.IsSet && !Status.IsSet)
        {
            yield return new ValidationResult(
                "Informe ao menos um campo para atualização.",
                new[] { nameof(Name), nameof(Description), nameof(Status) });
        }

        if (Name.IsSet)
        {
            if (Name.Value is null)
            {
                yield return new ValidationResult(
                    "O campo name não pode ser nulo.", new[] { nameof(Name) });
            }
            else if (Name.Value.Length is < 1 or > 100)
            {
                yield return new ValidationResult(
                    "O campo name deve ter entre 1 e 100 caracteres.", new[] { nameof(Name) });
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
    }
}
