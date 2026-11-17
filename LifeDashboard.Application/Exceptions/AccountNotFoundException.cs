namespace LifeDashboard.Application.Exceptions;

public sealed class AccountNotFoundException : KeyNotFoundException
{
    public AccountNotFoundException(string message)
        : base(message)
    {
    }
}
