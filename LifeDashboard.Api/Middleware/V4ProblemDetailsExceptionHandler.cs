using LifeDashboard.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeDashboard.Api.Middleware;

public sealed class V4ProblemDetailsExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<V4ProblemDetailsExceptionHandler> _logger;

    public V4ProblemDetailsExceptionHandler(
        ILogger<V4ProblemDetailsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isV4Api = path.StartsWith(
            "/api/dashboard/v4",
            StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/me", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/transactions", StringComparison.OrdinalIgnoreCase);

        if (!isV4Api)
        {
            return false;
        }

        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "Internal Server Error";
        var type = "https://api.lifedashboard/errors/internal-server-error";
        var detail = "An unexpected server error occurred.";
        var errors = default(Dictionary<string, string[]>);

        switch (exception)
        {
            case NoCashflowAccountException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                title = "Cashflow Account Required";
                type = "https://api.lifedashboard/errors/no-cashflow-account";
                detail = "Create an active Cashflow account before creating a transaction.";
                break;
            case InsufficientFundsException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Insufficient Balance";
                type = "https://api.lifedashboard/errors/insufficient-balance";
                detail = "Account balance is insufficient for this expense.";
                break;
            case ArgumentOutOfRangeException argumentOutOfRange:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation Failed";
                type = "https://api.lifedashboard/errors/validation-failed";
                detail = "One or more query or request values are invalid.";
                errors = new Dictionary<string, string[]>
                {
                    [argumentOutOfRange.ParamName ?? "request"] =
                        ["The supplied value is invalid."]
                };
                break;
            case ArgumentException argumentException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation Failed";
                type = "https://api.lifedashboard/errors/validation-failed";
                detail = "One or more request values are invalid.";
                errors = new Dictionary<string, string[]>
                {
                    [argumentException.ParamName ?? "request"] =
                        ["The supplied value is invalid."]
                };
                break;
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title = "Not Found";
                type = "https://api.lifedashboard/errors/not-found";
                detail = "The requested resource was not found.";
                break;
            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Data Conflict";
                type = "https://api.lifedashboard/errors/concurrency-conflict";
                detail = "The account data changed before the transaction was completed.";
                break;
            default:
                _logger.LogError(
                    exception,
                    "Unhandled V4 API exception for {Path}",
                    httpContext.Request.Path);
                break;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ValidationProblemDetails(
            errors ?? new Dictionary<string, string[]>())
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);

        return true;
    }
}
