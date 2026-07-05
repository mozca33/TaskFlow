using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Corpo de atualização parcial de projeto (D7): campo omitido (null) fica
/// inalterado. Ao menos um campo deve ser informado. Arquivar (status=archived)
/// está sujeito à regra 1, validada no serviço.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateProjectRequest : IValidatableObject
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    public ProjectStatus? Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null && Description is null && Status is null)
        {
            yield return new ValidationResult(
                "Informe ao menos um campo para atualização.",
                new[] { nameof(Name), nameof(Description), nameof(Status) });
        }
    }
}
