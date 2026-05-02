using FluentAssertions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Customers;

namespace ServiceFlow.Domain.Tests.Customers;

public sealed class CustomerTests
{
    [Fact]
    public void Phone_is_stripped_to_digits()
    {
        var customer = Customer.Create("Jane Doe", "+1 (415) 555-0100", "jane@example.com");
        customer.PhoneNumber.Should().Be("14155550100");
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    public void Invalid_phone_length_throws(string phone)
    {
        var act = () => Customer.Create("Jane", phone);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(DomainErrors.Customer.PhoneInvalid);
    }

    [Fact]
    public void Email_is_lowercased()
    {
        var customer = Customer.Create("Jane", "14155550100", "Jane@Example.COM");
        customer.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void Missing_name_throws()
    {
        var act = () => Customer.Create(" ", "14155550100");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(DomainErrors.Customer.FullNameRequired);
    }
}
