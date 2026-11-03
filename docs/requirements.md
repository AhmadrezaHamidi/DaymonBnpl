# سند نیازمندی‌های پیش‌خرید اقساطی (BNPL) و نگاشت آن به کد

نسخهٔ سند: ۱۴۰۵/۰۸/۱۲ (۲۰۲۶-۱۱-۰۳) · مبنای کد: کامیت `4ab05fb` روی `main`

این سند می‌گوید سرویس `DaymonBnpl` الان دقیقاً چه کاری می‌کند. هر نیازمندی به منبعش (فیگما، یوزر استوری یا تصمیم پروژه)،
به کدی که آن را پیاده کرده و به تستی که آن را بررسی می‌کند وصل شده است. آخر سند، فاصلهٔ کد با نیازمندی‌ها و سؤال‌های باز آمده است.

## ۱. منابع نیازمندی

| کد منبع | منبع | توضیح |
|---|---|---|
| **F** | [فیگمای اپ](figma/diamond-trading-figma.pdf) | بخش پیش‌خرید اقساطی، اعداد «شرایط وام» و متن قرارداد (ماده ۵ و ۶) |
| **US** | یوزر استوری الماس امید (`omid_diamond_user_stories.docx`) | بخش ۳ (تکمیل اطلاعات کاربری)، بخش ۴ (پیش‌خرید اقساطی) و US-5 (باکس پیام) |
| **D** | تصمیم‌های پروژه | مدل وثیقه (تأیید مهسا، ۱۴۰۵/۰۸/۱۰) و تصمیم‌های فنی |
| **API** | [مستند API دایادایموند](dayadiamond-api.md) | سرویس‌های مستند (لاگین، قیمت، درگاه، پروفایل، خرید) |

بخش ۱ و ۲ یوزر استوری (خرید و فروش توکن، تسهیلات نقدی) و یوزر استوری «دارایی مس» در دامنهٔ این سرویس نیستند.

## ۲. مدل کسب‌وکار

- **خرید عادی** یعنی مشتری پول می‌دهد و توکن می‌خرد. این کار با API مستند دایادایموند (`buy/request`) انجام می‌شود و جزو این سرویس نیست.
- **پیش‌خرید اقساطی** یعنی مشتری توکن‌هایی را که از قبل دارد وثیقه می‌گذارد:
  1. توکن آزاد مشتری به‌اندازهٔ وثیقه مسدود می‌شود و در برابرش اعتبار خرید می‌گیرد.
  2. با این اعتبار، توکن جدید خریده می‌شود و پولش را قسطی پس می‌دهد.
  3. بعد از پرداخت آخرین قسط، وثیقه آزاد می‌شود.

پیاده‌سازی: [`BnplRequest`](../src/BnplService.Domain/Entity/Requests/BnplRequest.cs) (aggregate اصلی) و پورت
[`ITokenCustody`](../src/BnplService.Contracts/Providers/IBnplProviders.cs).

## ۳. چرخهٔ عمر درخواست

```
Draft ──AcceptTerms──► TermsAccepted ──Sign (+قفل وثیقه)──► Signed ──پرداخت قسط اول──► Active ──آخرین قسط──► Settled
  │                        │                                 │                            │
  └──── Cancel / Expire ───┴──────── Cancel / Expire ────────┘                            └── Terminate (ادمین)
```

| وضعیت | معنی | کجا عوض می‌شود |
|---|---|---|
| `Draft` | مبلغ و طرح انتخاب شده | `BnplRequest.Create` |
| `TermsAccepted` | شرایط تأیید شده | `BnplRequest.AcceptTerms` |
| `Signed` | قرارداد امضا شده، وثیقه قفل شده و مهلت پرداخت قسط اول شروع شده | `BnplRequest.Sign` و `MarkCollateralLocked` |
| `Active` | قسط اول پرداخت شده، تعداد توکن قطعی شده و توکن واریز شده | `BnplRequest.ApplyPayment` |
| `Settled` | همهٔ اقساط پرداخت شده و وثیقه آزاد می‌شود | `BnplRequest.ApplyPayment` |
| `Cancelled` | مشتری درخواست ناتمام را لغو کرده | `BnplRequest.Cancel` |
| `Expired` | مهلت تکمیل یا مهلت پرداخت قسط اول گذشته | `BnplRequest.ExpireIfDue` |
| `Terminated` | ادمین قرارداد فعال را خاتمه داده (نکول)؛ وثیقه مسدود می‌ماند | `BnplRequest.Terminate` |

تعریف وضعیت‌ها در [`BnplEnums.cs`](../src/BnplService.Domain/Enums/BnplEnums.cs) است. هر تغییر وضعیت در
[`BnplRequestHistory`](../src/BnplService.Domain/Entity/Requests/BnplRequestHistory.cs) ثبت می‌شود و می‌گوید چه کسی (مشتری، ادمین یا سیستم) آن را انجام داده است.

## ۴. نیازمندی‌های کارکردی

ستون «وضعیت»: ✅ کامل · ⚠️ ناقص یا با فرض · ❌ پیاده نشده

