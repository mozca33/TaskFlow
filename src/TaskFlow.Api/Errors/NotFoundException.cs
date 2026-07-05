namespace TaskFlow.Api.Errors;

/// <summary>
/// Recurso inexistente. Mapeada para 404 (ProblemDetails) no pipeline HTTP.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
