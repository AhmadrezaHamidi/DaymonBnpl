namespace BnplService.Domain.Entity.Requests;

/// <summary>یک قسط. پرداخت جزئی نداریم: هر قسط یک‌جا (به‌علاوهٔ جریمهٔ روزهای دیرکرد) پرداخت می‌شود.</summary>
public class Installment
{
    public long Id { get; private set; }
    public long RequestId { get; private set; }
    public int Number { get; private set; }

    /// <summary>تا فعال شدن قرارداد موقت است؛ با پرداخت قسط اول از همان روز دوباره چیده می‌شود.</summary>
    public DateOnly DueDate { get; private set; }

    /// <summary>مبلغ قسط بدون جریمه (قسط اول کارمزد قسط اول را دارد، قسط آخر تخفیف را کم دارد).</summary>
    public long Amount { get; private set; }

    public InstallmentStatus Status { get; private set; } = InstallmentStatus.Unpaid;
    public DateTime? PaidAt { get; private set; }
    public long? PaymentId { get; private set; }

    /// <summary>جریمه‌ای که هنگام پرداخت گرفته شد (ثابت می‌ماند).</summary>
    public long PenaltyPaid { get; private set; }

    /// <summary>ادمین جریمهٔ این قسط را بخشیده است.</summary>
    public bool PenaltyWaived { get; private set; }

    private Installment() { }

    internal Installment(int number, DateOnly dueDate, long amount)
    {
        Number = number;
        DueDate = dueDate;
        Amount = amount;
    }

    public bool IsPaid => Status == InstallmentStatus.Paid;

    internal void SetAmount(long amount) => Amount = amount;

    internal void Reschedule(DateOnly dueDate) => DueDate = dueDate;

    internal void MarkPaid(long paymentId, long penalty, DateTime at)
    {
        Status = InstallmentStatus.Paid;
        PaymentId = paymentId;
        PenaltyPaid = penalty;
        PaidAt = at;
    }

    internal void WaivePenalty() => PenaltyWaived = true;
}