مسیر Handlerها نسبت به `src/BnplService.Application/Handler/` نوشته شده است.

### ۴.۱ تکمیل اطلاعات کاربری (US-3-01، US-3-02، US-4-04)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-PRF-01 | فرم اطلاعات هویتی: نام، نام خانوادگی، نام پدر، تاریخ تولد شمسی، کد ملی، جنسیت، شبا. موبایل از کاربر لاگین‌شده می‌آید و قابل ویرایش نیست | US-3-01 | `Customer/Profile/Commands/UpsertProfileInfo` ← دایادایموند `profile/upsertInfo` | `Encrypted_mobile_registers_the_user_and_profile_forms_complete_it` | ✅ |
| R-PRF-02 | کد ملی ۱۰ رقمی با رقم کنترل و شبا (IR + ۲۴ رقم با کنترل mod 97) بررسی شود و خطای فارسی برگردد | US-3-01 · F | `NationalCodeRule`، `ShebaRule` در [`IdentityRules.cs`](../src/BnplService.Domain/Helpers/IdentityRules.cs) | `Profile_info_rejects_an_invalid_national_code_and_sheba` | ✅ |
| R-PRF-03 | فرم نشانی: استان، شهر، آدرس (حداکثر ۲۶۰ حرف)، کد پستی ۱۰ رقمی، تلفن ثابت با پیش‌شماره | US-3-02 · F | `Customer/Profile/Commands/UpsertProfileAddress` ← `profile/upsertAddress` | `Address_province_comes_from_the_list_and_phone_matches_its_area_code` | ✅ |
| R-PRF-04 | استان از فهرست ۳۱ استان انتخاب شود و تلفن ثابت با پیش‌شمارهٔ همان استان شروع شود | US-3-02 | `Customer/Profile/Queries/GetProvinces`، [`IranProvinces`](../src/BnplService.Domain/Helpers/IranProvinces.cs) | همان | ✅ |
| R-PRF-05 | شهرها با انتخاب استان فیلتر شوند | US-3-02 | شهر فعلاً متن آزاد است | — | ❌ |
| R-PRF-06 | کاربر اطلاعات و نشانی ثبت‌شده را ببیند و ویرایش کند | US-3-01 · US-3-02 | `Customer/Profile/Queries/GetProfile` از روی رونوشت [`CustomerProfile`](../src/BnplService.Domain/Entity/Customers/CustomerProfile.cs)، چون دایادایموند API خواندن پروفایل ندارد | `Registered_profile_and_address_can_be_viewed_for_editing` | ⚠️ |
| R-PRF-07 | وضعیت کامل بودن پروفایل و نشانی برای دکمه‌های «تکمیل اطلاعات کاربری» و «تکمیل آدرس» برگردد | US-4-04 · US-4-05 | `Customer/Profile/Queries/GetProfileStatus` ← `user/getOrAdd` (موبایل رمزنگاری‌شده) | `Encrypted_mobile_registers_...` | ✅ |
| R-PRF-08 | بدون پروفایل و نشانی کامل، درخواست پیش‌خرید ثبت نشود (خطای 412) | F | `CreateRequestCommandHandler` | `Incomplete_profile_blocks_a_new_request` | ✅ |

### ۴.۲ صفحهٔ درخواست جدید و محاسبهٔ شرایط (US-4-02)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-CFG-01 | اسلایدر مبلغ از ۵۰ میلیون تا ۵ میلیارد ریال، با گام ۱۰ میلیون ریال | F | `BnplSettings.EnsureAmountAllowed`، `Customer/Config/Queries/GetConfig` | `Amount_outside_the_slider_is_rejected` | ✅ |
| R-CFG-02 | مبلغ‌های پیشنهادی را ادمین تعیین می‌کند | US-4-02 | `BnplSettings.SuggestedAmounts` ← `GetConfig` | `Suggested_amounts_come_from_admin_settings` | ✅ |
| R-CFG-03 | با انتخاب مبلغ، این‌ها محاسبه شوند: تعداد اقساط، کارمزد، قسط اول + کارمزد، بقیهٔ اقساط، جمع بازپرداخت، تعداد توکن، توکن وثیقه و زمان شروع اقساط | US-4-02 · F | [`BnplQuote.Calculate`](../src/BnplService.Domain/Rules/BnplQuote.cs)، `Customer/Config/Queries/GetQuote`، `QuoteDto` | `Fifty_million_matches_figma`، `Two_hundred_million_matches_figma`، `Installments_sum_to_total_repayment` | ✅ |
| R-CFG-04 | محاسبه با قیمت لحظه‌ای توکن انجام شود | F · API | `ITokenPriceProvider` ← `price/token` (تومان به ریال تبدیل می‌شود) | `Config_and_quote_use_the_live_token_price`، `Login_and_price_go_through_the_jwt_and_toman_is_converted_to_rial` | ✅ |
| R-CFG-05 | مبلغ‌ها به ریال و معادل تومان نمایش داده شوند | US-4-02 | API همه‌چیز را به ریال می‌دهد و تبدیل به تومان (÷۱۰) کار اپ است | — | ⚠️ |
| R-CFG-06 | اگر ادمین پیش‌خرید را خاموش کرده باشد، ثبت درخواست جدید بسته شود ولی قراردادهای موجود ادامه پیدا کنند | D | `BnplSettings.IsEnabled` | `Settings_and_plans_are_editable_and_disable_new_requests` | ✅ |

