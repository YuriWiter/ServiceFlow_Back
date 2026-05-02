using FluentValidation;

namespace ServiceFlow.Application.Common;

internal static class ValidationExtensions
{
    public static async Task ValidateAndThrowAppAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        if (!result.IsValid)
        {
            throw new Exceptions.ValidationException(result.Errors);
        }
    }
}
