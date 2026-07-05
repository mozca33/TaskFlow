using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Persistence;

namespace TaskFlow.Api.Services;

/// <summary>
/// Casos de uso de tarefas. Concentra as regras 2 (exclusão só de pending),
/// 3 (completedAt automático ao concluir), 4 (projeto arquivado não aceita
/// tarefa) e 5/D1 (transição estrita pending → in_progress → done).
/// </summary>
public class TaskService : ITaskService
{
    private readonly TaskFlowDbContext _db;

    public TaskService(TaskFlowDbContext db)
    {
        _db = db;
    }

    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Projeto não encontrado.");

        // Regra 4: projeto arquivado não aceita novas tarefas.
        if (project.Status == ProjectStatus.Archived)
        {
            throw new BusinessRuleException(
                "Não é possível criar tarefa",
                "O projeto está arquivado e não aceita novas tarefas.");
        }

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority!.Value,
            ProjectId = projectId
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        return TaskResponse.FromEntity(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(
        Guid projectId, TaskItemStatus? status, TaskItemPriority? priority, CancellationToken ct = default)
    {
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!projectExists)
        {
            throw new NotFoundException("Projeto não encontrado.");
        }

        var query = _db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId);

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(t => t.Priority == priority);
        }

        var tasks = await query.OrderBy(t => t.CreatedAt).ToListAsync(ct);
        return tasks.Select(TaskResponse.FromEntity).ToList();
    }

    public async Task<TaskResponse> UpdateAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Tarefa não encontrada.");

        if (request.Title.IsSet)
        {
            task.Title = request.Title.Value!;
        }

        if (request.Description.IsSet)
        {
            task.Description = request.Description.Value;
        }

        if (request.Priority.IsSet)
        {
            task.Priority = request.Priority.Value!.Value;
        }

        if (request.Status.IsSet)
        {
            ApplyStatusTransition(task, request.Status.Value!.Value);
        }

        await _db.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task DeleteAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Tarefa não encontrada.");

        // Regra 2: só é possível excluir tarefa com status pending.
        if (task.Status != TaskItemStatus.Pending)
        {
            throw new BusinessRuleException(
                "Não é possível excluir a tarefa",
                "Apenas tarefas com status pending podem ser excluídas.");
        }

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Regra 5/D1: transição estrita pending → in_progress → done, um passo à
    /// frente, sem pular etapa nem retroceder. Mudar para o mesmo status é no-op.
    /// Regra 3: ao concluir (→ done), completedAt é gravado automaticamente.
    /// </summary>
    private static void ApplyStatusTransition(TaskItem task, TaskItemStatus newStatus)
    {
        if (newStatus == task.Status)
        {
            return;
        }

        var transicaoValida =
            (task.Status == TaskItemStatus.Pending && newStatus == TaskItemStatus.InProgress) ||
            (task.Status == TaskItemStatus.InProgress && newStatus == TaskItemStatus.Done);

        if (!transicaoValida)
        {
            throw new BusinessRuleException(
                "Transição de status inválida",
                $"Não é permitido ir de {ToWire(task.Status)} para {ToWire(newStatus)}: " +
                "a transição deve seguir pending → in_progress → done, um passo por vez, sem retroceder.");
        }

        task.Status = newStatus;

        if (newStatus == TaskItemStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Nome do status como aparece no contrato (snake_case).</summary>
    private static string ToWire(TaskItemStatus status) => status switch
    {
        TaskItemStatus.Pending => "pending",
        TaskItemStatus.InProgress => "in_progress",
        TaskItemStatus.Done => "done",
        _ => status.ToString()
    };
}
