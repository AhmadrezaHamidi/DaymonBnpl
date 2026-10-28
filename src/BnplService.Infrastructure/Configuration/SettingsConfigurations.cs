using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static BnplService.Infrastructure.DBContext.BnplModel;

namespace BnplService.Infrastructure.Configuration;

public sealed class BnplSettingsConfiguration : IEntityTypeConfiguration<BnplSettings>
{
    public void Configure(EntityTypeBuilder<BnplSettings> e)
    {
        e.ToTable("Settings");
        Versioned(e);
        e.Property(x => x.Id).ValueGeneratedNever();
        ActorColumn(e.Property(x => x.UpdatedBy));
    }
}

public sealed class BnplPlanConfiguration : IEntityTypeConfiguration<BnplPlan>
{
    public void Configure(EntityTypeBuilder<BnplPlan> e)
    {
        e.ToTable("Plans");
        Versioned(e);
        e.Property(x => x.Title).HasMaxLength(100).IsRequired();
        Percent(e.Property(x => x.FeePercent));
        Percent(e.Property(x => x.UpfrontFeePercent));
        Percent(e.Property(x => x.LatePenaltyDailyPercent));
    }
}

public sealed class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> e)
    {
        e.ToTable("DiscountCodes");
        Versioned(e);
        e.Property(x => x.Code).HasMaxLength(30).IsUnicode(false).IsRequired();
        e.Property(x => x.Title).HasMaxLength(100).IsRequired();
        Percent(e.Property(x => x.FeePercent));
        e.HasIndex(x => x.Code).IsUnique();
    }
}
