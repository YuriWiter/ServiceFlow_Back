using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceFlow.Application.Auth;
using ServiceFlow.Application.Customers;
using ServiceFlow.Application.RepairRequests;
using ServiceFlow.Application.ServiceOrders;
using ServiceFlow.Application.Vehicles;

namespace ServiceFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IServiceOrderService, ServiceOrderService>();
        services.AddScoped<IRepairRequestService, RepairRequestService>();

        return services;
    }
}
