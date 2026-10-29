namespace BnplService.Infrastructure.Services.Sandbox;

/// <summary>قیمت ثابت آزمایشی.</summary>
public sealed class SandboxTokenPriceProvider(SandboxStore store) : ITokenPriceProvider
{
    public Task<TokenPriceDto> GetAsync(CancellationToken ct) =>
        Task.FromResult(new TokenPriceDto("DAYADIAMOND", store.Options.TokenPriceRial));
}

/// <summary>پروفایل آزمایشی در حافظه؛ ثبت اطلاعات یا نشانی همان بخش را کامل می‌کند.</summary>
public sealed class SandboxProfileProvider(SandboxStore store) : ICustomerProfileProvider
{
    public Task<CustomerProfileStatusDto> GetOrAddAsync(string mobileNumber, CancellationToken ct) => Task.FromResult(store.ProfileOf(mobileNumber));

    public Task UpsertInfoAsync(CustomerInfoInput input, CancellationToken ct)
    {
        store.SetProfile(store.ProfileOf(input.PhoneNumber) with { ProfileCompleted = true });
        return Task.CompletedTask;
    }

    public Task UpsertAddressAsync(CustomerAddressInput input, CancellationToken ct)
    {
        store.SetProfile(store.ProfileOf(input.PhoneNumber) with { AddressCompleted = true });
        return Task.CompletedTask;
    }
}

/// <summary>
/// درگاه آزمایشی: PayUrl به صفحهٔ Sandbox همین سرویس می‌رود (دو دکمهٔ «پرداخت موفق» و «ناموفق»)،
/// که بعد مثل درگاه واقعی به کال‌بک برمی‌گردد. Verify از همین حافظه جواب می‌دهد.
/// </summary>
public sealed class SandboxPaymentGateway(SandboxStore store, string payPageBaseUrl) : IBnplPaymentGateway
{
    public const int GatewayId = 5;

    public Task<IReadOnlyList<PaymentGatewayDto>> ListAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PaymentGatewayDto>>([new PaymentGatewayDto(GatewayId, "Sandbox", "درگاه آزمایشی")]);

    public Task<GatewayPaymentStartDto> StartAsync(GatewayPaymentStartInput input, CancellationToken ct)
    {
        var payment = store.AddPayment(input);
        return Task.FromResult(new GatewayPaymentStartDto(payment.Reference, $"{payPageBaseUrl.TrimEnd('/')}/{payment.Reference}"));
    }

    public Task<GatewayPaymentResultDto> VerifyAsync(string providerReference, long expectedAmount, CancellationToken ct)
    {
        var payment = store.FindPayment(providerReference);
        var result = payment switch
        {
            null => new GatewayPaymentResultDto(false, null, "پرداخت در درگاه آزمایشی یافت نشد."),
            { State: SandboxStore.PaymentState.Paid } when payment.Amount == expectedAmount =>
                new GatewayPaymentResultDto(true, "SBX" + providerReference[..10].ToUpperInvariant(), null),
            { State: SandboxStore.PaymentState.Paid } => new GatewayPaymentResultDto(false, null, "مبلغ پرداخت‌شده با مبلغ قسط برابر نیست."),
            { State: SandboxStore.PaymentState.Failed } => new GatewayPaymentResultDto(false, null, "پرداخت توسط کاربر لغو شد."),
            _ => new GatewayPaymentResultDto(false, null, "پرداخت انجام نشد."),
        };
        return Task.FromResult(result);
    }
}

/// <summary>نگهداری توکن آزمایشی: موجودی آزاد، قفل و آزادسازی توکن‌های هر قرارداد در حافظه.</summary>
public sealed class SandboxTokenCustody(SandboxStore store) : ITokenCustody
{
    public Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct) => Task.FromResult(store.WalletOf(mobileNumber).Free);

    public Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        var wallet = store.WalletOf(mobileNumber);
        lock (wallet) wallet.Locked[contractNumber] = tokenAmount;
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        var wallet = store.WalletOf(mobileNumber);
        lock (wallet)
            if (wallet.Locked.Remove(contractNumber)) wallet.Free += tokenAmount;
        return Task.CompletedTask;
    }

    public Task<string> DebitAsync(string mobileNumber, string contractNumber, decimal tokenAmount, string description, CancellationToken ct)
    {
        var wallet = store.WalletOf(mobileNumber);
        lock (wallet)
        {
            if (wallet.Free < tokenAmount) throw new BnplProviderException("موجودی توکن آزاد کافی نیست.");
            wallet.Free -= tokenAmount;
        }
        return Task.FromResult("SBXT" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant());
    }
}
