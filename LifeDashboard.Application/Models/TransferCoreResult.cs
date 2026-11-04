using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Models;

public sealed record TransferCoreResult(
    Transaction Transaction,
    Account SourceAccount,
    Account DestinationAccount);
