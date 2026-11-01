using DayaDiamond.Client;
using DayaDiamond.Client.Models;

namespace BnplService.Infrastructure.Providers.DayaDiamond;

/// <summary>قیمت توکن از GET price/token؛ دایادایموند تومان می‌دهد و این سرویس با ریال کار می‌کند (×۱۰).</summary>
public sealed class DayaDiamondTokenPriceProvider(IDayaDiamondClient client) : ITokenPriceProvider
{
    public const int RialPerToman = 10;

    public Task<TokenPriceDto> GetAsync(CancellationToken ct) => DayaDiamondErrors.Call(async () =>
    {
        var price = await client.GetTokenPriceAsync(ct);
        return new TokenPriceDto(string.IsNullOrEmpty(price.AssetCode) ? "DAYADIAMOND" : price.AssetCode,
            (long)Math.Round(price.TokenPrice * RialPerToman, MidpointRounding.AwayFromZero));
    }, "دریافت قیمت توکن");
}

/// <summary>پروفایل مشتری: user/getOrAdd (موبایل رمز‌شده)، profile/upsertInfo، profile/upsertAddress.</summary>
public sealed class DayaDiamondProfileProvider(IDayaDiamondClient client) : ICustomerProfileProvider
{
    public Task<CustomerProfileStatusDto> GetOrAddAsync(string mobileNumber, CancellationToken ct) => DayaDiamondErrors.Call(async () =>
    {
        var user = await client.GetOrAddUserAsync(mobileNumber, ct);
        return new CustomerProfileStatusDto(string.IsNullOrEmpty(user.PhoneNumber) ? mobileNumber : user.PhoneNumber,
            user.ProfileCompleted, user.AddressCompleted);
    }, "استعلام پروفایل");

    public Task UpsertInfoAsync(CustomerInfoInput input, CancellationToken ct) => DayaDiamondErrors.Call(() =>
        client.UpsertProfileInfoAsync(new UpsertProfileInfoRequest
        {
            PhoneNumber = input.PhoneNumber,
            NationalCode = input.NationalCode,
            FirstName = input.FirstName,
            LastName = input.LastName,
            FatherName = input.FatherName,
            PersianBirthDate = input.PersianBirthDate,
            Gender = input.Gender,
            Sheba = input.Sheba,
        }, ct), "ثبت اطلاعات هویتی");

    public Task UpsertAddressAsync(CustomerAddressInput input, CancellationToken ct) => DayaDiamondErrors.Call(() =>
        client.UpsertAddressAsync(new UpsertAddressRequest
        {
            PhoneNumber = input.PhoneNumber,
            Province = input.Province,
            City = input.City,
            Street = input.Street,
            PostalCode = input.PostalCode,
            TelephoneNumber = input.TelephoneNumber,
        }, ct), "ثبت نشانی");
}

/// <summary>
/// درگاه پرداخت قسط: فهرست از gateway/list (مستند) و پرداخت/استعلام از bnpl/payment/* (⚠ پیشنهادی).
/// نتیجه همیشه از خود دایادایموند استعلام و مبلغ با مبلغ قسط مقایسه می‌شود.
/// </summary>
public sealed class DayaDiamondPaymentGateway(IDayaDiamondClient client, IDayaDiamondBnplApi bnpl) : IBnplPaymentGateway
{
    public Task<IReadOnlyList<PaymentGatewayDto>> ListAsync(CancellationToken ct) => DayaDiamondErrors.Call(async () =>
    {
        var list = await client.GetGatewaysAsync(ct);
        return (IReadOnlyList<PaymentGatewayDto>)list.Select(g => new PaymentGatewayDto(g.Id, g.Title, g.Description)).ToList();
    }, "دریافت فهرست درگاه‌ها");

    public Task<GatewayPaymentStartDto> StartAsync(GatewayPaymentStartInput input, CancellationToken ct) => DayaDiamondErrors.Call(async () =>
    {
        var result = await bnpl.CreateInstallmentPaymentAsync(new InstallmentPaymentRequest
        {
            PhoneNumber = input.MobileNumber,
            AmountRial = input.Amount,
            GatewayId = input.GatewayId,
            ReturnUrl = input.CallbackUrl,
            Description = input.Description,
            Reference = input.PaymentId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }, ct);
        if (string.IsNullOrEmpty(result.PaymentExtId) || string.IsNullOrEmpty(result.PayUrl))
            throw new BnplProviderException("شروع پرداخت: پاسخ درگاه ناقص بود.");
        return new GatewayPaymentStartDto(result.PaymentExtId, result.PayUrl);
    }, "شروع پرداخت");

    public Task<GatewayPaymentResultDto> VerifyAsync(string providerReference, long expectedAmount, CancellationToken ct) => DayaDiamondErrors.Call(async () =>
    {
        var status = await bnpl.GetInstallmentPaymentStatusAsync(providerReference, ct);
        return status.Status switch
        {
            PaymentStatuses.Paid when status.AmountRial == expectedAmount =>
                new GatewayPaymentResultDto(true, status.TrackingNumber, null),
            PaymentStatuses.Paid => new GatewayPaymentResultDto(false, status.TrackingNumber,
                "مبلغ پرداخت‌شده با مبلغ قسط برابر نیست."),
            // نتیجه هنوز معلوم نیست: خطا می‌دهیم تا پرداخت «در انتظار» بماند و Worker دوباره استعلام کند (نه اینکه ناموفق ثبت شود).
            PaymentStatuses.Pending => throw new BnplProviderException("نتیجهٔ پرداخت هنوز از درگاه نرسیده است؛ چند دقیقهٔ دیگر بررسی می‌شود."),
            _ => new GatewayPaymentResultDto(false, status.TrackingNumber, status.Message ?? "پرداخت ناموفق بود."),
        };
    }, "استعلام پرداخت");
}

/// <summary>حساب توکن مشتری از bnpl/wallet/* (⚠ پیشنهادی). reference = شمارهٔ قرارداد تا تکرار بی‌اثر باشد.</summary>
public sealed class DayaDiamondTokenCustody(IDayaDiamondBnplApi bnpl) : ITokenCustody
{
    public Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct) => DayaDiamondErrors.Call(async () =>
        (await bnpl.GetWalletBalanceAsync(mobileNumber, ct)).FreeAmount, "استعلام موجودی توکن");

    public Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct) =>
        DayaDiamondErrors.Call(() => bnpl.LockTokensAsync(Op(mobileNumber, contractNumber, tokenAmount, "وثیقهٔ پیش‌خرید"), ct),
            "مسدودسازی وثیقه");

    public Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct) =>
        DayaDiamondErrors.Call(() => bnpl.ReleaseTokensAsync(Op(mobileNumber, contractNumber, tokenAmount, "آزادسازی وثیقه"), ct),
            "آزادسازی وثیقه");

    public Task CreditAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct) =>
        DayaDiamondErrors.Call(() => bnpl.CreditTokensAsync(Op(mobileNumber, contractNumber, tokenAmount, "توکن پیش‌خرید"), ct),
            "واریز توکن");

    public Task<string> DebitAsync(string mobileNumber, string reference, decimal tokenAmount, string description, CancellationToken ct) =>
        DayaDiamondErrors.Call(async () =>
            (await bnpl.DebitTokensAsync(Op(mobileNumber, reference, tokenAmount, description), ct)).TrackingNumber,
            "پرداخت توکنی");

    private static WalletOperationRequest Op(string mobile, string reference, decimal amount, string description) => new()
    {
        PhoneNumber = mobile,
        Reference = reference,
        Amount = amount,
        Description = description,
    };
}
