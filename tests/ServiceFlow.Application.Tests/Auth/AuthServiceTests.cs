using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Auth;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Application.Tests.Fakes;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Tests.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_customer_creates_user_and_customer_and_returns_token()
    {
        using var fixture = new InMemoryAppFixture();
        var service = new AuthService(
            fixture.Db,
            fixture.PasswordHasher,
            fixture.JwtService,
            fixture.Validator<RegisterStaffCommand>(),
            fixture.Validator<RegisterCustomerCommand>(),
            fixture.Validator<LoginCommand>());

        var response = await service.RegisterCustomerAsync(new RegisterCustomerCommand(
            "jane@example.com",
            "Jane Doe",
            "StrongP@ss1",
            "+1 415 555 0100"));

        response.Role.Should().Be(UserRole.Customer);
        response.AccessToken.Should().NotBeNullOrWhiteSpace();

        (await fixture.Db.Users.CountAsync()).Should().Be(1);
        (await fixture.Db.Customers.CountAsync()).Should().Be(1);
        (await fixture.Db.Customers.FirstAsync()).UserId.Should().Be(response.UserId);
    }

    [Fact]
    public async Task Login_rejects_invalid_credentials()
    {
        using var fixture = new InMemoryAppFixture();
        var service = new AuthService(
            fixture.Db,
            fixture.PasswordHasher,
            fixture.JwtService,
            fixture.Validator<RegisterStaffCommand>(),
            fixture.Validator<RegisterCustomerCommand>(),
            fixture.Validator<LoginCommand>());

        await service.RegisterCustomerAsync(new RegisterCustomerCommand(
            "jane@example.com",
            "Jane Doe",
            "StrongP@ss1",
            "+1 415 555 0100"));

        var act = () => service.LoginAsync(new LoginCommand("jane@example.com", "wrong"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Register_customer_blocks_duplicate_email()
    {
        using var fixture = new InMemoryAppFixture();
        var service = new AuthService(
            fixture.Db,
            fixture.PasswordHasher,
            fixture.JwtService,
            fixture.Validator<RegisterStaffCommand>(),
            fixture.Validator<RegisterCustomerCommand>(),
            fixture.Validator<LoginCommand>());

        var command = new RegisterCustomerCommand("jane@example.com", "Jane Doe", "StrongP@ss1", "14155550100");
        await service.RegisterCustomerAsync(command);

        var act = () => service.RegisterCustomerAsync(command);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Register_customer_requires_valid_password_length()
    {
        using var fixture = new InMemoryAppFixture();
        var service = new AuthService(
            fixture.Db,
            fixture.PasswordHasher,
            fixture.JwtService,
            fixture.Validator<RegisterStaffCommand>(),
            fixture.Validator<RegisterCustomerCommand>(),
            fixture.Validator<LoginCommand>());

        var command = new RegisterCustomerCommand("jane@example.com", "Jane", "short", "14155550100");

        var act = () => service.RegisterCustomerAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }
}
