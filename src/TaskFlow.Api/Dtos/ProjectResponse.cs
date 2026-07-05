using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Representação de saída de um projeto (schema Project). Não expõe a coleção
/// de tarefas nem detalhes de persistência.
/// </summary>
public record ProjectResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ProjectStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }

    public static ProjectResponse FromEntity(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        Status = project.Status,
        CreatedAt = project.CreatedAt
    };
}
