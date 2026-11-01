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
        Percent(e.Property(x => x.CollateralPercent));
        e.Property(x => x.SuggestedAmounts).HasMaxLength(250).IsUnicode(false).IsRequired();
        e.Ignore(x => x.SuggestedAmountList);
    }
}

public sealed class PageMessageConfiguration : IEntityTypeConfiguration<PageMessage>
{
    public void Configure(EntityTypeBuilder<PageMessage> e)
    {
        e.ToTable("PageMessages");
        Versioned(e);
        e.Property(x => x.PageKey).HasMaxLength(60).IsUnicode(false).IsRequired();
        e.Property(x => x.Text).HasMaxLength(500).IsRequired();
        e.Property(x => x.Color).HasMaxLength(7).IsUnicode(false);
        e.HasIndex(x => new { x.PageKey, x.IsActive });
    }
}

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> e)
    {
        e.ToTable("CustomerProfiles");
        e.HasKey(x => x.CustomerId);
        Versioned(e);
        e.Property(x => x.CustomerId).HasMaxLength(64).IsUnicode(false);
        e.Property(x => x.MobileNumber).HasMaxLength(11).IsUnicode(false).IsRequired();
        e.Property(x => x.FirstName).HasMaxLength(50);
        e.Property(x => x.LastName).HasMaxLength(50);
        e.Property(x => x.FatherName).HasMaxLength(50);
        e.Property(x => x.NationalCode).HasMaxLength(10).IsUnicode(false);
        e.Property(x => x.PersianBirthDate).HasMaxLength(10).IsUnicode(false);
        e.Property(x => x.Gender).HasMaxLength(10).IsUnicode(false);
        e.Property(x => x.Sheba).HasMaxLength(24).IsUnicode(false);
        e.Property(x => x.Province).HasMaxLength(50);
        e.Property(x => x.City).HasMaxLength(50);
        e.Property(x => x.Street).HasMaxLength(260);
        e.Property(x => x.PostalCode).HasMaxLength(10).IsUnicode(false);
        e.Property(x => x.TelephoneNumber).HasMaxLength(11).IsUnicode(false);
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
