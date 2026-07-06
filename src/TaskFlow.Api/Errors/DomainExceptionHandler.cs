using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Traduz exceções de domínio para respostas ProblemDetails (RFC 7807) num
/// ponto central, evitando espalhar tratamento de erro pelos controllers:
/// <see cref="NotFoundException"/> → 404, <see cref="BusinessRuleException"/> → 422.
/// Escreve via <see cref="IProblemDetailsService"/> para que 404/422 saiam com o
/// mesmo enriquecimento (type, traceId) do 400 automático do [ApiController],
/// mantendo o corpo de erro uniforme. Outras exceções ficam com o pipeline padrão.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public DomainExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            BusinessRuleException br => (StatusCodes.Status422UnprocessableEntity, br.Title),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return false; // não é exceção de domínio: deixa o pipeline padrão tratar.
        }

        httpContext.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = exception.Message
            }
        });
    }
}
