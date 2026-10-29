namespace BnplService.Api.Authorization;

/// <summary>
/// احراز هویت مشتری و پنل از Identity. Program.cs:  builder.Services.AddBnplAuthorization(builder.Configuration);
/// تنظیمات: Identity:BaseUrl (آدرس IdentityService / Admin.Api) و InternalApi:Key (همان کلید دو طرف).
/// </summary>
public static class BnplAuthorizationRegistration
{
    public static IServiceCollection AddBnplAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IBnplCurrentUser, BnplCurrentUserAccessor>();
        services.AddScoped<IBnplCurrentCustomer, BnplCurrentCustomerAccessor>();
        services.AddHttpClient<IIdentityAuthorizationClient, IdentityAuthorizationClient>(client => Configure(client, configuration, "Identity:PanelBaseUrl"));
        services.AddHttpClient<IIdentityCustomerClient, IdentityCustomerClient>(client => Configure(client, configuration, "Identity:CustomerBaseUrl"));
        return services;
    }

    /// <summary>پنل و مشتری می‌توانند دو آدرس جدا داشته باشند (Admin.Api و IdentityService)؛ پیش‌فرض هر دو Identity:BaseUrl.</summary>
    private static void Configure(HttpClient client, IConfiguration configuration, string specificKey)
    {
        var baseUrl = configuration[specificKey] ?? configuration["Identity:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        var key = configuration["InternalApi:Key"];
        if (!string.IsNullOrWhiteSpace(key)) client.DefaultRequestHeaders.Add(configuration["InternalApi:HeaderName"] ?? "X-Api-Key", key);
        client.Timeout = TimeSpan.FromSeconds(5);
    }
}
