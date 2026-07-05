using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Representação de saída de uma tarefa (schema Task). completedAt é read-only,
/// preenchido pelo servidor ao concluir (D4).
/// </summary>
public record TaskResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required TaskItemStatus Status { get; init; }
    public required TaskItemPriority Priority { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required Guid ProjectId { get; init; }

    public static TaskResponse FromEntity(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        ProjectId = task.ProjectId
    };
}
