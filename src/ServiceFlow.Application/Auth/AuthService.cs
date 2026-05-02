using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterStaffCommand> _registerStaffValidator;
    private readonly IValidator<RegisterCustomerCommand> _registerCustomerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IValidator<RegisterStaffCommand> registerStaffValidator,
        IValidator<RegisterCustomerCommand> registerCustomerValidator,
        IValidator<LoginCommand> loginValidator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _registerStaffValidator = registerStaffValidator;
        _registerCustomerValidator = registerCustomerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterStaffAsync(RegisterStaffCommand command, CancellationToken cancellationToken = default)
    {
        await _registerStaffValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == emailNormalized, cancellationToken);
        if (exists)
        {
            throw new ConflictException("user.email.taken", "Email is already registered.");
        }

        var user = User.Create(command.Email, command.FullName, _passwordHasher.Hash(command.Password), command.Role);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return IssueResponse(user);
    }

    public async Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await _registerCustomerValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == emailNormalized, cancellationToken);
        if (exists)
        {
            throw new ConflictException("user.email.taken", "Email is already registered.");
        }

        var user = User.Create(command.Email, command.FullName, _passwordHasher.Hash(command.Password), UserRole.Customer);
        _db.Users.Add(user);

        var customer = Customer.Create(command.FullName, command.PhoneNumber, emailNormalized, user.Id);
        _db.Customers.Add(customer);

        await _db.SaveChangesAsync(cancellationToken);

        return IssueResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailNormalized, cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new ForbiddenException("Invalid email or password.", "auth.invalid_credentials");
        }

        return IssueResponse(user);
    }

    private AuthResponse IssueResponse(User user)
    {
        var token = _jwtTokenService.IssueAccessToken(user);
        return new AuthResponse(user.Id, user.Email, user.FullName, user.Role, token.Token, token.ExpiresAt);
    }
}
