using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Services;

/// <summary>Casos de uso de projetos. As regras de negócio vivem aqui.</summary>
public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ProjectResponse>> ListAsync(ProjectStatus? status, CancellationToken ct = default);

    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default);
}
