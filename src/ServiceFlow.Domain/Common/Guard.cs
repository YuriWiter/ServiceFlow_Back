using System.Runtime.CompilerServices;

namespace ServiceFlow.Domain.Common;

/// <summary>
/// Centralized invariant checks. Keeps entity constructors / methods small and readable.
/// Every violation raises a <see cref="DomainException"/> with a stable error code.
/// </summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(
        string? value,
        string code,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(code, $"'{paramName}' is required.");
        }

        return value.Trim();
    }

    public static string MaxLength(
        string value,
        int max,
        string code,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Length > max)
        {
            throw new DomainException(code, $"'{paramName}' must be at most {max} characters.");
        }

        return value;
    }

    public static int InRange(
        int value,
        int min,
        int max,
        string code,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < min || value > max)
        {
            throw new DomainException(code, $"'{paramName}' must be between {min} and {max}.");
        }

        return value;
    }

    public static long NotNegative(
        long value,
        string code,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
        {
            throw new DomainException(code, $"'{paramName}' must be >= 0.");
        }

        return value;
    }
}
