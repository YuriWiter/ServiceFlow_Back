using ServiceFlow.Application.Common.Abstractions;

namespace ServiceFlow.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
