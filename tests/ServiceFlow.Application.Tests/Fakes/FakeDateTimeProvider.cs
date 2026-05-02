using ServiceFlow.Application.Common.Abstractions;

namespace ServiceFlow.Application.Tests.Fakes;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
}
