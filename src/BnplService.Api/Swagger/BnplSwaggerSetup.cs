using Microsoft.OpenApi.Models;

namespace BnplService.Api.Swagger;

/// <summary>Swagger سرویس BNPL. Program.cs:  builder.Services.AddBnplSwagger();  …  app.UseBnplSwagger();</summary>
public static class BnplSwaggerSetup
{
    public const string DocumentName = "v2";

    private const string Summary = """
        پیش‌خرید اقساطی توکن الماس (BNPL).
        - مسیرهای «مشتری» با هدرهای Token و MobileNumber اپ (یا Authorization: Bearer) و Identity احراز می‌شوند.
        - مسیرهای «پنل» با کوکی/Bearer پنل و کلیدهای مجوز bnpl.* از Identity.
        - همهٔ پاسخ‌ها: { statusCode, message, data }؛ خطای قاعده با پیام فارسی در message.
        """;

    public static IServiceCollection AddBnplSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(DocumentName, new OpenApiInfo { Title = "BNPL API — پیش‌خرید اقساطی توکن الماس", Version = DocumentName, Description = Summary });
            c.EnableAnnotations();
            c.SupportNonNullableReferenceTypes();
            c.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
            c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });

            c.AddSecurityDefinition("customerToken", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "Token", Description = "توکن فعال اپ مشتری.",
            });
            c.AddSecurityDefinition("customerMobile", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "MobileNumber", Description = "موبایل مشتری.",
            });
            c.AddSecurityDefinition("panel", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", Description = "توکن پنل (یا کوکی token از Admin.Api).",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [Ref("customerToken")] = [], [Ref("customerMobile")] = [],
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { [Ref("panel")] = [] });

            foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
                if (File.Exists(Path.ChangeExtension(xml, ".dll")))
                    c.IncludeXmlComments(xml, includeControllerXmlComments: true);
        });
        return services;
    }

    public static WebApplication UseBnplSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint($"/swagger/{DocumentName}/swagger.json", "BNPL v2");
            o.DocumentTitle = "BNPL API";
            o.DisplayRequestDuration();
        });
        return app;
    }

    private static OpenApiSecurityScheme Ref(string id) =>
        new() { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = id } };
}
