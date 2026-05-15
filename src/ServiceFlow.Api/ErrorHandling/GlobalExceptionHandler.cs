using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Common;

namespace ServiceFlow.Api.ErrorHandling;

/// <summary>
/// Single place to translate thrown exceptions into RFC 7807 ProblemDetails responses.
/// Keeps endpoints clean and guarantees a consistent error contract for clients.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
            return false;

        var problem = exception switch
        {
            ValidationException ve => Build(
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                extensions: new Dictionary<string, object?> { ["errors"] = ve.Errors }),

            DomainException de => Build(
                StatusCodes.Status400BadRequest,
                "Domain rule violated",
                de.Message,
                extensions: new Dictionary<string, object?> { ["code"] = de.Code }),

            NotFoundException nf => Build(
                StatusCodes.Status404NotFound,
                "Not found",
                nf.Message,
                extensions: new Dictionary<string, object?> { ["code"] = nf.Code }),

            ConflictException ce => Build(
                StatusCodes.Status409Conflict,
                "Conflict",
                ce.Message,
                extensions: new Dictionary<string, object?> { ["code"] = ce.Code }),

            ForbiddenException fe => Build(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                fe.Message,
                extensions: new Dictionary<string, object?> { ["code"] = fe.Code }),

            _ => null
        };

        if (problem is null)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

            problem = Build(
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred. Please try again later.");
        }
        else
        {
            _logger.LogWarning(exception, "{StatusCode} {Title}: {Detail}", problem.Status, problem.Title, problem.Detail);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Build(
        int status,
        string title,
        string detail,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        if (extensions is not null)
        {
            foreach (var (k, v) in extensions)
            {
                problem.Extensions[k] = v;
            }
        }

        return problem;
    }
}
