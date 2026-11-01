using System.Globalization;

namespace DayaDiamond.Mock;

/// <summary>
/// وضعیت درون‌حافظهٔ mock: توکن‌های لاگین، مشتری‌ها، کیف توکن، و پرداخت‌ها. همهٔ تغییرات زیر یک قفل انجام می‌شود.
/// قوانین کیف (سرویس‌های پیشنهادی bnpl/wallet/*):
///  - lock/credit/debit با reference تکراری دوباره اعمال نمی‌شوند (idempotent).
///  - release فقط وثیقه‌ای را آزاد می‌کند که با همان reference قفل شده و هنوز آزاد نشده است.
/// </summary>
public sealed class MockState(MockOptions options)
{
    public readonly object Sync = new();

    public long TokenPriceToman { get; set; } = options.TokenPriceToman;
    public bool Outage { get; set; }
    public HashSet<string> IssuedTokens { get; } = [];
    public Dictionary<string, MockCustomer> Customers { get; } = new();
    public Dictionary<string, MockPayment> Payments { get; } = new();
    public Dictionary<string, decimal> Locks { get; } = new();          // reference → مقدار قفل‌شدهٔ باقی‌مانده
    public HashSet<string> Credits { get; } = [];
    public Dictionary<string, string> Debits { get; } = new();        // reference → شمارهٔ پیگیری
    public List<string> Log { get; } = [];
    private int _tracking = 1000;

    public MockCustomer Customer(string phone)
    {
        if (!Customers.TryGetValue(phone, out var c))
            Customers[phone] = c = new MockCustomer { PhoneNumber = phone, FreeAmount = options.DefaultFreeTokens };
        return c;
    }

    public string NextTracking() => "MOCK" + (++_tracking).ToString(CultureInfo.InvariantCulture);

    public void Reset()
    {
        TokenPriceToman = options.TokenPriceToman;
        Outage = false;
        IssuedTokens.Clear();
        Customers.Clear();
        Payments.Clear();
        Locks.Clear();
        Credits.Clear();
        Debits.Clear();
        Log.Clear();
    }
}

public sealed class MockCustomer
{
    public string PhoneNumber { get; set; } = string.Empty;
    public bool Registered { get; set; }
    public bool ProfileCompleted { get; set; }
    public bool AddressCompleted { get; set; }
    public decimal FreeAmount { get; set; }
    public decimal LockedAmount { get; set; }
    public Dictionary<string, string?> Info { get; set; } = new();
    public Dictionary<string, string?> Address { get; set; } = new();
}

public enum MockPaymentKind { Buy, Installment }

public sealed class MockPayment
{
    public string PaymentExtId { get; set; } = string.Empty;
    public MockPaymentKind Kind { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public long AmountRial { get; set; }
    public decimal TokenAmount { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? TrackingNumber { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
