namespace BnplService.Application.Handler.Shared;

/// <summary>تبدیل Entity به DTOهای صفحه‌های فیگما.</summary>
public static class BnplMappings
{
    public static BnplRequestListItemDto ToListItem(this BnplRequest r, DateOnly today)
    {
        var next = r.Status is BnplRequestStatus.Active or BnplRequestStatus.Signed ? r.NextUnpaid : null;
        var last = r.ActivatedAt is null ? null : r.Installments.MaxBy(i => i.Number);
        DateTime? deadline = r.Status switch
        {
            BnplRequestStatus.Draft or BnplRequestStatus.TermsAccepted => r.ExpiresAt,
            BnplRequestStatus.Signed => r.ActivationDeadline,
            _ => null,
        };
        return new BnplRequestListItemDto(
            r.Id, r.ContractNumber, r.Status, BnplTitles.Of(r.Status), r.PlanTitle, r.Amount, r.InstallmentCount, r.PaidCount, r.FeePercent,
            r.InstallmentAmount, r.TokenAmount, r.CreatedAt, r.ActivatedAt, last?.DueDate, last is null ? null : Jalali.Format(last.DueDate),
            deadline, next is null ? null : r.ToNext(next, today), r.IsOverdue(today), r.RowVersion);
    }

    private static NextInstallmentDto ToNext(this BnplRequest r, Installment i, DateOnly today)
    {
        var penalty = r.PenaltyOf(i, today);
        return new NextInstallmentDto(i.Number, BnplTitles.Installment(i.Number), i.Amount, penalty, i.Amount + penalty, i.DueDate,
            Jalali.Format(i.DueDate), r.Status == BnplRequestStatus.Active && i.DueDate < today,
            r.Status == BnplRequestStatus.Active ? LatePenalty.DaysLate(i.DueDate, today) : 0);
    }

    public static InstallmentDto ToDto(this BnplRequest r, Installment i, DateOnly today)
    {
        var penalty = r.PenaltyOf(i, today);
        var active = r.Status == BnplRequestStatus.Active && !i.IsPaid;
        return new InstallmentDto(i.Number, BnplTitles.Installment(i.Number), i.DueDate, Jalali.Format(i.DueDate), i.Amount, i.Status,
            BnplTitles.Of(i.Status), penalty, i.PenaltyWaived, i.IsPaid ? i.Amount + i.PenaltyPaid : 0,
            i.IsPaid ? 0 : i.Amount + penalty, i.PaidAt, active && i.DueDate < today,
            active ? LatePenalty.DaysLate(i.DueDate, today) : 0);
    }

    public static BnplRequestDetailsDto ToDetails(this BnplRequest r, DateOnly today) => new(
        r.ToListItem(today), r.MobileNumber, r.CustomerName, r.UpfrontFeePercent, r.LatePenaltyDailyPercent, r.UpfrontFee, r.TotalFee,
        r.TotalRepayment, r.DiscountCode, r.DiscountAmount, r.TokenPrice, r.ActivatedAt is not null, r.OwnedTokenAmount, r.UnpaidDebt(today),
        r.PaidPercent, r.CloseReason,
        r.Installments.OrderBy(i => i.Number).Select(i => r.ToDto(i, today)).ToList(),
        r.History.OrderBy(h => h.At).ThenBy(h => h.Id)
            .Select(h => new HistoryDto(h.FromStatus, h.ToStatus, BnplTitles.Of(h.ToStatus), h.At, ActorDto.Of(h.By), h.Description)).ToList());
}
