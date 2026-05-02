using System.Text.RegularExpressions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Domain.Customers;

public sealed partial class Customer : AuditableEntity
{
    private readonly List<Vehicle> _vehicles = [];

    public string FullName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string PhoneNumber { get; private set; } = null!;

    /// <summary>
    /// Optional link to the <see cref="Users.User"/> account used for the customer portal.
    /// Kept optional so offline customers can be registered by staff.
    /// </summary>
    public Guid? UserId { get; private set; }

    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    private Customer() { }

    public static Customer Create(string fullName, string phoneNumber, string? email = null, Guid? userId = null)
    {
        fullName = Guard.NotNullOrWhiteSpace(fullName, DomainErrors.Customer.FullNameRequired);
        Guard.MaxLength(fullName, 200, DomainErrors.Customer.FullNameTooLong);

        phoneNumber = NormalizePhone(phoneNumber);
        email = NormalizeEmail(email);

        return new Customer
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email,
            UserId = userId
        };
    }

    public void UpdateContact(string fullName, string phoneNumber, string? email)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, DomainErrors.Customer.FullNameRequired);
        Guard.MaxLength(FullName, 200, DomainErrors.Customer.FullNameTooLong);

        PhoneNumber = NormalizePhone(phoneNumber);
        Email = NormalizeEmail(email);
        Touch();
    }

    public void LinkToUser(Guid userId)
    {
        UserId = userId;
        Touch();
    }

    private static string NormalizePhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException(DomainErrors.Customer.PhoneInvalid, "Phone number is required.");
        }

        var digits = DigitsOnly().Replace(phoneNumber, string.Empty);
        if (digits.Length is < 8 or > 15)
        {
            throw new DomainException(DomainErrors.Customer.PhoneInvalid, "Phone number must contain 8-15 digits.");
        }

        return digits;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        email = email.Trim().ToLowerInvariant();
        if (!email.Contains('@') || email.Length < 5 || email.Length > 255)
        {
            throw new DomainException(DomainErrors.Customer.EmailInvalid, "Invalid email format.");
        }

        return email;
    }

    [GeneratedRegex(@"\D+")]
    private static partial Regex DigitsOnly();
}
