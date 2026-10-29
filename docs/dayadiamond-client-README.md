# DayaDiamond.Client

کلاینت C# (.NET 8) برای سرویس‌های دایادایموند، بر اساس مستند ارسالی.

## ساختار

```
src/DayaDiamond.Client/      کتابخانه کلاینت
  DayaDiamondClient.cs       پیاده‌سازی همه سرویس‌ها + مدیریت خودکار JWT
  MobileEncryptor.cs         Encrypt/Decrypt موبایل (AES-ECB, PKCS7, Base64)
  Models/                    DTOهای درخواست/پاسخ
samples/DayaDiamond.Sample/  نمونه استفاده کامل (ثبت نام → پروفایل → خرید → بازخرید)
```

## راه‌اندازی

```csharp
builder.Services.AddDayaDiamondClient(builder.Configuration);
// سپس IDayaDiamondClient را inject کنید.
```

```json
"DayaDiamond": {
  "BaseUrl": "https://...",
  "Username": "", "Password": "",
  "MobileKeyBase64": "", "MobileIvBase64": "",
  "AuthorizationScheme": "Bearer"
}
```

کلید، IV و رمز عبور را در کد یا گیت قرار ندهید؛ از `dotnet user-secrets` یا متغیر محیطی
(`DayaDiamond__Password` و ...) استفاده کنید.

## سرویس‌ها

| متد | سرویس |
|---|---|
| `LoginAsync` | `POST account/login` (خودکار؛ توکن تا زمان `exp` کش می‌شود و در 401 یک بار تازه می‌شود) |
| `GetTokenPriceAsync` | `GET price/token` |
| `GetGatewaysAsync` | `GET gateway/list` |
| `GetOrAddUserAsync(mobile)` | `POST user/getOrAdd` (موبایل خام بدهید؛ Encrypt داخل کلاینت) |
| `UpsertProfileInfoAsync` | `POST profile/upsertInfo` |
| `UpsertAddressAsync` | `POST profile/upsertAddress` |
| `GetBuyPolicyAsync` | `GET buy/policy` |
| `GetBuyPriceAsync(amount, assetCode)` | `GET price/buy` |
| `CreateBuyRequestAsync` | `POST buy/request` → کاربر را به `PayUrl` هدایت کنید |
| `ValidateWithdrawRequestAsync(tokenAmount, phone)` | `GET withdraw/validateRequest` |

هر پاسخی با `isSuccessful = false` یا کد HTTP ناموفق، `DayaDiamondException` می‌دهد
(فیلد `Error` همان `error` خام سرور است).

## فرض‌هایی که مستند روشن نکرده

- **هدر Authorization**: مستند فقط «JwtToken» گفته؛ پیش‌فرض `Bearer <token>` است. اگر سرور توکن خام
  می‌خواهد، `AuthorizationScheme` را خالی بگذارید.
- **فیلد توکن لاگین**: مستند `accesToken` (با یک s) نوشته؛ هر دو املا پشتیبانی می‌شود.
- **پاسخ getOrAdd**: در مستند بدون پوشش `isSuccessful/result` آمده؛ هر دو حالت پشتیبانی می‌شود.
- **gender**: فقط `Male` در مستند آمده؛ `Female` حدس است.
- **withdrawDuration**: نوعش مشخص نیست (null)، به صورت `JsonElement?` نگه داشته شده.
- **ساختار error**: مشخص نیست؛ کلاینت از `message` / رشته خام استفاده می‌کند.
- مستند در بخش بازخرید تمام شده؛ سرویس ثبت درخواست بازخرید (و احتمالا استعلام وضعیت پرداخت) هنوز اضافه نشده.
