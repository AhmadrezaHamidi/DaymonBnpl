using BnplService.Contracts.Common;
using BnplService.Contracts.Settings;

namespace BnplService.Contracts.Requests;

/// <summary>صفحهٔ «درخواست جدید»: بازهٔ اسلایدر، طرح‌ها و قیمت لحظه‌ای توکن.</summary>
public sealed record BnplConfigDto(bool IsEnabled, long MinAmount, long MaxAmount, long AmountStep, int ActivationMinutes,
    IReadOnlyList<PlanDto> Plans, string AssetCode, long TokenPrice);

/// <summary>کارت‌های «مبلغ هر قسط / تعداد توکن» و «شرایط وام».</summary>
public sealed record QuoteDto(
    long Amount, int InstallmentCount, decimal FeePercent, decimal UpfrontFeePercent, decimal LatePenaltyDailyPercent,
    long InstallmentAmount, long FirstInstallmentAmount, long LastInstallmentAmount, long UpfrontFee, long TotalFee,
    long DiscountAmount, long TotalRepayment, long TokenPrice, decimal TokenAmount)
{
    /// <summary>«تاریخ اولین قسط: زمان عقد قرارداد».</summary>
    public string FirstInstallmentDate => "زمان عقد قرارداد";

    public static QuoteDto Of(BnplQuote q) => new(q.Amount, q.InstallmentCount, q.FeePercent, q.UpfrontFeePercent, q.LatePenaltyDailyPercent,
        q.InstallmentAmount, q.FirstInstallmentAmount, q.LastInstallmentAmount, q.UpfrontFee, q.TotalFee, q.DiscountAmount,
        q.TotalRepayment, q.TokenPrice, q.TokenAmount);
}

public sealed record NextInstallmentDto(int Number, string Title, long Amount, long Penalty, long Total, DateOnly DueDate, string DueDateJalali,
    bool IsOverdue, int DaysLate);

/// <summary>کارت قرارداد در «تاریخچه درخواست‌ها» و درخواست تکمیل‌نشده.</summary>
public sealed record BnplRequestListItemDto(
    long Id, string ContractNumber, BnplRequestStatus Status, string StatusTitle, string PlanTitle, long Amount, int InstallmentCount,
    int PaidCount, decimal FeePercent, long InstallmentAmount, decimal TokenAmount, DateTime CreatedAt, DateTime? ActivatedAt,
    DateOnly? EndDate, string? EndDateJalali, DateTime? Deadline, NextInstallmentDto? NextInstallment, bool IsOverdue,
    string RowVersion);

public sealed record InstallmentDto(
    int Number, string Title, DateOnly DueDate, string DueDateJalali, long Amount, InstallmentStatus Status, string StatusTitle,
    long Penalty, bool PenaltyWaived, long PaidAmount, long Remaining, DateTime? PaidAt, bool IsOverdue, int DaysLate);

public sealed record HistoryDto(BnplRequestStatus? FromStatus, BnplRequestStatus ToStatus, string ToStatusTitle, DateTime At,
    ActorDto By, string? Description);

/// <summary>صفحهٔ «جزئیات»: خلاصهٔ قرارداد + «لیست اقساط».</summary>
public sealed record BnplRequestDetailsDto(
    BnplRequestListItemDto Summary,
    string MobileNumber,
    string? CustomerName,
    decimal UpfrontFeePercent,
    decimal LatePenaltyDailyPercent,
    long UpfrontFee,
    long TotalFee,
    long TotalRepayment,
    string? DiscountCode,
    long DiscountAmount,
    long TokenPrice,
    bool IsTokenAmountFinal,
    decimal OwnedTokenAmount,
    long UnpaidDebt,
    int PaidPercent,
    string? CloseReason,
    IReadOnlyList<InstallmentDto> Installments,
    IReadOnlyList<HistoryDto> History);
