namespace ServiceFlow.Application.Common.Exceptions;

public sealed class ForbiddenException : Exception
{
    public string Code { get; }

    public ForbiddenException(string message = "Access denied.", string code = "auth.forbidden")
        : base(message)
    {
        Code = code;
    }
}
