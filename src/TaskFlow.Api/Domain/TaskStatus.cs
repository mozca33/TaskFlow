namespace TaskFlow.Api.Domain;

/// <summary>
/// Estados possíveis de uma tarefa. A transição é estrita
/// (pending → in_progress → done), controlada pelo serviço (ver D1/regra 5).
/// Serializado como string minúscula no contrato: pending/in_progress/done.
/// </summary>
public enum TaskStatus
{
    Pending,
    InProgress,
    Done
}
