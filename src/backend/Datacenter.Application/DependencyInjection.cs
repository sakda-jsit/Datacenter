using Datacenter.Application.Common.Behaviours;
using Datacenter.Application.Common.Security;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Datacenter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ICompanyAccessGuard, CompanyAccessGuard>();

        // ลำดับสำคัญ: validate ก่อน แล้วจึงตรวจสิทธิ์เข้าถึงบริษัท
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CompanyAccessBehaviour<,>));

        return services;
    }
}
