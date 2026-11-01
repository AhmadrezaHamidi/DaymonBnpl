using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using DayaDiamond.Client;
using DayaDiamond.Mock;
using Microsoft.Extensions.Options;

// شبیه‌ساز سرور دایادایموند.
//  - سرویس‌های مستند (docs/dayadiamond-api.md): account/login، price/token، price/buy، gateway/list، user/getOrAdd،
//    profile/upsertInfo، profile/upsertAddress، buy/policy، buy/request، withdraw/validateRequest، gateway/redirectToIpg.
//  - ⚠ سرویس‌های پیشنهادی پیش‌خرید (در مستند نیستند): bnpl/wallet/*، bnpl/payment/*.
//  - کنترل تست (فقط mock): mock/state، mock/reset، mock/price، mock/wallet، mock/outage، mock/payments/{id}/complete.

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<MockOptions>(builder.Configuration.GetSection("Mock"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MockOptions>>().Value);
builder.Services.AddSingleton<MockState>();
builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<MockOptions>();
    return new MobileEncryptor(o.MobileKeyBase64, o.MobileIvBase64);
});

var app = builder.Build();
var inv = CultureInfo.InvariantCulture;

// ---------- پاسخ‌ها (پوشش isSuccessful/error/result مستند) ----------
static IResult Ok(object? result = null) => Results.Json(new { isSuccessful = true, error = (object?)null, result });
static IResult Fail(string message, int status = 200) =>
    Results.Json(new { isSuccessful = false, error = new { message }, result = (object?)null }, statusCode: status);

app.MapGet("/", () => Results.Text("DayaDiamond mock is running. See tools/DayaDiamond.Mock/README.md", "text/plain; charset=utf-8"));

