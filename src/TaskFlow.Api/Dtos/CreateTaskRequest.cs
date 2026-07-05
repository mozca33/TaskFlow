using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Corpo de criação de tarefa. A tarefa nasce Pending (D3): status não é aceito
/// aqui. priority é obrigatória — modelada como enum anulável para que a ausência
/// vire erro de validação (400) em vez de assumir Low silenciosamente.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateTaskRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required]
    public TaskItemPriority? Priority { get; init; }
}
