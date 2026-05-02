using ServiceFlow.Application.RepairRequests;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Application.ServiceOrders;

internal static class ServiceOrderMapper
{
    public static ServiceOrderSummaryDto ToSummary(ServiceOrder order, Customer customer, Vehicle vehicle)
    {
        return new ServiceOrderSummaryDto(
            order.Id,
            customer.Id,
            customer.FullName,
            vehicle.Id,
            FormatVehicle(vehicle),
            order.AssignedStaffId,
            order.Stage,
            order.CreatedAt,
            order.UpdatedAt,
            order.CompletedAt);
    }

    public static ServiceOrderDetailDto ToDetail(
        ServiceOrder order,
        Customer customer,
        Vehicle vehicle,
        User? staff,
        IEnumerable<ServiceStatusHistoryEntry> history,
        IEnumerable<RepairRequest> repairs)
    {
        return new ServiceOrderDetailDto(
            order.Id,
            customer.Id,
            customer.FullName,
            vehicle.Id,
            FormatVehicle(vehicle),
            order.AssignedStaffId,
            staff?.FullName,
            order.Stage,
            order.OpeningDescription,
            history
                .OrderBy(h => h.ChangedAt)
                .Select(h => new ServiceStatusHistoryDto(h.Id, h.FromStage, h.ToStage, h.ChangedByUserId, h.ChangedAt, h.Note))
                .ToList(),
            repairs
                .OrderByDescending(r => r.CreatedAt)
                .Select(RepairRequestMapper.ToDto)
                .ToList(),
            order.CreatedAt,
            order.UpdatedAt,
            order.CompletedAt);
    }

    internal static string FormatVehicle(Vehicle v) => $"{v.Year} {v.Make} {v.Model}";
}
