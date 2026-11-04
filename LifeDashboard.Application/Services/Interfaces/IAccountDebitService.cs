using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IAccountDebitService
{
    void Debit(Account account, decimal amount);
}