### ۴.۳ ثبت و تکمیل درخواست (US-4-03، US-4-05)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-REQ-01 | ثبت درخواست با مبلغ و طرح، شمارهٔ قرارداد `P5_U` و ۷ رقم، و ساخت جدول اقساط | F | `Customer/Requests/Commands/CreateRequest`، `BnplRequest.Create` | `Full_contract_from_request_to_settlement` | ✅ |
| R-REQ-02 | درصدها و مبلغ‌های طرح روی قرارداد کپی شوند تا تغییر بعدی طرح روی قراردادهای قبلی اثر نگذارد | D | `BnplRequest.ApplyQuote` | `Settings_and_plans_are_editable_...` | ✅ |
| R-REQ-03 | مرور توضیحات طرح و تأیید صریح شرایط، قبل از امضا | US-4-03 · F | `Customer/Requests/Commands/AcceptTerms`، `BnplRequest.AcceptTerms` | `Full_contract_...` | ✅ |
| R-REQ-04 | بازگشت و تغییر شرایط بدون از دست رفتن انتخاب‌ها | US-4-03 | فقط کد تخفیف قبل از امضا عوض می‌شود. برای تغییر مبلغ یا طرح باید درخواست لغو و دوباره ثبت شود | — | ⚠️ |
| R-REQ-05 | فهرست درخواست‌های ناتمام با مبلغ، کارمزد، مهلت تکمیل و شناسه | US-4-05 | `SearchMyRequests` با `Filter = Incomplete`، `BnplRequestListItemDto.Deadline` | `Incomplete_requests_are_listed_up_to_the_limit` | ✅ |
| R-REQ-06 | حداکثر درخواست ناتمام هم‌زمان (پیش‌فرض ۳، قابل تنظیم). سقف پر باشد، خطای 409 برمی‌گردد | US-4-05 · D | `CreateRequestCommandHandler`، `BnplSettings.MaxIncompleteRequestsPerCustomer` | همان | ✅ |
| R-REQ-07 | حداکثر قرارداد فعال هم‌زمان (پیش‌فرض ۳) | D | `BnplSettings.MaxActiveContractsPerCustomer` | — | ✅ |
| R-REQ-08 | «حذف درخواست» برای هر درخواست ناتمام. اگر قرارداد امضا شده باشد، وثیقه آزاد می‌شود | US-4-05 | `Customer/Requests/Commands/CancelRequest`، `BnplRequest.Cancel` | `Cancelled_request_frees_the_slot_for_a_new_one`، `Cancelling_a_signed_contract_releases_the_collateral` | ✅ |
| R-REQ-09 | درخواست ناتمام بعد از ۲۴ ساعت منقضی شود | F · D | `BnplRequest.ExpireIfDue` موقع خواندن و در Worker | `Unfinished_request_expires_after_the_completion_window` | ✅ |
| R-REQ-10 | مشتری فقط درخواست‌های خودش را ببیند | D | فیلتر `CustomerId` در `RequestAsync` | `Customers_cannot_see_each_others_requests` | ✅ |

### ۴.۴ کد تخفیف (US-4-05، ماده ۵ قرارداد)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-DSC-01 | ثبت یا حذف کد تخفیف در درخواست ناتمام، فقط قبل از امضا | US-4-05 | `Customer/Requests/Commands/ApplyDiscount`، `BnplRequest.Requote` | `Discount_code_reduces_the_last_installment_and_is_counted_on_activation` | ✅ |
| R-DSC-02 | تخفیف فقط از قسط آخر کم شود و هیچ‌وقت آن را منفی نکند | F (ماده ۵) | `BnplQuote.Calculate` | `Discount_reduces_only_the_last_installment`، `Discount_never_makes_the_last_installment_negative`، `Single_installment_...` | ✅ |
| R-DSC-03 | تخفیف یا درصدی از کل کارمزد است (با سقف اختیاری) یا مبلغ ثابت | D | [`DiscountCode.AmountFor`](../src/BnplService.Domain/Entity/Settings/DiscountCode.cs) | `Percent_discount_code_is_taken_from_total_fee_and_capped`، `Discount_code_must_be_either_percent_or_fixed` | ✅ |
| R-DSC-04 | کد نادرست، منقضی، هنوز شروع‌نشده یا تمام‌شده، پیام متناسب با علت بدهد و مبلغ‌ها عوض نشوند | US-4-05 | `DiscountCode.EnsureUsable` (قبل از `Requote` و دوباره موقع امضا) | `Discount_code_usage_limit_is_enforced` | ✅ |
| R-DSC-05 | ظرفیت کد با فعال شدن قرارداد مصرف شود، نه با ثبت درخواست | D | `BnplPaymentProcessor.ApplyAsync` ← `MarkUsed` | `Discount_code_reduces_...` | ✅ |

