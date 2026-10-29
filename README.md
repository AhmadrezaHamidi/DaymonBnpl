# DaymonBnpl

بک‌اند «پیش‌خرید اقساطی» (BNPL) توکن الماس، به‌همراه کلاینت API دایادایموند. نوشته‌شده با .NET 8.
لایه‌بندی مثل `facilityservice` در IdentityAndFaciltityCore است.

## ساختار

| پروژه | توضیح |
|---|---|
| `src/DayaDiamond.Client` | کلاینت API دایادایموند: لاگین و توکن، قیمت، درگاه‌ها، getOrAdd با رمزنگاری موبایل، پروفایل، خرید، برداشت |
| `src/BnplService.Domain` | موجودیت‌ها (درخواست، قسط، پرداخت، تنظیمات، پلن، کد تخفیف)، قوانین محاسبه، تاریخ شمسی، کد ملی و شبا |
| `src/BnplService.Contracts` | DTOها، `IBnplDbContext` و پورت‌های ارائه‌دهنده (قیمت، پروفایل، درگاه، نگهداری توکن) |
| `src/BnplService.Application` | Handlerهای MediatR برای مشتری، پنل ادمین و کار دوره‌ای (Maintenance) |
| `src/BnplService.Infrastructure` | EF Core (SqlServer و Sqlite)، پیکربندی جدول‌ها، Migrationها و Seed؛ پیاده‌سازی پورت‌ها روی دایادایموند و نسخهٔ Sandbox |
| `src/BnplService.IoC` | ثبت همهٔ وابستگی‌ها با `AddBnplServices` (مثل `FacilityService.IoC`) |
| `src/BnplService.Api` | کنترلرهای مشتری و پنل، کال‌بک درگاه، صفحهٔ درگاه آزمایشی، احراز هویت از Identity، Worker دوره‌ای و Swagger |
| `Database/` | `bnpl_migrations.sql` (اسکریپت idempotent برای DBA) و `identity/bnpl_panel_permissions.sql` (کلیدهای مجوز پنل در Identity) |
| `test/BnplService.Tests` | ۷۵ تست: محاسبات فیگما، قوانین، سناریوهای مشتری و ادمین، کل مسیر HTTP با درگاه Sandbox، ارائه‌دهنده‌ها و همگامی Migration |
| `samples/DayaDiamond.Sample` | برنامهٔ نمونه‌ای که کل مسیر کلاینت دایادایموند را اجرا می‌کند |
| `docs/` | مستند API دایادایموند، PDF فیگما، یادداشت‌ها و خروجی چت‌های پروژه |

## قوانین پیاده‌شده (از فیگما)

- مبلغ بین ۵۰ میلیون و ۵ میلیارد ریال، با گام‌های ۱۰ میلیون ریالی.
- پلن پیش‌فرض ۱۲ قسط است با کارمزد ۲۲٪. ۴٪ آن در قسط اول گرفته می‌شود.
  - برای ۵۰ میلیون: قسط اول ۶٬۹۱۶٬۶۶۰ و بقیهٔ اقساط ۴٬۹۱۶٬۶۶۰.
  - جمع کل ۶۰٬۹۹۹٬۹۲۰ ریال است.
- تخفیف فقط از قسط آخر کم می‌شود.
- جریمهٔ دیرکرد روزانه ۱٪ مبلغ قسط است.
- وضعیت‌های درخواست به ترتیب: پیش‌نویس ← پذیرش شرایط ← امضا ← فعال ← تسویه.
  - درخواست ناتمام بعد از ۲۴ ساعت منقضی می‌شود.
  - قسط اول باید تا ۳۰ دقیقه بعد از امضا پرداخت شود.
- تعداد توکن با قیمت لحظهٔ پرداخت قسط اول قطعی می‌شود.
  - توکن‌ها قفل می‌شوند و بعد از تسویه آزاد می‌شوند.
- پرداخت دیرهنگام یا تکراری به وضعیت «نیاز به استرداد» می‌رود و ادمین آن را ثبت می‌کند.
- در پنل ادمین این امکان‌ها هست:
  - داشبورد، جستجو، جزئیات و فسخ قرارداد
  - بخشودگی جریمه، ثبت پرداخت دستی و ثبت استرداد
  - تنظیمات، پلن‌ها و کدهای تخفیف

## اجرا

```bash
dotnet test
cd src/BnplService.Api && dotnet run     # http://localhost:55460/swagger
```

در محیط Development دیتابیس SQLite (`bnpl.db`) خودکار ساخته می‌شود. هر چهار ارائه‌دهنده هم Sandbox هستند: قیمت ثابت، پروفایل کامل، درگاه آزمایشی و ۱۰۰ توکن آزاد.
پس بدون دایادایموند کل مسیر قابل امتحان است. `payUrl` پرداخت درگاهی یک صفحه با دو دکمهٔ «پرداخت موفق» و «ناموفق» باز می‌کند.
این صفحه بعد از انتخاب، مثل درگاه واقعی به کال‌بک برمی‌گردد.

