using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static BnplService.Infrastructure.DBContext.BnplModel;

namespace BnplService.Infrastructure.Configuration;

public sealed class BnplRequestConfiguration : IEntityTypeConfiguration<BnplRequest>
{
    public void Configure(EntityTypeBuilder<BnplRequest> e)
    {
        e.ToTable("Requests");
        Versioned(e);
        e.Property(x => x.ContractNumber).HasMaxLength(20).IsUnicode(false).IsRequired();
        e.Property(x => x.CustomerId).HasMaxLength(64).IsUnicode(false).IsRequired();
        e.Property(x => x.MobileNumber).HasMaxLength(11).IsUnicode(false).IsRequired();
        e.Property(x => x.CustomerName).HasMaxLength(150);
        e.Property(x => x.PlanTitle).HasMaxLength(100).IsRequired();
        e.Property(x => x.DiscountCode).HasMaxLength(30).IsUnicode(false);
        e.Property(x => x.CloseReason).HasMaxLength(1000);
        Percent(e.Property(x => x.FeePercent));
        Percent(e.Property(x => x.UpfrontFeePercent));
        Percent(e.Property(x => x.LatePenaltyDailyPercent));
        Tokens(e.Property(x => x.TokenAmount));
        e.HasOne<BnplPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<DiscountCode>().WithMany().HasForeignKey(x => x.DiscountCodeId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.Installments).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.History).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(x => x.IsIncomplete);
        e.Ignore(x => x.NextUnpaid);
        e.Ignore(x => x.PaidCount);
        e.Ignore(x => x.PaidPercent);
        e.Ignore(x => x.OwnedTokenAmount);
        e.HasIndex(x => x.ContractNumber).IsUnique();
        e.HasIndex(x => new { x.CustomerId, x.Status });
        e.HasIndex(x => new { x.Status, x.ExpiresAt });
        e.HasIndex(x => x.MobileNumber);
    }
}

public sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> e)
    {
        e.ToTable("Installments");
        e.Ignore(x => x.IsPaid);
        e.HasIndex(x => new { x.RequestId, x.Number }).IsUnique();
        e.HasIndex(x => new { x.Status, x.DueDate });
    }
}

public sealed class BnplRequestHistoryConfiguration : IEntityTypeConfiguration<BnplRequestHistory>
{
    public void Configure(EntityTypeBuilder<BnplRequestHistory> e)
    {
        e.ToTable("RequestHistory");
        ActorColumn(e.Property(x => x.By)).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1200);
    }
}

public sealed class BnplPaymentConfiguration : IEntityTypeConfiguration<BnplPayment>
{
    public void Configure(EntityTypeBuilder<BnplPayment> e)
    {
        e.ToTable("Payments");
        e.Property(x => x.ContractNumber).HasMaxLength(20).IsUnicode(false).IsRequired();
        e.Property(x => x.CustomerId).HasMaxLength(64).IsUnicode(false).IsRequired();
        e.Property(x => x.GatewayTitle).HasMaxLength(100);
        e.Property(x => x.ProviderReference).HasMaxLength(100).IsUnicode(false);
        e.Property(x => x.PayUrl).HasMaxLength(1000).IsUnicode(false);
        e.Property(x => x.ReturnUrl).HasMaxLength(500).IsUnicode(false);
        e.Property(x => x.TrackingNumber).HasMaxLength(100).IsUnicode(false);
        e.Property(x => x.RefundTrackingNumber).HasMaxLength(100).IsUnicode(false);
        e.Property(x => x.FailureReason).HasMaxLength(500);
        e.Property(x => x.Description).HasMaxLength(300).IsRequired();
        Tokens(e.Property(x => x.TokenAmount));
        ActorColumn(e.Property(x => x.By)).IsRequired();
        ActorColumn(e.Property(x => x.RefundedBy));
        e.Ignore(x => x.Total);
        e.Ignore(x => x.IsPending);
        e.HasOne<BnplRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.RequestId, x.Status });
        e.HasIndex(x => new { x.CustomerId, x.Id });
        e.HasIndex(x => new { x.Status, x.ExpiresAt });
        e.HasIndex(x => x.ProviderReference);
    }
}
