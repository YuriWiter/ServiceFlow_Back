using FluentValidation;

namespace ServiceFlow.Application.ServiceOrders;

internal sealed class OpenServiceOrderCommandValidator : AbstractValidator<OpenServiceOrderCommand>
{
    public OpenServiceOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.OpeningDescription).NotEmpty().MaximumLength(2000);
    }
}

internal sealed class TransitionServiceOrderCommandValidator : AbstractValidator<TransitionServiceOrderCommand>
{
    public TransitionServiceOrderCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.NextStage).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal sealed class AssignStaffCommandValidator : AbstractValidator<AssignStaffCommand>
{
    public AssignStaffCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.StaffUserId).NotEmpty();
    }
}

internal sealed class ServiceOrderListFilterValidator : AbstractValidator<ServiceOrderListFilter>
{
    public ServiceOrderListFilterValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