### ۴.۵ وثیقه و امضای قرارداد (US-4-05، مدل وثیقه)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-COL-01 | با «ادامه»، نشان داده شود توکن آزاد برای وثیقه کافی است یا نه. اگر نیست، پیام «تعداد n توکن کم دارید» و مسیر خرید توکن | US-4-05 · D | `Customer/Requests/Queries/GetCollateralStatus`، `CollateralStatusDto` | `Continue_shows_the_collateral_shortfall_and_sign_is_refused` | ✅ |
| R-COL-02 | کمبود وثیقه با خرید عادی توکن (API مستند `buy/request`) جبران شود | US-4-05 | سمت اپ و کلاینت `DayaDiamond.Client` | `Collateral_shortfall_is_fixed_by_buying_tokens_through_the_documented_buy_api` | ✅ |
| R-COL-03 | وثیقه پیش‌فرض ۱۰۰٪ مبلغ پیش‌خرید است و ادمین آن را بین ۱ تا ۵۰۰٪ تنظیم می‌کند | D | `BnplSettings.CollateralPercent`، `BnplRequest.RequiredCollateral` | `Settings_and_plans_are_editable_...` | ⚠️ (درصد هنوز تأیید نشده) |
| R-COL-04 | موقع امضا، تعداد وثیقه با قیمت همان لحظه قطعی شود. اگر توکن آزاد کافی نباشد، امضا با خطای 412 رد شود | D | `BnplRequest.Sign` | `Continue_shows_...` | ✅ |
| R-COL-05 | وثیقه هم‌زمان با امضا نزد دایادایموند قفل شود. اگر قفل رد شد، امضا ثبت نشود. اگر ذخیرهٔ امضا شکست خورد، قفل برگردانده شود | D | `SignContractCommandHandler` ← `ITokenCustody.LockAsync` / `ReleaseAsync` | `Collateral_lock_failure_keeps_the_contract_unsigned`، `DayaDiamond_outage_at_signing_leaves_nothing_locked` | ✅ |
| R-COL-06 | بعد از امضا، ۳۰ دقیقه برای پرداخت قسط اول فرصت هست. بعد از آن قرارداد منقضی و وثیقه آزاد می‌شود | F | `BnplSettings.ActivationMinutes`، `BnplRequest.IsDue`، Worker | `First_installment_must_be_paid_within_the_activation_window`، `Expired_signed_contract_releases_the_collateral_in_maintenance` | ✅ |
| R-COL-07 | وثیقه بعد از تسویه آزاد شود | D | `BnplPaymentProcessor.SyncCustodyAsync` | `Whole_contract_runs_against_the_mock_from_profile_to_collateral_release` | ✅ |
| R-COL-08 | در خاتمه یا نکول، وثیقه مسدود بماند تا ادمین تعیین تکلیف کند | D | `BnplRequest.NeedsCollateralRelease` | — | ⚠️ (فرایند تعیین تکلیف وثیقه تعریف نشده) |

### ۴.۶ پرداخت اقساط

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-PAY-01 | فهرست درگاه‌های پرداخت | API | `Customer/Payments/Queries/GetGateways` ← `gateway/list` | `Full_contract_...` | ✅ |
| R-PAY-02 | پرداخت قسط از درگاه: انتقال به PayUrl، کال‌بک و استعلام نتیجه از خود درگاه (به پارامترهای مرورگر اعتماد نمی‌شود) | F | `StartPayment` (Gateway)، `CompleteGatewayPayment`، [`BnplGatewayVerifier`](../src/BnplService.Application/Handler/Shared/BnplGatewayVerifier.cs) | `Failed_gateway_payment_leaves_the_installment_unpaid_and_callback_is_idempotent`، `Cancelled_gateway_payment_fails_and_unfinished_one_stays_pending` | ⚠️ (API پیشنهادی) |
| R-PAY-03 | آدرس بازگشت اپ باید در لیست مجاز باشد | D | `BnplOptions.ResolveReturnUrl` (`Bnpl:AllowedReturnUrlPrefixes`) | `Return_url_must_be_whitelisted` | ✅ |
| R-PAY-04 | قسط اول فقط از درگاه پرداخت شود و قرارداد را فعال کند | F · D | `StartPaymentCommandHandler.TokenAsync` | `Activation_cannot_be_paid_with_tokens_and_installments_go_in_order` | ✅ |
| R-PAY-05 | با فعال شدن، تعداد توکن خریدنی با قیمت لحظهٔ پرداخت قسط اول قطعی شود، سررسیدها از همان روز ماه‌به‌ماه (شمسی) چیده شوند و توکن به حساب مشتری واریز شود | F · D | `BnplRequest.ApplyPayment`، `ITokenCustody.CreditAsync`، [`Jalali.AddMonths`](../src/BnplService.Domain/Helpers/Jalali.cs) | `Jalali_monthly_due_dates_keep_the_day`، `Token_delivery_failure_does_not_block_activation_and_is_retried_later` | ⚠️ (فرض: کل توکن بعد از قسط اول تحویل می‌شود) |
| R-PAY-06 | پرداخت توکنی قسط‌های بعدی، از موجودی آزاد و با قیمت لحظه‌ای | F | `StartPaymentCommandHandler.TokenAsync` ← `ITokenCustody.DebitAsync` | `Token_payment_needs_enough_free_balance` | ⚠️ (API پیشنهادی) |
| R-PAY-07 | اقساط به‌ترتیب پرداخت شوند و پرداخت جزئی نداریم | D | `BnplRequest.PayableInstallment` | `Activation_cannot_be_paid_...` | ✅ |
| R-PAY-08 | مهلت قسط اول با زمان شروع پرداخت سنجیده شود، نه زمان رسیدن کال‌بک | D | پارامتر `paymentStartedAt` در `PayableInstallment` | `Gateway_result_counts_from_payment_start_not_callback_time` | ✅ |
| R-PAY-09 | پولی که دیر یا تکراری رسیده (قرارداد لغو یا منقضی شده، یا قسط قبلاً پرداخت شده) به وضعیت «نیاز به استرداد» برود | D | `BnplPaymentProcessor.ApplyAsync` ← `BnplPayment.RequireRefund` | `Payment_that_arrives_after_cancellation_needs_refund` | ✅ |
| R-PAY-10 | پرداخت درگاهی که بعد از ۲۰ دقیقه نتیجه نداشته، دوباره استعلام و تعیین تکلیف شود | D | Worker ← `BnplGatewayVerifier`، `BnplSettings.GatewayPaymentMinutes` | `Maintenance_settles_a_paid_gateway_payment_whose_callback_never_came` | ✅ |
| R-PAY-11 | رسید پرداخت و فهرست پرداخت‌های مشتری | F | `Customer/Payments/Queries/GetMyPayment`، `SearchMyPayments`، `PaymentReceiptDto` | `Search_and_details_show_customer_payments_and_history` | ✅ |