## API

همهٔ مسیرها زیر `v2/bnpl` هستند و پاسخ همیشه `{ statusCode, message, data }` است.

| بخش | مسیرها | احراز |
|---|---|---|
| مشتری | `Config`، `Quote`، `Profile` (+ `Info`، `Address`)، `Requests` (ثبت، ناتمام، تاریخچه، جزئیات، `Discount`، `AcceptTerms`، `Sign`، `Cancel`، `Payments`)، `Payments` (فهرست، رسید، `Gateways`) | هدر `Token` (یا `Authorization: Bearer`) و `MobileNumber` اپ؛ Identity: `v2/InternalIdentity/Authenticate` |
| کال‌بک درگاه | `Payments/{id}/Callback` (GET و POST) | ندارد؛ نتیجه با Verify از خود درگاه گرفته می‌شود |
| پنل | `Admin/Dashboard`، `Admin/Requests` (جستجو، جزئیات، `Terminate`، `Installments/{n}/WaivePenalty`، `ManualPayments`)، `Admin/Payments` (+ `Refunded`)، `Admin/Settings`، `Admin/Plans`، `Admin/Discounts` | کوکی `token` یا Bearer پنل؛ Identity: `Internal/Authorization/Resolve` و کلیدهای `bnpl.*` |

## تنظیمات

| کلید | توضیح |
|---|---|
| `ConnectionStrings:Bnpl` و `Bnpl:DatabaseProvider` | `SqlServer` (پیش‌فرض) یا `Sqlite` برای اجرای محلی |
| `Bnpl:ApplyMigrationsOnStartup` | اعمال Migrationها هنگام شروع (در Development همیشه) |
| `Bnpl:PublicBaseUrl` | آدرس عمومی سرویس؛ کال‌بک درگاه از روی آن ساخته می‌شود |
| `Bnpl:AllowedReturnUrlPrefixes` و `Bnpl:DefaultReturnUrl` | آدرس‌های مجاز بازگشت به اپ پس از درگاه |
| `Bnpl:Providers:{Price,Profile,Gateway,Custody}` | `DayaDiamond` (پیش‌فرض) یا `Sandbox`. Sandbox در Production خطا می‌دهد |
| `Bnpl:Sandbox` | قیمت ثابت، موجودی اولیهٔ توکن و کامل بودن پروفایل در حالت Sandbox |
| `Bnpl:Maintenance` | `Enabled`، `IntervalSeconds` (۶۰) و `BatchSize` (۱۰۰) برای Worker |
| `Bnpl:CustomerAuth` | نام هدرهای توکن و موبایل مشتری |
| `Identity:BaseUrl`، `InternalApi:Key` و `InternalApi:HeaderName` | آدرس Identity و کلید داخلی (پیش‌فرض هدر `X-Api-Key`) |
| `DayaDiamond:*` | آدرس، نام کاربری، رمز و کلید AES. مقدار محرمانه را با user-secrets یا متغیر محیطی بدهید |

## استقرار

1. روی دیتابیس BNPL: `Database/bnpl_migrations.sql`، یا `Bnpl:ApplyMigrationsOnStartup = true`.
2. روی دیتابیس Identity: `Database/identity/bnpl_panel_permissions.sql`. این اسکریپت ۱۱ کلید پنل را می‌سازد و به نقش `admin` می‌دهد.
3. بعد از هر تغییر مدل: `dotnet ef migrations add <Name> -p src/BnplService.Infrastructure` و بعد `UPDATE_SQL=1 dotnet test --filter DatabaseScriptTests`.

## فرض‌هایی که باید تأیید شود

- نام هدرهای توکن و موبایل مشتری (`Token` و `MobileNumber`) و قالب پاسخ `InternalIdentity/Authenticate` با پکیج `OmidBank.Common` چک شود. هر دو از تنظیمات عوض می‌شوند.
- قالب پاسخ `Internal/Authorization/Resolve` همان فرض Facility است.
- قیمت دایادایموند تومان است و در BNPL به ریال (× ۱۰) تبدیل می‌شود.

## سؤال‌های باز از دایادایموند

در مستندات دایادایموند API مستندی برای این کارها پیدا نکردیم:

- پرداخت قسط از درگاه و verify آن
- قفل، آزادسازی و برداشت توکن کاربر

برای همین در کد به شکل پورت (`IBnplPaymentGateway` و `ITokenCustody`) تعریف شده‌اند.
پیاده‌سازی دایادایموندِ این دو فعلاً خطای روشن می‌دهد. فهرست درگاه‌ها از `gateway/list` واقعی گرفته می‌شود.
تا API برسد، از Sandbox استفاده کنید. وقتی مستند رسید، فقط `DayaDiamondPaymentGateway` و `DayaDiamondTokenCustody` باید کامل شوند.
قفل نشدن توکن جلوی فعال‌سازی قرارداد را نمی‌گیرد و Worker آن را دوباره امتحان می‌کند.
