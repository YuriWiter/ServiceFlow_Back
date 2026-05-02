using FluentAssertions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_lowercases_email_and_sets_active()
    {
        var user = User.Create("Someone@Example.COM", "Some One", "hash", UserRole.Staff);

        user.Email.Should().Be("someone@example.com");
        user.IsActive.Should().BeTrue();
        user.Role.Should().Be(UserRole.Staff);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Create_rejects_invalid_email(string email)
    {
        var act = () => User.Create(email, "Some One", "hash", UserRole.Staff);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivate_sets_flag_false()
    {
        var user = User.Create("someone@example.com", "Some One", "hash", UserRole.Staff);
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }
}
