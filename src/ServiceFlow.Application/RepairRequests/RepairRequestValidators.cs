using FluentValidation;

namespace ServiceFlow.Application.RepairRequests;

internal sealed class CreateRepairRequestCommandValidator : AbstractValidator<CreateRepairRequestCommand>
{
    public CreateRepairRequestCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.IssueDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PriceEstimateCents).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Urgency).IsInEnum();
    }
}

internal sealed class UpdateRepairEstimateCommandValidator : AbstractValidator<UpdateRepairEstimateCommand>
{
    public UpdateRepairEstimateCommandValidator()
    {
        RuleFor(x => x.RepairRequestId).NotEmpty();
        RuleFor(x => x.IssueDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PriceEstimateCents).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Urgency).IsInEnum();
    }
}

internal sealed class AttachRepairMediaCommandValidator : AbstractValidator<AttachRepairMediaCommand>
{
    public AttachRepairMediaCommandValidator()
    {
        RuleFor(x => x.RepairRequestId).NotEmpty();
        RuleFor(x => x.MediaType).IsInEnum();
        RuleFor(x => x.StoragePath).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MimeType).MaximumLength(100);
        RuleFor(x => x.SizeBytes).GreaterThanOrEqualTo(0);
    }
}

internal sealed class RegisterCustomerDecisionCommandValidator : AbstractValidator<RegisterCustomerDecisionCommand>
{
    public RegisterCustomerDecisionCommandValidator()
    {
        RuleFor(x => x.RepairRequestId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
