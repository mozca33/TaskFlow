namespace TaskFlow.Api.Domain;

/// <summary>
/// Prioridade de uma tarefa (obrigatória na criação). Serializado como string
/// minúscula no contrato: low/medium/high. Nomeado TaskItemPriority por
/// uniformidade com a entidade TaskItem e o enum TaskItemStatus.
/// </summary>
public enum TaskItemPriority
{
    Low,
    Medium,
    High
}
