namespace LifeDashboard.Application.Exceptions;

public sealed class NoCashflowAccountException : Exception
{
    public NoCashflowAccountException()
        : base("An active Cashflow account is required to create a transaction.")
    {
    }
}
