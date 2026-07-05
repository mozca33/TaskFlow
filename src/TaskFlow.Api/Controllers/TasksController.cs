using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

/// <summary>
/// Endpoints de tarefas. Criação e listagem são no contexto de um projeto
/// (/projetos/{projectId}/tarefas); atualização e exclusão referenciam a tarefa
/// diretamente (/tarefas/{id}). Regras e erros vêm do <see cref="ITaskService"/>.
/// </summary>
[ApiController]
public class TasksController : ControllerBase
{
    private readonly ITaskService _service;

    public TasksController(ITaskService service)
    {
        _service = service;
    }

    [HttpPost("projetos/{projectId:guid}/tarefas")]
    public async Task<ActionResult<TaskResponse>> Create(
        Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(projectId, request, ct);
        return Created($"/tarefas/{created.Id}", created);
    }

    [HttpGet("projetos/{projectId:guid}/tarefas")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List(
        Guid projectId,
        [FromQuery] TaskItemStatus? status,
        [FromQuery] TaskItemPriority? priority,
        CancellationToken ct)
    {
        var tasks = await _service.ListAsync(projectId, status, priority, ct);
        return Ok(tasks);
    }

    [HttpPatch("tarefas/{id:guid}")]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("tarefas/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
