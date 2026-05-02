namespace ServiceFlow.Domain.Common;

/// <summary>
/// Thrown when a domain invariant is violated. The Application layer translates this
/// into a 4xx response so that validation failures never leak as 500s.
/// </summary>
public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
