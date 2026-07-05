using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

/// <summary>
/// Endpoints de projetos. Só orquestra HTTP ↔ caso de uso; as regras de negócio
/// e os erros (404/422) vêm do <see cref="IProjectService"/> e do handler central.
/// A constraint {id:guid} garante que UUID malformado vira 404 (D6).
/// </summary>
[ApiController]
[Route("projetos")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectsController(IProjectService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> List(
        [FromQuery] ProjectStatus? status, CancellationToken ct)
    {
        var projects = await _service.ListAsync(status, ct);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> GetById(Guid id, CancellationToken ct)
    {
        var project = await _service.GetByIdAsync(id, ct);
        return Ok(project);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(Guid id, UpdateProjectRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);
        return Ok(updated);
    }
}
