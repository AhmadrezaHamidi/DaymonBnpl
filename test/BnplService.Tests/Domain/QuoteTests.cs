namespace BnplService.Tests.Domain;

/// <summary>اعداد «شرایط وام» دقیقاً همان دو نمونهٔ فیگما.</summary>
public class QuoteTests
{
    private static BnplPlan Figma() => new("۱۲ قسط", 12, 22, 4, 1, 1);

    [Fact]
    public void Fifty_million_matches_figma()
    {
        var q = BnplQuote.Calculate(50_000_000, Figma(), 15_000_000);
        Assert.Equal(4_916_660, q.InstallmentAmount);
        Assert.Equal(6_916_660, q.FirstInstallmentAmount);
        Assert.Equal(60_999_920, q.TotalRepayment);
        Assert.Equal(2_000_000, q.UpfrontFee);
        Assert.Equal(10_999_920, q.TotalFee);
        Assert.Equal(4_916_660, q.LastInstallmentAmount);
    }

    [Fact]
    public void Two_hundred_million_matches_figma()
    {
        var q = BnplQuote.Calculate(200_000_000, Figma(), 15_000_000);
        Assert.Equal(19_666_660, q.InstallmentAmount);
        Assert.Equal(27_666_660, q.FirstInstallmentAmount);
        Assert.Equal(243_999_920, q.TotalRepayment);
    }

    [Fact]
    public void Installments_sum_to_total_repayment()
    {
        var q = BnplQuote.Calculate(370_000_000, Figma(), 15_000_000);
        Assert.Equal(q.TotalRepayment, Enumerable.Range(1, 12).Sum(n => q.AmountOf(n)));
    }

    [Fact]
    public void Token_amount_is_rounded_down_to_three_decimals()
    {
        var q = BnplQuote.Calculate(50_000_000, Figma(), 15_010_000);
        Assert.Equal(3.331m, q.TokenAmount);
    }

    [Fact]
    public void Discount_reduces_only_the_last_installment()
    {
        var plain = BnplQuote.Calculate(50_000_000, Figma(), 15_000_000);
        var discounted = BnplQuote.Calculate(50_000_000, 12, 22, 4, 1, 15_000_000, _ => 1_000_000);
        Assert.Equal(1_000_000, discounted.DiscountAmount);
        Assert.Equal(plain.FirstInstallmentAmount, discounted.FirstInstallmentAmount);
        Assert.Equal(plain.InstallmentAmount, discounted.AmountOf(11));
        Assert.Equal(plain.LastInstallmentAmount - 1_000_000, discounted.LastInstallmentAmount);
        Assert.Equal(plain.TotalRepayment - 1_000_000, discounted.TotalRepayment);
    }

    [Fact]
    public void Discount_never_makes_the_last_installment_negative()
    {
        var q = BnplQuote.Calculate(50_000_000, 12, 22, 4, 1, 15_000_000, _ => 999_999_999);
        Assert.Equal(0, q.LastInstallmentAmount);
        Assert.Equal(q.InstallmentAmount, q.DiscountAmount);
    }

    [Fact]
    public void Single_installment_contract_carries_fee_and_discount_on_the_same_installment()
    {
        var q = BnplQuote.Calculate(100_000_000, 1, 10, 2, 1, 15_000_000, _ => 500_000);
        Assert.Equal(q.FirstInstallmentAmount, q.LastInstallmentAmount);
        Assert.Equal(q.TotalRepayment, q.AmountOf(1));
        Assert.Equal(110_000_000 - 500_000, q.TotalRepayment);
    }

    [Fact]
    public void Percent_discount_code_is_taken_from_total_fee_and_capped()
    {
        var code = new DiscountCode("off50", "نیم کارمزد", 50, null, 3_000_000, null, null, null, DateTime.UtcNow);
        Assert.Equal("OFF50", code.Code);
        var q = BnplQuote.Calculate(50_000_000, Figma(), 15_000_000, code);
        Assert.Equal(3_000_000, q.DiscountAmount);   // نصف ۱۰٬۹۹۹٬۹۲۰ بیش از سقف است
    }

    [Fact]
    public void Missing_token_price_is_a_friendly_error()
    {
        var ex = Assert.Throws<BnplDomainException>(() => BnplQuote.Calculate(50_000_000, Figma(), 0));
        Assert.Contains("قیمت توکن", ex.Message);
    }
}
