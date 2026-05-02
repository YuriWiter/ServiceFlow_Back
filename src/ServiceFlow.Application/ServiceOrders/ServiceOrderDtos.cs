using ServiceFlow.Application.Common;
using ServiceFlow.Application.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Application.ServiceOrders;

public sealed record OpenServiceOrderCommand(
    Guid CustomerId,
    Guid VehicleId,
    string OpeningDescription,
    Guid? AssignedStaffId);

public sealed record TransitionServiceOrderCommand(Guid ServiceOrderId, ServiceStage NextStage, string? Note);

public sealed record AssignStaffCommand(Guid ServiceOrderId, Guid StaffUserId);

public sealed record ServiceOrderSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleLabel,
    Guid? AssignedStaffId,
    ServiceStage Stage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record ServiceOrderDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleLabel,
    Guid? AssignedStaffId,
    string? AssignedStaffName,
    ServiceStage Stage,
    string OpeningDescription,
    IReadOnlyList<ServiceStatusHistoryDto> History,
    IReadOnlyList<RepairRequestDto> Repairs,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record ServiceStatusHistoryDto(
    Guid Id,
    ServiceStage? FromStage,
    ServiceStage ToStage,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAt,
    string? Note);

public sealed record ServiceOrderListFilter(
    ServiceStage? Stage,
    Guid? CustomerId,
    Guid? AssignedStaffId,
    string? Search,
    int Page = 1,
    int PageSize = 20);
