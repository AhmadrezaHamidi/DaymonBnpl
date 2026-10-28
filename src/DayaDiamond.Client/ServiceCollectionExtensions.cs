using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayaDiamond.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// ثبت کلاینت دایادایموند. تنظیمات از بخش «DayaDiamond» کانفیگ خوانده می‌شود.
    /// </summary>
    public static IServiceCollection AddDayaDiamondClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DayaDiamondOptions>(configuration.GetSection(DayaDiamondOptions.SectionName));
        services.AddSingleton<DayaDiamondTokenStore>();
        services.AddHttpClient<IDayaDiamondClient, DayaDiamondClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
        return services;
    }
}
