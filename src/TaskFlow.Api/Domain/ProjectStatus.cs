namespace TaskFlow.Api.Domain;

/// <summary>
/// Estados possíveis de um projeto. Serializado como string minúscula
/// (active/archived) no contrato e persistido como string no banco (ver D9/D8).
/// </summary>
public enum ProjectStatus
{
    Active,
    Archived
}
