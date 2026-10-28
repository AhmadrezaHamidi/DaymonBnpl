namespace BnplService.Domain.Rules;

/// <summary>
/// جریمهٔ دیرکرد (ماده شش قرارداد): «روزانه ۱ درصد مبلغ آن قسط را به عنوان جریمه پرداخت نماید».
/// روزِ سررسید خودش جریمه ندارد؛ از فردای سررسید به ازای هر روز تقویمی (تهران) جریمه اضافه می‌شود.
/// </summary>
public static class LatePenalty
{
    public static int DaysLate(DateOnly dueDate, DateOnly today) => Math.Max(0, today.DayNumber - dueDate.DayNumber);

    public static long Calculate(long installmentAmount, decimal dailyPercent, DateOnly dueDate, DateOnly today) =>
        Money.FloorTo10(installmentAmount * dailyPercent / 100m * DaysLate(dueDate, today));
}
