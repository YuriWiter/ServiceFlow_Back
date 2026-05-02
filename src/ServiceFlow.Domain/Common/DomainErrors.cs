namespace ServiceFlow.Domain.Common;

/// <summary>
/// Stable error codes returned by <see cref="DomainException"/>. Clients can map these to i18n keys.
/// </summary>
public static class DomainErrors
{
    public static class User
    {
        public const string EmailRequired = "user.email.required";
        public const string EmailTooLong = "user.email.too_long";
        public const string EmailInvalid = "user.email.invalid";
        public const string FullNameRequired = "user.full_name.required";
        public const string FullNameTooLong = "user.full_name.too_long";
        public const string PasswordHashRequired = "user.password_hash.required";
    }

    public static class Customer
    {
        public const string FullNameRequired = "customer.full_name.required";
        public const string FullNameTooLong = "customer.full_name.too_long";
        public const string EmailInvalid = "customer.email.invalid";
        public const string PhoneInvalid = "customer.phone.invalid";
    }

    public static class Vehicle
    {
        public const string MakeRequired = "vehicle.make.required";
        public const string ModelRequired = "vehicle.model.required";
        public const string YearOutOfRange = "vehicle.year.out_of_range";
        public const string LicensePlateRequired = "vehicle.license_plate.required";
        public const string LicensePlateInvalid = "vehicle.license_plate.invalid";
    }

    public static class ServiceOrder
    {
        public const string OpeningDescriptionRequired = "service_order.opening_description.required";
        public const string OpeningDescriptionTooLong = "service_order.opening_description.too_long";
        public const string InvalidStageTransition = "service_order.invalid_stage_transition";
        public const string AlreadyCompleted = "service_order.already_completed";
        public const string CannotTransitionBeforeApproval = "service_order.cannot_transition_before_approval";
    }

    public static class RepairRequest
    {
        public const string IssueDescriptionRequired = "repair_request.issue_description.required";
        public const string PriceEstimateNegative = "repair_request.price_estimate.negative";
        public const string AlreadyDecided = "repair_request.already_decided";
        public const string MediaRequired = "repair_request.media.required";
        public const string InvalidMediaPath = "repair_request.media.invalid_path";
    }
}
