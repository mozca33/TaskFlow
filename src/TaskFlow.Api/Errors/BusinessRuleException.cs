namespace TaskFlow.Api.Errors;

/// <summary>
/// Violação de uma regra de negócio (regras 1–5 do enunciado). Mapeada para
/// 422 (ProblemDetails) no pipeline HTTP. <see cref="Title"/> vira o title do
/// ProblemDetails e a Message vira o detail explicativo exigido pelo enunciado.
/// </summary>
public class BusinessRuleException : Exception
{
    public string Title { get; }

    public BusinessRuleException(string title, string detail) : base(detail)
    {
        Title = title;
    }
}
