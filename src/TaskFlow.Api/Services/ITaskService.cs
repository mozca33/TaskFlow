using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Services;

/// <summary>Casos de uso de tarefas. As regras 2–5 vivem aqui.</summary>
public interface ITaskService
{
    Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<TaskResponse>> ListAsync(
        Guid projectId, TaskItemStatus? status, TaskItemPriority? priority, CancellationToken ct = default);

    Task<TaskResponse> UpdateAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid taskId, CancellationToken ct = default);
}
