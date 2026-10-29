using BnplService.Infrastructure.Seed;
using BnplService.Infrastructure.Services.DayaDiamond;
using BnplService.Infrastructure.Services.Sandbox;
using DayaDiamond.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BnplService.Infrastructure;

/// <summary>
/// زیرساخت BNPL (مثل FacilityPanelInfrastructureRegistration). از طریق IoC:  services.AddBnplData(configuration);  services.AddBnplProviders(...);
/// تنظیمات:
///   ConnectionStrings:Bnpl (یا Default)
///   Bnpl:DatabaseProvider   = SqlServer (پیش‌فرض) | Sqlite (فقط اجرای محلی؛ مثلاً "Data Source=bnpl.db")
///   Bnpl:Providers:Price    = DayaDiamond (پیش‌فرض) | Sandbox
///   Bnpl:Providers:Profile  = DayaDiamond (پیش‌فرض) | Sandbox
///   Bnpl:Providers:Gateway  = DayaDiamond (پیش‌فرض) | Sandbox
///   Bnpl:Providers:Custody  = DayaDiamond (پیش‌فرض) | Sandbox
///   Bnpl:Sandbox            = { TokenPriceRial, FreeTokenBalance, ProfileCompleted, PayPageBaseUrl }
/// Sandbox در محیط Production مجاز نیست و برنامه با خطا بالا نمی‌آید.
/// </summary>
public static class BnplInfrastructureRegistration
{
    public const string DayaDiamondProvider = "DayaDiamond";
    public const string SandboxProvider = "Sandbox";

    public static IServiceCollection AddBnplData(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Bnpl") ?? configuration.GetConnectionString("Default")
                         ?? throw new InvalidOperationException("ConnectionStrings:Bnpl تنظیم نشده است.");
        var sqlite = string.Equals(configuration["Bnpl:DatabaseProvider"], "Sqlite", StringComparison.OrdinalIgnoreCase);
        services.AddDbContext<BnplDbContext>(o =>
        {
            if (sqlite) o.UseSqlite(connection);
            else o.UseSqlServer(connection, BnplModel.SqlServer);
        });
        services.AddScoped<IBnplDbContext>(sp => sp.GetRequiredService<BnplDbContext>());
        return services;
    }

    public static IServiceCollection AddBnplProviders(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var price = Provider(configuration, "Price", environment);
        var profile = Provider(configuration, "Profile", environment);
        var gateway = Provider(configuration, "Gateway", environment);
        var custody = Provider(configuration, "Custody", environment);

        if (new[] { price, profile, gateway }.Contains(DayaDiamondProvider)) services.AddDayaDiamondClient(configuration);

        var sandbox = configuration.GetSection(SandboxOptions.SectionName).Get<SandboxOptions>() ?? new SandboxOptions();
        services.AddSingleton(new SandboxStore(sandbox));

        if (price == SandboxProvider) services.AddSingleton<ITokenPriceProvider, SandboxTokenPriceProvider>();
        else services.AddTransient<ITokenPriceProvider, DayaDiamondTokenPriceProvider>();

        if (profile == SandboxProvider) services.AddSingleton<ICustomerProfileProvider, SandboxProfileProvider>();
        else services.AddTransient<ICustomerProfileProvider, DayaDiamondProfileProvider>();

        if (gateway == SandboxProvider)
        {
            var payPage = sandbox.PayPageBaseUrl
                          ?? $"{(configuration["Bnpl:PublicBaseUrl"] ?? "http://localhost:55460").TrimEnd('/')}/v2/bnpl/Sandbox/Pay";
            services.AddSingleton<IBnplPaymentGateway>(sp => new SandboxPaymentGateway(sp.GetRequiredService<SandboxStore>(), payPage));
        }
        else services.AddTransient<IBnplPaymentGateway, DayaDiamondPaymentGateway>();

        if (custody == SandboxProvider) services.AddSingleton<ITokenCustody, SandboxTokenCustody>();
        else services.AddSingleton<ITokenCustody, DayaDiamondTokenCustody>();

        return services;
    }

    /// <summary>درگاه Sandbox فعال است؟ (صفحهٔ پرداخت آزمایشی فقط در این حالت جواب می‌دهد.)</summary>
    public static bool IsSandboxGateway(IConfiguration configuration) =>
        string.Equals(configuration["Bnpl:Providers:Gateway"], SandboxProvider, StringComparison.OrdinalIgnoreCase);

    private static string Provider(IConfiguration configuration, string port, IHostEnvironment environment)
    {
        var value = configuration[$"Bnpl:Providers:{port}"];
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, DayaDiamondProvider, StringComparison.OrdinalIgnoreCase))
            return DayaDiamondProvider;
        if (!string.Equals(value, SandboxProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Bnpl:Providers:{port} باید DayaDiamond یا Sandbox باشد (مقدار فعلی: {value}).");
        if (environment.IsProduction())
            throw new InvalidOperationException($"Bnpl:Providers:{port} = Sandbox در محیط Production مجاز نیست.");
        return SandboxProvider;
    }

    /// <summary>
    /// به‌روزرسانی دیتابیس هنگام شروع برنامه:
    ///   SQL Server ← اعمال Migrationهای باقی‌مانده (جداول + تنظیمات و طرح پیش‌فرض)
    ///   SQLite (فقط اجرای محلی) ← ساخت جداول از مدل و دادهٔ اولیه
    /// اسکریپت همین Migrationها برای DBA: Database/bnpl_migrations.sql
    /// </summary>
    public static async Task MigrateBnplDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BnplDbContext>();
        if (db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync(ct);
            await BnplSeed.EnsureSeedAsync(db, ct);
            return;
        }
        await db.Database.MigrateAsync(ct);
    }
}
