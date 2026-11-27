using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Services;
using LifeDashboard.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class V4TransactionsController : ControllerBase
{
    private readonly IV4TransactionService _transactionService;

    public V4TransactionsController(IV4TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TransactionListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionListResponseDto>> GetTransactions(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] DateOnly? date,
        [FromQuery] string? keyword,
        [FromQuery] string? type,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? transactionType,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "page 必須大於或等於 1。");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "pageSize 必須介於 1 到 100。");
        }

        if (year.HasValue && (year.Value < 1 || year.Value > 9999))
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "year 必須介於 1 到 9999。");
        }

        if (month.HasValue && (month.Value < 1 || month.Value > 12))
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "month 必須介於 1 到 12。");
        }

        if (!date.HasValue && year.HasValue != month.HasValue)
        {
            throw new ArgumentException(
                "year 與 month 必須一併提供。",
                nameof(year));
        }

        if (startDate.HasValue &&
            endDate.HasValue &&
            startDate.Value > endDate.Value)
        {
            throw new ArgumentException(
                "startDate 不可晚於 endDate。",
                nameof(startDate));
        }

        var parsedType = ParseTransactionType(
            transactionType,
            nameof(transactionType));
        var parsedLegacyType = ParseTransactionType(type, nameof(type));
        if (parsedType.HasValue &&
            parsedLegacyType.HasValue &&
            parsedType.Value != parsedLegacyType.Value)
        {
            throw new ArgumentException(
                "type 與 transactionType 不可指定不同的交易類型。",
                nameof(transactionType));
        }

        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy)
            ? null
            : sortBy.Trim().ToLowerInvariant();
        if (normalizedSortBy is not null &&
            normalizedSortBy is not ("transactiondate" or "amount" or "note"))
        {
            throw new ArgumentException(
                "sortBy 僅支援 transactionDate、amount 或 note。",
                nameof(sortBy));
        }

        var normalizedSortDirection = string.IsNullOrWhiteSpace(sortDirection)
            ? "desc"
            : sortDirection.Trim().ToLowerInvariant();
        if (normalizedSortDirection is not ("asc" or "desc"))
        {
            throw new ArgumentException(
                "sortDirection 僅支援 asc 或 desc。",
                nameof(sortDirection));
        }

        var result = await _transactionService.GetTransactionsAsync(
            User.GetUserId(),
            new V4TransactionQuery(
                year,
                month,
                date,
                startDate,
                endDate,
                keyword,
                accountId,
                categoryId,
                parsedType ?? parsedLegacyType,
                normalizedSortBy,
                normalizedSortDirection,
                page,
                pageSize),
            cancellationToken);

        return Ok(result);
    }

    private static TransactionType? ParseTransactionType(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TransactionType>(
                value.Trim(),
                ignoreCase: true,
                out var transactionType) ||
            !Enum.IsDefined(transactionType) ||
            !string.Equals(
                transactionType.ToString(),
                value.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "不支援的交易類型。",
                parameterName);
        }

        return transactionType;
    }

    [HttpPost("quick")]
    [ProducesResponseType(typeof(V4TransactionItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<V4TransactionItemDto>> QuickCreate(
        [FromBody] QuickCreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.QuickCreateAsync(
            User.GetUserId(),
            request,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        return Created(
            $"/api/transactions/{result.Id}",
            result);
    }
}
