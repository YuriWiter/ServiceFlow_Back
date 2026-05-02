using FluentValidation;

namespace ServiceFlow.Application.Vehicles;

internal sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.LicensePlate).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.Vin).MaximumLength(20);
    }
}

internal sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Make).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.Vin).MaximumLength(20);
    }
}
