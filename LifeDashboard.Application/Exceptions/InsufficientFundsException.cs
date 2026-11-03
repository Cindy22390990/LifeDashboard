namespace LifeDashboard.Application.Exceptions;

public class InsufficientFundsException : ArgumentException
{
    public InsufficientFundsException(string message)
        : base(message)
    {
    }
}