### ۴.۷ جریمهٔ دیرکرد (ماده ۶ قرارداد)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-PEN-01 | روزانه ۱٪ مبلغ همان قسط. روز سررسید جریمه ندارد و از فردای آن به وقت تهران حساب می‌شود | F | [`LatePenalty`](../src/BnplService.Domain/Rules/LatePenalty.cs)، `BnplClock.Today` | `Late_installment_is_charged_one_percent_a_day`، `No_penalty_before_due_date` | ✅ |
| R-PEN-02 | جریمه موقع شروع پرداخت ثابت شود و بعد از پرداخت عوض نشود | D | `BnplPayment.Start`، `Installment.PenaltyPaid` | همان | ✅ |
| R-PEN-03 | ادمین جریمهٔ یک قسط را با ذکر دلیل ببخشد | D | `Admin/Requests/Commands/WaivePenalty` | `Overdue_filter_waive_penalty_and_manual_payment` | ✅ |

### ۴.۸ تب‌ها و جزئیات قرارداد (US-4-01، US-4-06، US-4-07)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-LST-01 | سه تب «درخواست»، «در جریان» و «تکمیل‌شده» | US-4-01 | `SearchMyRequests` با `Filter` = `Incomplete` / `Unsettled` / `Settled` | `Incomplete_requests_...`، `Full_contract_...` | ✅ (خود تب‌ها و پیام خالی بودن کار اپ است) |
| R-LST-02 | «در جریان»: وضعیت، مبلغ، شناسه، قسط بعدی و سررسید آن، و نشانهٔ معوق بودن | US-4-06 · F | `BnplRequestListItemDto.NextInstallment`، `IsOverdue` | `Late_installment_...` | ✅ |
| R-LST-03 | جزئیات با جدول اقساط پرداخت‌شده و مانده، درصد پرداخت، بدهی مانده، توکن متعلق به مشتری و تاریخچه | US-4-06 · F | `GetMyRequest`، `BnplRequestDetailsDto`، `OwnedTokenAmount`، `UnpaidDebt` | `Full_contract_...` | ✅ |
| R-LST-04 | «تکمیل‌شده»: وضعیت، مبلغ، شناسه و **تاریخ تکمیل** | US-4-07 | فهرست، تاریخ سررسید آخرین قسط (`EndDate`) را می‌دهد، نه زمان واقعی تسویه. `SettledAt` فقط در تاریخچهٔ جزئیات دیده می‌شود | — | ⚠️ |
| R-LST-05 | در جزئیات، آزاد شدن وثیقه بعد از تسویه دیده شود | US-4-07 | `BnplRequestDetailsDto.CollateralReleased` | `Whole_contract_..._collateral_release` | ✅ |

### ۴.۹ باکس پیام پویا (US-5)

| شناسه | نیازمندی | منبع | پیاده‌سازی | تست | وضعیت |
|---|---|---|---|---|---|
| R-MSG-01 | هر صفحه با یک کلید (مثل `bnpl.new-request`) پیام فعال خودش را بگیرد: متن، نوع (راهنما، هشدار یا اطلاع‌رسانی) و رنگ | US-5 | `Customer/Messages/Queries/GetPageMessage`، [`PageMessage`](../src/BnplService.Domain/Entity/Settings/PageMessage.cs) | `Page_message_box_shows_the_active_message_or_nothing` | ✅ |
| R-MSG-02 | اگر پیامی نباشد، چیزی برنگردد تا باکس نمایش داده نشود | US-5 | همان (پاسخ خالی) | همان | ✅ |
| R-MSG-03 | ادمین پیام‌ها را با بازهٔ نمایش و فعال/غیرفعال مدیریت کند | US-5 | `Admin/Messages/Commands/SavePageMessage`، `SearchPageMessages` | همان | ✅ |
| R-MSG-04 | خطای دریافت پیام صفحه را متوقف نکند | US-5 | کار اپ است | — | — |

