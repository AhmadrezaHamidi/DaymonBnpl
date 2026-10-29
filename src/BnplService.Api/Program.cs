using BnplService.Api;
using BnplService.Api.Authorization;
using BnplService.Api.Swagger;
using BnplService.Api.Workers;
using BnplService.Infrastructure;
using BnplService.IoC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddBnplAuthorization(builder.Configuration);                      // احراز مشتری و پنل از Identity
builder.Services.AddBnplServices(builder.Configuration, builder.Environment);     // IoC: Handlerها، DbContext، دایادایموند/Sandbox
builder.Services.AddBnplErrorHandling();                                           // قالب یکسان و فارسی خطاها
builder.Services.AddBnplSwagger();
if (builder.Configuration.GetValue("Bnpl:Maintenance:Enabled", true))
    builder.Services.AddHostedService<BnplMaintenanceWorker>();                    // کار دوره‌ای: انقضا، استعلام درگاه، قفل توکن

var app = builder.Build();

app.UseBnplErrorHandling();                                                        // باید اولین middleware باشد

if (!app.Environment.IsProduction())
    app.UseBnplSwagger();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Bnpl:ApplyMigrationsOnStartup"))
    await app.Services.MigrateBnplDatabaseAsync();                                 // Migrationها (SQL Server) / ساخت جداول (SQLite محلی)

app.UseRouting();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();
app.Run();

public partial class Program;
