namespace ServiceFlow.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterStaffAsync(RegisterStaffCommand command, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
}
