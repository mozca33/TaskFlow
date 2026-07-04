namespace TaskFlow.Api.Domain;

/// <summary>
/// Projeto do TaskFlow. Agrupa tarefas (1 projeto → N tarefas). O id e o
/// createdAt são gerados pelo servidor; o status nasce Active (ver decisões).
/// </summary>
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Tarefas pertencentes a este projeto.</summary>
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
