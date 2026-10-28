using Microsoft.Extensions.DependencyInjection;

namespace BnplService.Application.Handler.Shared;

/// <summary>ثبت لایهٔ Application. Program.cs (از طریق IoC):  services.AddBnplApplication();</summary>
public static class BnplApplicationRegistration
{
    public static IServiceCollection AddBnplApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<BnplHandlerBase>();
            cfg.AddOpenBehavior(typeof(BnplExceptionBehavior<,>));
        });
        services.TryAddSingletonTimeProvider();
        services.AddScoped<BnplPaymentProcessor>();
        services.AddScoped<BnplGatewayVerifier>();
        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.All(d => d.ServiceType != typeof(TimeProvider))) services.AddSingleton(TimeProvider.System);
    }
}