### ۴.۱۰ پنل ادمین

| شناسه | نیازمندی | پیاده‌سازی | مجوز | تست | وضعیت |
|---|---|---|---|---|---|
| R-ADM-01 | داشبورد: تعداد قرارداد به تفکیک وضعیت، قراردادهای فعال و معوق، مبلغ وصول‌شده و جریمه، بدهی باز و معوق، توکن وثیقهٔ قفل‌شده، استردادهای باز، و درخواست و فعال‌سازی امروز | `Admin/Dashboard/Queries/GetDashboard` | `bnpl.dashboard.view` | `Dashboard_counts_contracts_and_money` | ✅ |
| R-ADM-02 | جستجو (از جمله فیلتر معوق) و جزئیات قرارداد با پرداخت‌ها و تاریخچه | `Admin/Requests/Queries/SearchRequests`، `GetRequest` | `bnpl.requests.view` | `Search_and_details_...` | ✅ |
| R-ADM-03 | خاتمهٔ قرارداد فعال با دلیل (۵ تا ۱۰۰۰ حرف) و نسخهٔ جاری ردیف | `Admin/Requests/Commands/TerminateRequest` | `bnpl.requests.terminate` | `Terminate_requires_reason_and_current_row_version` | ✅ |
| R-ADM-04 | بخشودگی جریمه | `WaivePenalty` | `bnpl.penalties.waive` | `Overdue_filter_...` | ✅ |
| R-ADM-05 | ثبت دستی پرداخت قسط با شماره پیگیری (این کار قسط اول را هم می‌پذیرد و قرارداد را فعال می‌کند) | `Admin/Payments/Commands/RegisterManualPayment` | `bnpl.payments.manual` | `Overdue_filter_...` | ✅ |
| R-ADM-06 | فهرست پرداخت‌ها و ثبت استرداد برای پرداخت «نیاز به استرداد» | `SearchPayments`، `MarkRefunded` | `bnpl.payments.view` و `bnpl.payments.refund` | `Payment_that_arrives_after_cancellation_needs_refund` | ✅ |
| R-ADM-07 | تنظیمات سراسری (بخش ۶) و طرح‌های اقساط | `Admin/Settings/Commands/UpdateSettings`، `SavePlan`، `GetSettings` | `bnpl.settings.view` و `bnpl.settings.edit` | `Settings_and_plans_are_editable_...`، `Settings_reject_amounts_off_the_slider_steps`، `Plan_rejects_upfront_fee_above_total_fee` | ✅ |
| R-ADM-08 | مدیریت کدهای تخفیف | `Admin/Discounts/Commands/SaveDiscount`، `SearchDiscounts` | `bnpl.discounts.view` و `bnpl.discounts.edit` | `Discount_code_...` | ✅ |
| R-ADM-09 | مدیریت باکس پیام | بخش ۴.۹ | `bnpl.messages.view` و `bnpl.messages.edit` | — | ✅ |

ارزیابی مجوز مثل Identity است: باید Allow داشته باشد و Deny سراسری نداشته باشد
([`PanelPermissions`](../src/BnplService.Application/Authorization/PanelPermissions.cs)، کلیدها در
[`BnplPermissionKeys`](../src/BnplService.Application/Authorization/BnplPermissionKeys.cs)).

### ۴.۱۱ کار دوره‌ای (Maintenance)

[`RunMaintenanceCommand`](../src/BnplService.Application/Handler/Maintenance/Commands/RunMaintenance/RunMaintenanceCommand.cs)
برای اجرای هر دقیقه طراحی شده و هر بار حداکثر ۱۰۰ مورد را پردازش می‌کند:

1. پرداخت‌های درگاهی مهلت‌گذشته‌ای که کال‌بکشان نرسیده را استعلام و اعمال می‌کند (R-PAY-10).
2. درخواست‌های ناتمام مهلت‌گذشته را منقضی می‌کند (R-REQ-09، R-COL-06).
3. واریز توکن یا آزادسازی وثیقه‌ای که قبلاً نزد دایادایموند شکست خورده را دوباره امتحان می‌کند (R-PAY-05، R-COL-07).

خود Worker که این Command را زمان‌بندی کند هنوز نوشته نشده است (بخش ۹).

## ۵. قواعد محاسبه

نمونهٔ فیگما: ۵۰٬۰۰۰٬۰۰۰ ریال، ۱۲ قسط، کارمزد کل ۲۲٪ که ۴٪ آن با قسط اول گرفته می‌شود.

