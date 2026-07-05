using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Traduz exceções de domínio para respostas ProblemDetails (RFC 7807) num
/// ponto central, evitando espalhar tratamento de erro pelos controllers:
/// <see cref="NotFoundException"/> → 404, <see cref="BusinessRuleException"/> → 422.
/// Outras exceções são deixadas para o tratamento padrão do framework.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            NotFoundException nf => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = nf.Message
            },
            BusinessRuleException br => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = br.Title,
                Detail = br.Message
            },
            _ => null
        };

        if (problem is null)
        {
            return false; // não é exceção de domínio: deixa o pipeline padrão tratar.
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
