using BnplService.Application.Common;
using BnplService.Application.Handler.Shared;
using BnplService.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BnplService.IoC;

/// <summary>
/// ثبت همهٔ وابستگی‌های BNPL (مثل FacilityService.IoC.DependencyContainer).
/// Program.cs:  builder.Services.AddBnplServices(builder.Configuration, builder.Environment);
/// احراز هویت مشتری و پنل از Identity (AddBnplAuthorization)، Worker و Swagger در پروژهٔ Api می‌مانند.
/// </summary>
public static class DependencyContainer
{
    public static IServiceCollection AddBnplServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<BnplOptions>(configuration.GetSection(BnplOptions.SectionName));
        services.AddBnplApplication();                          // MediatR + BnplExceptionBehavior + TimeProvider + پردازشگر پرداخت
        services.AddBnplData(configuration);                    // DbContext (SqlServer / Sqlite محلی)
        services.AddBnplProviders(configuration, environment);  // دایادایموند یا Sandbox برای قیمت، پروفایل، درگاه و نگهداری توکن
        return services;
    }
}
