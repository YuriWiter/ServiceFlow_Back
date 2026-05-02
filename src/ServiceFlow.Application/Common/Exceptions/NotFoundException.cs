namespace ServiceFlow.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public string Code { get; }

    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
        Code = $"{entity.ToLowerInvariant()}.not_found";
    }
}
