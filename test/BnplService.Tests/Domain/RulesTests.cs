namespace BnplService.Tests.Domain;

public class RulesTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 49_160)]      // ۱٪ از ۴٬۹۱۶٬۶۶۰ = ۴۹٬۱۶۶٫۶ ← رند به پایین تا ده ریال
    [InlineData(3, 147_490)]
    public void Late_penalty_is_one_percent_per_day_after_due_date(int daysLate, long expected)
    {
        var due = new DateOnly(2026, 10, 1);
        Assert.Equal(expected, LatePenalty.Calculate(4_916_660, 1, due, due.AddDays(daysLate)));
    }

    [Fact]
    public void No_penalty_before_due_date() =>
        Assert.Equal(0, LatePenalty.Calculate(4_916_660, 1, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 1)));

    [Theory]
    [InlineData("0011111111", false)]
    [InlineData("1111111111", false)]
    [InlineData("0013542419", true)]
    [InlineData("0499370899", true)]
    [InlineData("123", false)]
    public void National_code_checksum(string code, bool valid) => Assert.Equal(valid, NationalCodeRule.IsValid(code));

    [Theory]
    [InlineData("820540102680020817909002", true)]
    [InlineData("820540102680020817909003", false)]
    [InlineData("12345", false)]
    public void Sheba_mod97(string digits, bool valid) => Assert.Equal(valid, ShebaRule.IsValid(digits));

    [Theory]
    [InlineData("09121111111", "09121111111")]
    [InlineData("۰۹۱۲۱۱۱۱۱۱۱", "09121111111")]
    [InlineData("+98 912 111 1111", "09121111111")]
    [InlineData("9121111111", "09121111111")]
    [InlineData("02188888888", null)]
    public void Mobile_number_is_normalized(string input, string? expected) => Assert.Equal(expected, MobileNumber.Normalize(input));

    [Fact]
    public void Jalali_monthly_due_dates_keep_the_day()
    {
        var start = Jalali.ToDate(1405, 6, 31);
        Assert.Equal("1405/07/30", Jalali.Format(Jalali.AddMonths(start, 1)));   // مهر ۳۰ روز دارد
        Assert.Equal("1406/06/31", Jalali.Format(Jalali.AddMonths(start, 12)));
    }

    [Fact]
    public void Settings_reject_amounts_off_the_slider_steps()
    {
        var s = new BnplSettings();
        s.EnsureAmountAllowed(50_000_000);
        s.EnsureAmountAllowed(5_000_000_000);
        Assert.Throws<BnplDomainException>(() => s.EnsureAmountAllowed(40_000_000));
        Assert.Throws<BnplDomainException>(() => s.EnsureAmountAllowed(55_000_000));
        Assert.Throws<BnplDomainException>(() => s.EnsureAmountAllowed(6_000_000_000));
    }

    [Fact]
    public void Plan_rejects_upfront_fee_above_total_fee() =>
        Assert.Throws<BnplDomainException>(() => new BnplPlan("x طرح", 12, 4, 5, 1, 1));

    [Fact]
    public void Discount_code_must_be_either_percent_or_fixed() =>
        Assert.Throws<BnplDomainException>(() => new DiscountCode("ABC", "دو نوع", 10, 1000, null, null, null, null, DateTime.UtcNow));

    [Fact]
    public void Discount_code_usage_limit_is_enforced()
    {
        var code = new DiscountCode("ONCE", "یک‌بار", null, 100_000, null, null, null, 1, DateTime.UtcNow);
        code.EnsureUsable(DateTime.UtcNow);
        code.MarkUsed();
        Assert.Throws<BnplDomainException>(() => code.EnsureUsable(DateTime.UtcNow));
    }
}