// ---------- حساب ----------
app.MapPost("account/login", (LoginBody body, MockOptions o, MockState s) =>
{
    if (s.Outage) return Fail("سرویس موقتاً در دسترس نیست.", 503);
    if (body.Username != o.Username || body.Password != o.Password)
        return Fail("نام کاربری یا رمز عبور نادرست است.", 401);
    var exp = DateTimeOffset.UtcNow.AddSeconds(o.TokenLifetimeSeconds).ToUnixTimeSeconds();
    static string B64(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    var jwt = $"{B64("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{B64($"{{\"sub\":\"{body.Username}\",\"jti\":\"{Guid.NewGuid():N}\",\"exp\":{exp}}}")}.mock";
    lock (s.Sync) s.IssuedTokens.Add(jwt);
    return Ok(new { accesToken = jwt });   // «accesToken» با همان غلط املایی مستند
});

// همهٔ سرویس‌های زیر JWT می‌خواهند (Bearer یا توکن خام).
var api = app.MapGroup("").AddEndpointFilter(async (ctx, next) =>
{
    var s = ctx.HttpContext.RequestServices.GetRequiredService<MockState>();
    if (s.Outage) return Fail("سرویس موقتاً در دسترس نیست.", 503);
    var header = ctx.HttpContext.Request.Headers.Authorization.ToString();
    var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : header.Trim();
    bool valid;
    lock (s.Sync) valid = token.Length > 0 && s.IssuedTokens.Contains(token) && !Expired(token);
    return valid ? await next(ctx) : Fail("توکن نامعتبر یا منقضی است.", 401);
});

// ---------- قیمت و درگاه ----------
api.MapGet("price/token", (MockState s) => Ok(new { assetCode = "DAYADIAMOND", tokenPrice = s.TokenPriceToman }));

api.MapGet("price/buy", (decimal amount, string? assetCode, MockOptions o, MockState s) =>
{
    if (amount <= 0) return Fail("تعداد توکن نامعتبر است.");
    return Ok(new { price = BuyPrice(amount, s.TokenPriceToman, o.BuyFeePercentage) });
});

api.MapGet("gateway/list", () => Ok(new[]
{
    new { id = 5, title = "OmidPay", description = "امید پی", message = (string?)null },
}));

// ---------- مشتری ----------
api.MapPost("user/getOrAdd", (CodeBody body, MobileEncryptor encryptor, MockState s) =>
{
    string phone;
    try { phone = encryptor.Decrypt(body.Code); }
    catch (Exception) { return Fail("کد موبایل قابل رمزگشایی نیست.", 400); }
    if (!IsMobile(phone)) return Fail("شماره موبایل نامعتبر است.", 400);
    lock (s.Sync)
    {
        var c = s.Customer(phone);
        c.Registered = true;
        s.Log.Add($"getOrAdd {phone}");
        // طبق مستند این پاسخ بدون پوشش isSuccessful/result است.
        return Results.Json(new { phoneNumber = c.PhoneNumber, profileCompleted = c.ProfileCompleted, addressCompleted = c.AddressCompleted });
    }
});

api.MapPost("profile/upsertInfo", (InfoBody b, MockState s) =>
{
    var missing = Missing(("phoneNumber", b.PhoneNumber), ("nationalCode", b.NationalCode), ("firstName", b.FirstName),
        ("lastName", b.LastName), ("fatherName", b.FatherName), ("persianBirthDate", b.PersianBirthDate), ("gender", b.Gender), ("sheba", b.Sheba));
    if (missing != null) return Fail($"فیلد {missing} الزامی است.");
    if (b.Gender is not ("Male" or "Female")) return Fail("جنسیت باید Male یا Female باشد.");
    lock (s.Sync)
    {
        if (!s.Customers.TryGetValue(b.PhoneNumber!, out var c) || !c.Registered) return Fail("کاربر یافت نشد؛ ابتدا user/getOrAdd.");
        c.Info = new() { ["nationalCode"] = b.NationalCode, ["firstName"] = b.FirstName, ["lastName"] = b.LastName,
            ["fatherName"] = b.FatherName, ["persianBirthDate"] = b.PersianBirthDate, ["gender"] = b.Gender, ["sheba"] = b.Sheba };
        c.ProfileCompleted = true;
    }
    return Ok();
});

api.MapPost("profile/upsertAddress", (AddressBody b, MockState s) =>
{
    var missing = Missing(("phoneNumber", b.PhoneNumber), ("province", b.Province), ("city", b.City), ("street", b.Street),
        ("postalCode", b.PostalCode), ("telephoneNumber", b.TelephoneNumber));
    if (missing != null) return Fail($"فیلد {missing} الزامی است.");
    if (b.PostalCode!.Length != 10 || !b.PostalCode.All(char.IsAsciiDigit)) return Fail("کد پستی باید ۱۰ رقم باشد.");
    lock (s.Sync)
    {
        if (!s.Customers.TryGetValue(b.PhoneNumber!, out var c) || !c.Registered) return Fail("کاربر یافت نشد؛ ابتدا user/getOrAdd.");
        c.Address = new() { ["province"] = b.Province, ["city"] = b.City, ["street"] = b.Street,
            ["postalCode"] = b.PostalCode, ["telephoneNumber"] = b.TelephoneNumber };
        c.AddressCompleted = true;
    }
    return Ok();
});

// ---------- خرید و بازخرید ----------
api.MapGet("buy/policy", (MockOptions o) => Ok(new
{
    minAmount = o.MinBuyTokens, maxAmount = o.MaxBuyTokens, feePercentage = o.BuyFeePercentage,
    isHoliday = false, withdrawDuration = (object?)null, message = (string?)null,
}));

api.MapPost("buy/request", (HttpContext http, BuyBody b, MockOptions o, MockState s) =>
{
    if (string.IsNullOrWhiteSpace(b.PhoneNumber) || string.IsNullOrWhiteSpace(b.ReturnUrl)) return Fail("phoneNumber و returnUrl الزامی است.");
    if (b.Amount < o.MinBuyTokens || b.Amount > o.MaxBuyTokens) return Fail($"تعداد توکن باید بین {o.MinBuyTokens} و {o.MaxBuyTokens} باشد.");
    if (b.GatewayId != 5) return Fail("درگاه یافت نشد.");
    lock (s.Sync)
    {
        var p = NewPayment(s, MockPaymentKind.Buy, b.PhoneNumber, BuyPrice(b.Amount, s.TokenPriceToman, o.BuyFeePercentage) * 10, b.ReturnUrl, null,
            $"خرید {b.Amount.ToString(inv)} توکن");
        p.TokenAmount = b.Amount;
        return Ok(new { payUrl = PayUrl(http, p.PaymentExtId), paymentExtId = p.PaymentExtId });
    }
});

api.MapGet("withdraw/validateRequest", (decimal tokenAmount, string phoneNumber, MockState s) =>
{
    if (tokenAmount <= 0) return Fail("تعداد توکن نامعتبر است.");
    lock (s.Sync)
        return s.Customer(phoneNumber).FreeAmount < tokenAmount ? Fail("موجودی آزاد توکن کافی نیست.") : Ok();
});

// ---------- ⚠ پیشنهادی: حساب توکن پیش‌خرید ----------
api.MapGet("bnpl/wallet/balance", (string phoneNumber, MockState s) =>
{
    lock (s.Sync)
    {
        var c = s.Customer(phoneNumber);
        return Ok(new { phoneNumber = c.PhoneNumber, freeAmount = c.FreeAmount, lockedAmount = c.LockedAmount });
    }
});

api.MapPost("bnpl/wallet/lock", (WalletBody b, MockState s) => Wallet(b, s, "lock", c =>
{
    if (s.Locks.ContainsKey(b.Reference!)) return Ok();          // تکراری
    if (c.FreeAmount < b.Amount) return Fail($"موجودی آزاد کافی نیست ({c.FreeAmount.ToString(inv)} توکن).");
    c.FreeAmount -= b.Amount;
    c.LockedAmount += b.Amount;
    s.Locks[b.Reference!] = b.Amount;
    return Ok();
}));

api.MapPost("bnpl/wallet/release", (WalletBody b, MockState s) => Wallet(b, s, "release", c =>
{
    if (!s.Locks.TryGetValue(b.Reference!, out var locked)) return Fail("وثیقه‌ای با این شناسه قفل نشده است.");
    if (locked == 0) return Ok();                                 // قبلاً آزاد شده
    if (b.Amount != locked) return Fail($"مقدار آزادسازی ({b.Amount.ToString(inv)}) با وثیقهٔ قفل‌شده ({locked.ToString(inv)}) برابر نیست.");
    c.LockedAmount -= locked;
    c.FreeAmount += locked;
    s.Locks[b.Reference!] = 0;
    return Ok();
}));

api.MapPost("bnpl/wallet/credit", (WalletBody b, MockState s) => Wallet(b, s, "credit", c =>
{
    if (!s.Credits.Add(b.Reference!)) return Ok();               // تکراری
    c.FreeAmount += b.Amount;
    return Ok();
}));

api.MapPost("bnpl/wallet/debit", (WalletBody b, MockState s) => Wallet(b, s, "debit", c =>
{
    if (s.Debits.TryGetValue(b.Reference!, out var tracking)) return Ok(new { trackingNumber = tracking });
    if (c.FreeAmount < b.Amount) return Fail($"موجودی آزاد کافی نیست ({c.FreeAmount.ToString(inv)} توکن).");
    c.FreeAmount -= b.Amount;
    s.Debits[b.Reference!] = tracking = s.NextTracking();
    return Ok(new { trackingNumber = tracking });
}));

// ---------- ⚠ پیشنهادی: پرداخت قسط ----------
api.MapPost("bnpl/payment/request", (HttpContext http, InstallmentBody b, MockState s) =>
{
    if (string.IsNullOrWhiteSpace(b.PhoneNumber) || string.IsNullOrWhiteSpace(b.ReturnUrl) || string.IsNullOrWhiteSpace(b.Reference))
        return Fail("phoneNumber، returnUrl و reference الزامی است.");
    if (b.AmountRial <= 0) return Fail("مبلغ نامعتبر است.");
    if (b.GatewayId != 5) return Fail("درگاه یافت نشد.");
    lock (s.Sync)
    {
        var existing = s.Payments.Values.FirstOrDefault(p => p.Kind == MockPaymentKind.Installment && p.Reference == b.Reference);
        if (existing != null && existing.AmountRial != b.AmountRial) return Fail("این reference قبلاً با مبلغ دیگری ثبت شده است.");
        var p = existing ?? NewPayment(s, MockPaymentKind.Installment, b.PhoneNumber, b.AmountRial, b.ReturnUrl, b.Reference, b.Description ?? "");
        return Ok(new { payUrl = PayUrl(http, p.PaymentExtId), paymentExtId = p.PaymentExtId });
    }
});

api.MapGet("bnpl/payment/status", (string paymentExtId, MockOptions o, MockState s) =>
{
    lock (s.Sync)
    {
        if (!s.Payments.TryGetValue(paymentExtId, out var p)) return Fail("پرداخت یافت نشد.");
        if (p.Status == "Pending" && DateTimeOffset.UtcNow - p.CreatedAt > TimeSpan.FromMinutes(o.PaymentTimeoutMinutes))
        {
            p.Status = "Failed";
            p.Message = "مهلت پرداخت در درگاه تمام شد.";
        }
        return Ok(new { paymentExtId = p.PaymentExtId, status = p.Status, amountRial = p.AmountRial, trackingNumber = p.TrackingNumber, message = p.Message });
    }
});

// ---------- صفحهٔ درگاه آزمایشی (مرورگر) ----------
app.MapGet("gateway/redirectToIpg", (string paymentExtId, MockState s) =>
{
    MockPayment? p;
    lock (s.Sync) s.Payments.TryGetValue(paymentExtId, out p);
    if (p == null) return Results.NotFound();
    var id = WebUtility.HtmlEncode(p.PaymentExtId);
    var html = $"""
        <!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><title>درگاه آزمایشی</title></head>
        <body style="font-family:sans-serif;max-width:420px;margin:40px auto">
        <h2>درگاه پرداخت آزمایشی دایادایموند</h2>
        <p>{WebUtility.HtmlEncode(p.Description)}</p>
        <p>مبلغ: {p.AmountRial.ToString("#,0", inv)} ریال</p>
        <p><a href="/mock/pay/{id}?success=true">پرداخت موفق</a> · <a href="/mock/pay/{id}?success=false">انصراف / ناموفق</a></p>
        </body></html>
        """;
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("mock/pay/{paymentExtId}", (string paymentExtId, bool success, MockState s) =>
{
    lock (s.Sync)
    {
        var p = Complete(s, paymentExtId, success);
        if (p == null) return Results.NotFound();
        var sep = p.ReturnUrl.Contains('?') ? '&' : '?';
        return Results.Redirect($"{p.ReturnUrl}{sep}paymentExtId={Uri.EscapeDataString(p.PaymentExtId)}&status={p.Status}");
    }
});

// ---------- کنترل تست ----------
var mock = app.MapGroup("mock");
mock.MapGet("state", (MockState s) =>
{
    lock (s.Sync)
        return Results.Json(new { s.TokenPriceToman, s.Outage, customers = s.Customers.Values, payments = s.Payments.Values, locks = s.Locks, credits = s.Credits, debits = s.Debits, log = s.Log });
});
mock.MapPost("reset", (MockState s) => { lock (s.Sync) s.Reset(); return Results.NoContent(); });
mock.MapPost("price", (PriceBody b, MockState s) => { lock (s.Sync) s.TokenPriceToman = b.TokenPriceToman; return Results.NoContent(); });
mock.MapPost("outage", (OutageBody b, MockState s) => { lock (s.Sync) s.Outage = b.Enabled; return Results.NoContent(); });
mock.MapPost("wallet", (SeedWalletBody b, MockState s) =>
{
    lock (s.Sync)
    {
        var c = s.Customer(b.PhoneNumber);
        c.FreeAmount = b.FreeAmount;
        if (b.Registered is { } r) c.Registered = r;
        if (b.ProfileCompleted is { } pc) c.ProfileCompleted = pc;
        if (b.AddressCompleted is { } ac) c.AddressCompleted = ac;
    }
    return Results.NoContent();
});
mock.MapPost("payments/{paymentExtId}/complete", (string paymentExtId, bool success, MockState s) =>
{
    lock (s.Sync)
    {
        var p = Complete(s, paymentExtId, success);
        return p == null ? Results.NotFound() : Results.Json(new { p.PaymentExtId, p.Status, p.TrackingNumber, p.ReturnUrl });
    }
});

app.Run();

// ---------- کمکی‌ها ----------
static bool Expired(string jwt)
{
    try
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
        return DateTimeOffset.FromUnixTimeSeconds(doc.RootElement.GetProperty("exp").GetInt64()) <= DateTimeOffset.UtcNow;
    }
    catch (Exception) { return true; }
}

static bool IsMobile(string s) => s.Length == 11 && s.StartsWith("09", StringComparison.Ordinal) && s.All(char.IsAsciiDigit);

static string? Missing(params (string Name, string? Value)[] fields) =>
    fields.FirstOrDefault(f => string.IsNullOrWhiteSpace(f.Value)).Name;

static long BuyPrice(decimal tokens, long priceToman, decimal feePercent) =>
    (long)Math.Ceiling(tokens * priceToman * (1 + feePercent / 100));

static string PayUrl(HttpContext http, string id) =>
    $"{http.Request.Scheme}://{http.Request.Host}/gateway/redirectToIpg/?paymentExtId={id}";

static MockPayment NewPayment(MockState s, MockPaymentKind kind, string phone, long amountRial, string returnUrl, string? reference, string description)
{
    var p = new MockPayment
    {
        PaymentExtId = Guid.NewGuid().ToString(), Kind = kind, PhoneNumber = phone, AmountRial = amountRial,
        ReturnUrl = returnUrl, Reference = reference, Description = description,
    };
    s.Payments[p.PaymentExtId] = p;
    s.Log.Add($"payment {kind} {p.PaymentExtId} {amountRial}");
    return p;
}

/// <summary>پایان پرداخت در درگاه آزمایشی؛ تکرار، نتیجهٔ قبلی را عوض نمی‌کند. خرید موفق، توکن را به موجودی آزاد اضافه می‌کند.</summary>
static MockPayment? Complete(MockState s, string id, bool success)
{
    if (!s.Payments.TryGetValue(id, out var p)) return null;
    if (p.Status != "Pending") return p;
    if (success)
    {
        p.Status = "Paid";
        p.TrackingNumber = s.NextTracking();
        if (p.Kind == MockPaymentKind.Buy) s.Customer(p.PhoneNumber).FreeAmount += p.TokenAmount;
    }
    else
    {
        p.Status = "Failed";
        p.Message = "پرداخت توسط کاربر لغو شد.";
    }
    s.Log.Add($"complete {id} {p.Status}");
    return p;
}

static IResult Wallet(WalletBody b, MockState s, string op, Func<MockCustomer, IResult> apply)
{
    if (string.IsNullOrWhiteSpace(b.PhoneNumber) || string.IsNullOrWhiteSpace(b.Reference))
        return Results.Json(new { isSuccessful = false, error = new { message = "phoneNumber و reference الزامی است." }, result = (object?)null });
    if (b.Amount <= 0)
        return Results.Json(new { isSuccessful = false, error = new { message = "مقدار توکن باید مثبت باشد." }, result = (object?)null });
    lock (s.Sync)
    {
        s.Log.Add($"{op} {b.PhoneNumber} {b.Reference} {b.Amount.ToString(CultureInfo.InvariantCulture)}");
        return apply(s.Customer(b.PhoneNumber));
    }
}

public sealed record LoginBody(string? Username, string? Password);
public sealed record CodeBody(string Code);
public sealed record InfoBody(string? PhoneNumber, string? NationalCode, string? FirstName, string? LastName, string? FatherName,
    string? PersianBirthDate, string? Gender, string? Sheba);
public sealed record AddressBody(string? PhoneNumber, string? Province, string? City, string? Street, string? PostalCode, string? TelephoneNumber);
public sealed record BuyBody(string PhoneNumber, decimal Amount, int GatewayId, string ReturnUrl);
public sealed record WalletBody(string PhoneNumber, string? Reference, decimal Amount, string? Description);
public sealed record InstallmentBody(string PhoneNumber, long AmountRial, int GatewayId, string ReturnUrl, string? Description, string? Reference);
public sealed record PriceBody(long TokenPriceToman);
public sealed record OutageBody(bool Enabled);
public sealed record SeedWalletBody(string PhoneNumber, decimal FreeAmount, bool? Registered, bool? ProfileCompleted, bool? AddressCompleted);

/// <summary>برای WebApplicationFactory در تست‌ها.</summary>
public partial class Program;
