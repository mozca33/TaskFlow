namespace TaskFlow.Api.Domain;

/// <summary>
/// Tarefa do TaskFlow (schema "Task" no contrato / rota /tarefas). Nomeada
/// TaskItem em C# para evitar colisão com System.Threading.Tasks.Task.
/// Nasce Pending; completedAt é gerenciado pelo servidor ao concluir (D3/D4).
/// </summary>
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public TaskPriority Priority { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Preenchido pelo servidor quando o status vira Done (D4). Read-only na API.</summary>
    public DateTime? CompletedAt { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>Projeto pai. Navegação para EF Core.</summary>
    public Project? Project { get; set; }
}
