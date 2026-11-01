# DayaDiamond.Mock

شبیه‌ساز سرور دایادایموند برای توسعه و تست، بدون نیاز به سرور واقعی. وضعیت فقط در حافظه است و با هر اجرا از نو شروع می‌شود.

```bash
dotnet run --project tools/DayaDiamond.Mock --urls http://localhost:5199
```

سپس در سرویس BNPL (یا `samples/DayaDiamond.Sample`) این تنظیمات را بگذارید. مقدارهای `appsettings.json` این پروژه فقط آزمایشی‌اند و کلید یا رمز واقعی نیستند:

```json
"DayaDiamond": {
  "BaseUrl": "http://localhost:5199/",
  "Username": "mock-user",
  "Password": "mock-password",
  "MobileKeyBase64": "<Mock:MobileKeyBase64 از appsettings.json این پروژه>",
  "MobileIvBase64": "<Mock:MobileIvBase64>"
}
```

## سرویس‌های مستند (docs/dayadiamond-api.md)

| مسیر | رفتار mock |
|---|---|
| `POST account/login` | با نام کاربری و رمز `Mock` یک JWT با `exp` برمی‌گرداند (کلید `accesToken` با همان غلط املایی مستند) |
| `GET price/token` | قیمت به تومان، پیش‌فرض ۱٬۵۰۱٬۰۰۰ |
| `GET price/buy?amount=` | تعداد × قیمت × (۱ + کارمزد) |
| `GET gateway/list` | فقط OmidPay با شناسهٔ ۵ |
| `POST user/getOrAdd` | موبایل را با همان AES-ECB رمزگشایی می‌کند؛ پاسخ بدون پوشش `isSuccessful`، مثل مستند |
| `POST profile/upsertInfo`، `profile/upsertAddress` | فیلدهای خالی و کاربر ثبت‌نشده را رد می‌کند |
| `GET buy/policy`، `POST buy/request` | خرید ۰٫۵ تا ۱۲۰ توکن؛ پس از پرداخت موفق، توکن به موجودی آزاد اضافه می‌شود |
| `GET withdraw/validateRequest` | اگر موجودی آزاد کافی نباشد خطا می‌دهد |
| `GET gateway/redirectToIpg/?paymentExtId=` | صفحهٔ درگاه آزمایشی با دو لینک «موفق» و «ناموفق» که به `returnUrl` برمی‌گردند |

همهٔ مسیرها به‌جز login و صفحهٔ درگاه، توکن `Authorization: Bearer` می‌خواهند و بدون آن 401 می‌دهند.

## ⚠ سرویس‌های پیشنهادی پیش‌خرید (در مستند دایادایموند نیستند)

شکل این سرویس‌ها پیشنهاد ماست و باید با تیم دایادایموند نهایی شود (`src/DayaDiamond.Client/IDayaDiamondBnplApi.cs`).

| مسیر | کار |
|---|---|
| `GET bnpl/wallet/balance?phoneNumber=` | موجودی آزاد و مسدود |
| `POST bnpl/wallet/lock` | مسدودسازی وثیقه؛ تکرار با همان `reference` بی‌اثر است |
| `POST bnpl/wallet/release` | آزادسازی همان وثیقه؛ مقدار باید با مقدار قفل‌شده برابر باشد |
| `POST bnpl/wallet/credit` | واریز توکن خریده‌شده با اعتبار؛ با `reference` تکراری دوباره واریز نمی‌شود |
| `POST bnpl/wallet/debit` | پرداخت توکنی قسط؛ تکرار همان شمارهٔ پیگیری را برمی‌گرداند |
| `POST bnpl/payment/request` | شروع پرداخت مبلغ قسط (ریال) از درگاه |
| `GET bnpl/payment/status?paymentExtId=` | `Pending`، `Paid` یا `Failed`؛ پرداخت رهاشده پس از ۱۵ دقیقه `Failed` می‌شود |

## کنترل تست (فقط mock)

| مسیر | کار |
|---|---|
| `GET mock/state` | همهٔ وضعیت: مشتری‌ها، پرداخت‌ها، قفل‌ها، لاگ |
| `POST mock/reset` | پاک کردن همه چیز |
| `POST mock/price {"tokenPriceToman":1600000}` | تغییر قیمت |
| `POST mock/wallet {"phoneNumber":"0912…","freeAmount":1}` | تنظیم موجودی آزاد (و اختیاری `registered`، `profileCompleted`، `addressCompleted`) |
| `POST mock/outage {"enabled":true}` | همهٔ سرویس‌ها 503 می‌دهند |
| `POST mock/payments/{id}/complete?success=true` | پایان پرداخت بدون مرورگر |

مشتری‌ای که اولین بار دیده شود، ۱۰۰ توکن آزاد دارد (`Mock:DefaultFreeTokens`).
