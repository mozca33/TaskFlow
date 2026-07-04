namespace TaskFlow.Api.Domain;

/// <summary>
/// Prioridade de uma tarefa (obrigatória na criação). Serializado como string
/// minúscula no contrato: low/medium/high.
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High
}
