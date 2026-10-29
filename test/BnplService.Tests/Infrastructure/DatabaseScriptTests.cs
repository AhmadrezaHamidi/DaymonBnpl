using BnplService.Infrastructure.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BnplService.Tests.Infrastructure;

/// <summary>
/// Migrationها با مدل همگام‌اند و اسکریپت DBA (Database/bnpl_migrations.sql) همان خروجی Migrationهاست.
/// بعد از هر Migration تازه:  UPDATE_SQL=1 dotnet test --filter DatabaseScriptTests
/// </summary>
public class DatabaseScriptTests
{
    private const string Header = "-- جداول و دادهٔ اولیهٔ BNPL — اسکریپت idempotent همهٔ Migrationها؛ اجرای دوباره بی‌اثر است.\n"
                                  + "-- تولید: UPDATE_SQL=1 dotnet test --filter DatabaseScriptTests  (دستی ویرایش نکن)\n\n";

    private static BnplDbContext SqlServerContext() =>
        new(new DbContextOptionsBuilder<BnplDbContext>()
            .UseSqlServer("Server=unused;Database=Bnpl;TrustServerCertificate=True", BnplModel.SqlServer).Options);

    internal static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DaymonBnpl.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine([dir!.FullName, .. parts]);
    }

    [Fact]
    public void Model_has_no_changes_missing_from_the_migrations()
    {
        using var db = SqlServerContext();
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);
        var differ = db.GetService<IMigrationsModelDiffer>();
        var runtime = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot!.Model);
        var differences = differ.GetDifferences(runtime.GetRelationalModel(), db.GetService<IDesignTimeModel>().Model.GetRelationalModel());
        Assert.True(differences.Count == 0,
            "مدل با Migrationها نمی‌خواند؛ dotnet ef migrations add <Name> -p src/BnplService.Infrastructure بزن: "
            + string.Join(", ", differences.Select(d => d.GetType().Name)));
    }

    [Fact]
    public void Migration_script_is_up_to_date()
    {
        using var db = SqlServerContext();
        var script = Header + db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent).ReplaceLineEndings("\n");
        var path = RepoFile("Database", "bnpl_migrations.sql");
        if (Environment.GetEnvironmentVariable("UPDATE_SQL") == "1") File.WriteAllText(path, script);
        Assert.True(File.Exists(path), "Database/bnpl_migrations.sql نیست؛ UPDATE_SQL=1 بزن.");
        Assert.True(File.ReadAllText(path).ReplaceLineEndings("\n") == script, "اسکریپت Migration کهنه است؛ UPDATE_SQL=1 dotnet test --filter DatabaseScriptTests");
    }

    [Fact]
    public void Panel_permission_script_creates_every_bnpl_key()
    {
        var sql = File.ReadAllText(RepoFile("Database", "identity", "bnpl_panel_permissions.sql"));
        foreach (var key in BnplPermissionKeys.All) Assert.Contains($"('{key}',", sql);
    }
}