| کمیت | فرمول | مقدار |
|---|---|---|
| کارمزد قسط اول | مبلغ × ۴٪ | ۲٬۰۰۰٬۰۰۰ |
| مبلغ هر قسط | ⌊(مبلغ + مبلغ × (۲۲٪ − ۴٪)) ÷ ۱۲⌋، رند به پایین تا ۱۰ ریال | ۴٬۹۱۶٬۶۶۰ |
| قسط اول | قسط + کارمزد قسط اول | ۶٬۹۱۶٬۶۶۰ |
| جمع بازپرداخت | قسط × ۱۲ + کارمزد قسط اول − تخفیف | ۶۰٬۹۹۹٬۹۲۰ |
| قسط آخر | قسط − تخفیف (حداقل صفر) | ۴٬۹۱۶٬۶۶۰ |
| تعداد توکن خریدنی | مبلغ ÷ قیمت توکن، رند به پایین تا ۳ رقم اعشار | با قیمت لحظهٔ پرداخت قسط اول قطعی می‌شود |
| توکن وثیقه | (مبلغ × درصد وثیقه) ÷ قیمت توکن، رند به بالا | با قیمت لحظهٔ امضا قطعی می‌شود |
| جریمهٔ دیرکرد | ⌊مبلغ قسط × ۱٪ × روزهای دیرکرد⌋ تا ۱۰ ریال | — |
| توکن متعلق به مشتری | توکن × اقساط پرداخت‌شده ÷ کل اقساط (بعد از تسویه: همهٔ توکن) | — |

کد: [`BnplQuote`](../src/BnplService.Domain/Rules/BnplQuote.cs)، [`Money`](../src/BnplService.Domain/Helpers/Money.cs)، [`LatePenalty`](../src/BnplService.Domain/Rules/LatePenalty.cs).
تست: [`QuoteTests`](../test/BnplService.Tests/Domain/QuoteTests.cs)، [`RulesTests`](../test/BnplService.Tests/Domain/RulesTests.cs).

## ۶. تنظیمات قابل تغییر از پنل

| تنظیم | پیش‌فرض | بازهٔ مجاز |
|---|---|---|
| فعال بودن ثبت درخواست | روشن | — |
| حداقل و حداکثر مبلغ | ۵۰ میلیون تا ۵ میلیارد ریال | حداقل مثبت و ≤ حداکثر |
| گام مبلغ | ۱۰ میلیون ریال | فاصلهٔ حداقل تا حداکثر بر آن بخش‌پذیر باشد |
| مهلت تکمیل درخواست | ۲۴ ساعت | ۱ تا ۷۲۰ |
| مهلت پرداخت قسط اول بعد از امضا | ۳۰ دقیقه | ۵ تا ۱۴۴۰ |
| مهلت نتیجهٔ درگاه | ۲۰ دقیقه | ۵ تا ۱۲۰ |
| حداکثر قرارداد فعال هر مشتری | ۳ | ۱ تا ۱۰۰ |
| حداکثر درخواست ناتمام هر مشتری | ۳ | ۱ تا ۲۰ |
| درصد وثیقه | ۱۰۰٪ | ۱ تا ۵۰۰ |
| مبلغ‌های پیشنهادی | ۵۰، ۱۰۰، ۲۰۰ و ۵۰۰ میلیون ریال | ۱ تا ۱۰ مبلغ، در بازه و مضرب گام |
| طرح اقساط | «۱۲ قسط»، کارمزد ۲۲٪، قسط اول ۴٪، جریمه ۱٪ | تعداد ۱ تا ۶۰، کارمزد ۰ تا ۱۰۰٪، جریمه ۰ تا ۱۰٪ |

کد: [`BnplSettings`](../src/BnplService.Domain/Entity/Settings/BnplSettings.cs)، [`BnplPlan`](../src/BnplService.Domain/Entity/Settings/BnplPlan.cs)، seed در [`BnplSeed`](../src/BnplService.Infrastructure/Seed/BnplSeed.cs).

## ۷. یکپارچگی با دایادایموند

| پورت | کاربرد | سرویس دایادایموند | وضعیت API |
|---|---|---|---|
| `ITokenPriceProvider` | قیمت لحظه‌ای | `GET price/token` | ✅ مستند |
| `ICustomerProfileProvider` | ثبت کاربر و پروفایل | `user/getOrAdd` (موبایل رمزنگاری‌شده)، `profile/upsertInfo`، `profile/upsertAddress` | ✅ مستند |
| `IBnplPaymentGateway.ListAsync` | فهرست درگاه | `gateway/list` | ✅ مستند |
| `IBnplPaymentGateway.StartAsync` / `VerifyAsync` | پرداخت قسط | `bnpl/payment/request`، `bnpl/payment/status` | ⚠️ پیشنهادی، فقط در mock |
| `ITokenCustody` | موجودی آزاد، قفل، آزادسازی، واریز، کسر | `bnpl/wallet/balance`، `lock`، `release`، `credit`، `debit` | ⚠️ پیشنهادی، فقط در mock |

