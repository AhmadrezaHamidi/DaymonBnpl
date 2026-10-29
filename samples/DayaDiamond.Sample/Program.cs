using DayaDiamond.Client;
using DayaDiamond.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// مقادیر محرمانه را با dotnet user-secrets یا متغیر محیطی (DayaDiamond__Password و ...) تنظیم کنید.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDayaDiamondClient(builder.Configuration);
using var host = builder.Build();

var daya = host.Services.GetRequiredService<IDayaDiamondClient>();
const string mobile = "09121111111";

try
{
    var price = await daya.GetTokenPriceAsync();
    Console.WriteLine($"قیمت {price.AssetCode}: {price.TokenPrice:N0} تومان");

    foreach (var g in await daya.GetGatewaysAsync())
        Console.WriteLine($"درگاه {g.Id}: {g.Title} ({g.Description})");

    // ۱) ثبت نام / دریافت مشتری
    var user = await daya.GetOrAddUserAsync(mobile);
    Console.WriteLine($"کاربر {user.PhoneNumber} - پروفایل: {user.ProfileCompleted}، آدرس: {user.AddressCompleted}");

    // ۲) تکمیل پروفایل و آدرس در صورت نیاز
    if (!user.ProfileCompleted)
    {
        await daya.UpsertProfileInfoAsync(new UpsertProfileInfoRequest
        {
            PhoneNumber = mobile,
            NationalCode = "0011111111",
            FirstName = "سید حامد",
            LastName = "نظری",
            FatherName = "مرتضی",
            PersianBirthDate = "1360/11/12",
            Gender = Genders.Male,
            Sheba = "360560611828006835245102"
        });
    }

    if (!user.AddressCompleted)
    {
        await daya.UpsertAddressAsync(new UpsertAddressRequest
        {
            PhoneNumber = mobile,
            Province = "تهران",
            City = "تهران",
            Street = "خ ولیعصر",
            PostalCode = "1234567890",
            TelephoneNumber = "02188888888"
        });
    }

    // ۳) خرید
    var policy = await daya.GetBuyPolicyAsync();
    Console.WriteLine($"خرید بین {policy.MinAmount} تا {policy.MaxAmount} توکن، کارمزد {policy.FeePercentage}٪، تعطیل: {policy.IsHoliday}");

    const decimal amount = 3;
    var buyPrice = await daya.GetBuyPriceAsync(amount);
    Console.WriteLine($"مبلغ قابل پرداخت برای {amount} توکن: {buyPrice.Price:N0} تومان");

    var buy = await daya.CreateBuyRequestAsync(new BuyRequest
    {
        PhoneNumber = mobile,
        Amount = amount,
        GatewayId = 5,
        ReturnUrl = "https://your-site.example/daya/return"
    });
    Console.WriteLine($"کاربر را به این آدرس هدایت کنید: {buy.PayUrl} (paymentExtId={buy.PaymentExtId})");

    // ۴) بررسی امکان بازخرید
    await daya.ValidateWithdrawRequestAsync(2, mobile);
    Console.WriteLine("بازخرید ۲ توکن مجاز است.");
}
catch (DayaDiamondException ex)
{
    Console.Error.WriteLine($"خطای دایادایموند ({ex.StatusCode}): {ex.Message}");
}
