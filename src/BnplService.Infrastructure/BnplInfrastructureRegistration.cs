using BnplService.Infrastructure.Providers.DayaDiamond;
using DayaDiamond.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BnplService.Infrastructure;

public static class BnplInfrastructureRegistration
{
    /// <summary>
    /// دیتابیس و ارائه‌دهنده‌های دایادایموند.
    /// کانفیگ: ConnectionStrings:Bnpl، Bnpl:Database:Provider (SqlServer پیش‌فرض، یا Sqlite برای اجرای محلی)، بخش DayaDiamond.
    /// برای توسعه بدون سرور واقعی، DayaDiamond:BaseUrl را روی tools/DayaDiamond.Mock بگذارید.
    /// </summary>
    public static IServiceCollection AddBnplInfrastructure(this IServiceCollection services, IConfiguration configuration) => services
        .AddBnplPersistence(configuration)
        .AddBnplDayaDiamondProviders(configuration);

    public static IServiceCollection AddBnplPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Bnpl")
                         ?? throw new InvalidOperationException("ConnectionStrings:Bnpl تنظیم نشده است.");
        var provider = configuration["Bnpl:Database:Provider"] ?? "SqlServer";
        services.AddDbContext<BnplDbContext>(o =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(connection);
            else o.UseSqlServer(connection, BnplModel.SqlServer);
        });
        services.AddScoped<IBnplDbContext>(sp => sp.GetRequiredService<BnplDbContext>());
        return services;
    }

    /// <summary>پورت‌های قیمت، پروفایل، درگاه و حساب توکن روی کلاینت دایادایموند.</summary>
    public static IServiceCollection AddBnplDayaDiamondProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDayaDiamondClient(configuration);
        services.AddScoped<ITokenPriceProvider, DayaDiamondTokenPriceProvider>();
        services.AddScoped<ICustomerProfileProvider, DayaDiamondProfileProvider>();
        services.AddScoped<IBnplPaymentGateway, DayaDiamondPaymentGateway>();
        services.AddScoped<ITokenCustody, DayaDiamondTokenCustody>();
        return services;
    }
}