- adapterها در [`DayaDiamondProviders.cs`](../src/BnplService.Infrastructure/Providers/DayaDiamond/DayaDiamondProviders.cs) هستند.
- کلاینت در [`src/DayaDiamond.Client`](../src/DayaDiamond.Client) است؛ سرویس‌های پیشنهادی در `IDayaDiamondBnplApi` تعریف شده‌اند.
- شبیه‌ساز در [`tools/DayaDiamond.Mock`](../tools/DayaDiamond.Mock/README.md) است.
- عملیات کیف توکن با `reference` (شمارهٔ قرارداد، یا قرارداد/شناسهٔ پرداخت) idempotent است (`Custody_is_idempotent_by_reference`).
- خطای دایادایموند به پیام فارسی تبدیل می‌شود (`Outage_becomes_a_persian_provider_error`).

## ۸. نیازمندی‌های غیرکارکردی

| موضوع | پیاده‌سازی |
|---|---|
| کنترل هم‌زمانی | هر Entity یک `Version` دارد ([`VersionedEntity`](../src/BnplService.Domain/Entity/Common/VersionedEntity.cs)). عملیات ادمین `RowVersion` می‌خواهد و نسخهٔ کهنه خطای conflict می‌دهد (`Stale_row_version_is_a_conflict`) |
| پایداری پول | بعد از کسر توکن یا رسیدن پول، ذخیره با `CancellationToken.None` انجام می‌شود و قطع شدن درخواست HTTP آن را نیمه‌کاره نمی‌گذارد. شکست سمت دایادایموند را Worker دوباره امتحان می‌کند |
| زمان و تقویم | همهٔ زمان‌ها UTC ذخیره می‌شوند. «امروز» و جریمه به وقت تهران حساب می‌شود و سررسیدها شمسی هستند ([`BnplClock`](../src/BnplService.Domain/Helpers/BnplClock.cs)، [`Jalali`](../src/BnplService.Domain/Helpers/Jalali.cs)) |
| پیام‌ها | همهٔ خطاهای کسب‌وکار فارسی و با کد HTTP مناسب هستند (400، 404، 409، 412، 502) ([`BnplDomainException`](../src/BnplService.Domain/Exceptions/BnplDomainException.cs)، `BnplExceptionBehavior`) |
| پایگاه داده | SQL Server (اسکیمای `bnpl`) با Migration، و SQLite برای تست ([`Database/bnpl_migrations.sql`](../Database/bnpl_migrations.sql)) |
| تست | ۷۶ تست روی SQLite و SQL Server؛ سناریوهای سرتاسری روی mock دایادایموند |

## ۹. فاصله‌ها و سؤال‌های باز

### پیاده نشده

1. **لایهٔ API** (کنترلرها، `[RequireBnplCustomer]` و `[RequireBnplPermission]` روی Identity `Internal/Authorization/Resolve`). Handlerها آماده‌اند ولی هنوز endpoint HTTP ندارند.
2. **Worker** که `RunMaintenanceCommand` را هر دقیقه اجرا کند.
3. **فهرست شهرهای هر استان** (R-PRF-05). برای این کار منبع داده لازم است.

### ناقص یا با فرض

4. **تغییر مبلغ یا طرح بعد از ثبت درخواست** (R-REQ-04). الان فقط کد تخفیف عوض می‌شود. اگر US-4-03 «بازگشت و تغییر شرایط» را برای درخواست ثبت‌شده هم می‌خواهد، یک Command «ویرایش درخواست» لازم است.
5. **تاریخ تکمیل در تب «تکمیل‌شده»** (R-LST-04). لازم است `SettledAt` به `BnplRequestListItemDto` اضافه شود.
6. **نمایش تومان** (R-CFG-05). باید روشن شود API معادل تومان را هم برگرداند یا اپ خودش حساب کند.
7. **رونوشت پروفایل** (R-PRF-06). اگر کاربر اطلاعاتش را بیرون از این سرویس عوض کند، رونوشت کهنه می‌ماند، چون دایادایموند API خواندن پروفایل ندارد.

### نیاز به تأیید کسب‌وکار

8. **درصد وثیقه.** آیا ۱۰۰٪ مبلغ پیش‌خرید درست است؟
9. **زمان تحویل توکن.** الان کل توکن بعد از قسط اول به حساب مشتری واریز می‌شود، ولی متن قرارداد می‌گوید «خریدار تنها مالک تعداد توکن متناسب با اقساط پرداخت‌شده می‌باشد» (`OwnedTokenAmount` فقط برای نمایش است). باید روشن شود توکن یک‌جا واریز شود یا به‌تدریج آزاد شود.
10. **تکلیف وثیقه در نکول** (R-COL-08). آیا وثیقه ضبط شود، فروخته شود، یا ادمین دستی آزادش کند؟ فعلاً هیچ عملیاتی برای آن نیست.

### نیاز به هماهنگی با دایادایموند

11. شکل نهایی سرویس‌های `bnpl/wallet/*` (موجودی آزاد، قفل، آزادسازی، واریز، کسر) و `bnpl/payment/*` (پرداخت قسط و استعلام آن). تا این‌ها تأیید نشوند، R-PAY-02، R-PAY-05، R-PAY-06 و R-COL-05 فقط روی mock کار می‌کنند.
