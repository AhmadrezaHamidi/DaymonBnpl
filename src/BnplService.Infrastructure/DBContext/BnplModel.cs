using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BnplService.Infrastructure.DBContext;

/// <summary>مدل EF: تنظیم هر Entity در Configuration/<Entity>Configuration.cs، همه در اسکیمای «bnpl».</summary>
public static class BnplModel
{
    public const string Schema = "bnpl";

    /// <summary>تنظیم مشترک SQL Server (برنامه و dotnet ef): جدول تاریخچهٔ Migrationها در همان اسکیما.</summary>
    public static void SqlServer(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sql) => sql
        .MigrationsHistoryTable("__EFMigrationsHistory", Schema)
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly ValueConverter<Actor, string> ActorConverter = new(
        v => JsonSerializer.Serialize(v, Json),
        v => JsonSerializer.Deserialize<Actor>(v, Json)!);

    public static void Configure(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);
        b.ApplyConfigurationsFromAssembly(typeof(BnplModel).Assembly,
            t => t.Namespace != null && t.Namespace.StartsWith("BnplService.Infrastructure.Configuration"));
    }

    internal static void Versioned<T>(EntityTypeBuilder<T> e) where T : VersionedEntity
    {
        e.Property(x => x.Version).IsConcurrencyToken();
        e.Ignore(x => x.RowVersion);
    }

    internal static PropertyBuilder<Actor> ActorColumn(PropertyBuilder<Actor> p) =>
        p.HasConversion(ActorConverter).HasMaxLength(300);

    /// <summary>درصدها: decimal(7,4)؛ تعداد توکن: decimal(18,3).</summary>
    internal static void Percent(PropertyBuilder<decimal> p) => p.HasPrecision(7, 4);

    internal static void Percent(PropertyBuilder<decimal?> p) => p.HasPrecision(7, 4);

    internal static void Tokens(PropertyBuilder<decimal> p) => p.HasPrecision(18, 3);

    internal static void Tokens(PropertyBuilder<decimal?> p) => p.HasPrecision(18, 3);
}
