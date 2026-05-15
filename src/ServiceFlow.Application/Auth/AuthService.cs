using FluentValidation;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IServiceFlowPersistence _persistence;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IFirestoreSyncService _firestoreSync;
    private readonly IFirestoreUserReader _firestoreUserReader;
    private readonly IFirestoreMigrationSettings _firestoreMigration;
    private readonly IValidator<RegisterStaffCommand> _registerStaffValidator;
    private readonly IValidator<RegisterCustomerCommand> _registerCustomerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;

    public AuthService(
        IServiceFlowPersistence persistence,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IFirestoreSyncService firestoreSync,
        IFirestoreUserReader firestoreUserReader,
        IFirestoreMigrationSettings firestoreMigration,
        IValidator<RegisterStaffCommand> registerStaffValidator,
        IValidator<RegisterCustomerCommand> registerCustomerValidator,
        IValidator<LoginCommand> loginValidator)
    {
        _persistence = persistence;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _firestoreSync = firestoreSync;
        _firestoreUserReader = firestoreUserReader;
        _firestoreMigration = firestoreMigration;
        _registerStaffValidator = registerStaffValidator;
        _registerCustomerValidator = registerCustomerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterStaffAsync(RegisterStaffCommand command, CancellationToken cancellationToken = default)
    {
        await _registerStaffValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var exists = await _persistence.UserExistsWithEmailAsync(emailNormalized, cancellationToken);
        if (exists)
        {
            throw new ConflictException("user.email.taken", "Email is already registered.");
        }

        var user = User.Create(command.Email, command.FullName, _passwordHasher.Hash(command.Password), command.Role);
        await _persistence.AddUserAsync(user, cancellationToken);
        await _persistence.SaveChangesAsync(cancellationToken);
        await _firestoreSync.UpsertUserAsync(user, cancellationToken);

        return IssueResponse(user);
    }

    public async Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await _registerCustomerValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var exists = await _persistence.UserExistsWithEmailAsync(emailNormalized, cancellationToken);
        if (exists)
        {
            throw new ConflictException("user.email.taken", "Email is already registered.");
        }

        var user = User.Create(command.Email, command.FullName, _passwordHasher.Hash(command.Password), UserRole.Customer);
        await _persistence.AddUserAsync(user, cancellationToken);

        var customer = Customer.Create(command.FullName, command.PhoneNumber, emailNormalized, user.Id);
        await _persistence.AddCustomerAsync(customer, cancellationToken);

        await _persistence.SaveChangesAsync(cancellationToken);
        await _firestoreSync.UpsertUserAsync(user, cancellationToken);
        await _firestoreSync.UpsertCustomerAsync(customer, cancellationToken);

        return IssueResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var emailNormalized = command.Email.Trim().ToLowerInvariant();

        User? user = null;
        if (_firestoreMigration.AuthReadFromFirestore)
        {
            user = await _firestoreUserReader.GetByEmailAsync(emailNormalized, cancellationToken);
        }

        if (user is null)
        {
            user = await _persistence.FindUserByEmailAsync(emailNormalized, cancellationToken);
        }

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
