using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Persistence;

namespace TaskFlow.Api.Services;

/// <summary>
/// Casos de uso de projetos. Concentra a regra 1 (arquivamento condicionado) e
/// a D2 (desarquivar é livre). Toda ausência de recurso vira NotFoundException.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly TaskFlowDbContext _db;

    public ProjectService(TaskFlowDbContext db)
    {
        _db = db;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return ProjectResponse.FromEntity(project);
    }

    public async Task<IReadOnlyList<ProjectResponse>> ListAsync(ProjectStatus? status, CancellationToken ct = default)
    {
        var query = _db.Projects.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        var projects = await query.OrderBy(p => p.CreatedAt).ToListAsync(ct);
        return projects.Select(ProjectResponse.FromEntity).ToList();
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Projeto não encontrado.");

        return ProjectResponse.FromEntity(project);
    }

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Projeto não encontrado.");

        if (request.Name.IsSet)
        {
            project.Name = request.Name.Value!;
        }

        if (request.Description.IsSet)
        {
            project.Description = request.Description.Value;
        }

        if (request.Status.IsSet)
        {
            var newStatus = request.Status.Value!.Value;
            await ApplyStatusChangeAsync(project, newStatus, ct);
        }

        await _db.SaveChangesAsync(ct);
        return ProjectResponse.FromEntity(project);
    }

    /// <summary>
    /// Regra 1: só permite arquivar se nenhuma tarefa do projeto estiver
    /// in_progress. Desarquivar (archived → active) é livre (D2).
    /// </summary>
    private async Task ApplyStatusChangeAsync(Project project, ProjectStatus newStatus, CancellationToken ct)
    {
        var isArchiving = newStatus == ProjectStatus.Archived && project.Status != ProjectStatus.Archived;

        if (isArchiving)
        {
            var hasInProgressTask = await _db.Tasks
                .AnyAsync(t => t.ProjectId == project.Id && t.Status == TaskItemStatus.InProgress, ct);

            if (hasInProgressTask)
            {
                throw new BusinessRuleException(
                    "Não é possível arquivar o projeto",
                    "O projeto possui tarefas em andamento (in_progress) e não pode ser arquivado.");
            }
        }

        project.Status = newStatus;
    }
}
