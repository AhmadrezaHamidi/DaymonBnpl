/* =====================================================================
   کاتالوگ مجوزهای پنل BNPL (پیش‌خرید اقساطی) در Identity + انتساب به نقش ادمین
   روی دیتابیس Identity اجرا شود (همان‌جا که dbo.PanelMenu و dbo.RolePanelMenu هستند).

   ساختار مثل 02_seed_permissions_and_matrix.sql: هر تب یک ردیف Kind=1 با کلید «.view» و Url،
   عملیات‌های آن تب ردیف‌های فرزند Kind=2 بدون Url.
   کلیدها عیناً با BnplService.Application.Authorization.BnplPermissionKeys یکی هستند
   (تست BnplPermissionScriptTests این را چک می‌کند). کلید منتشرشده هرگز تغییر نام نمی‌دهد.

   پیش‌نیاز: identityservice/Database/01_panel_authorization.sql
   قابل اجرای چندباره (idempotent). فقط اضافه می‌کند؛ انتساب دیگر نقش‌ها را پاک نمی‌کند.
   ===================================================================== */

SET XACT_ABORT ON;
BEGIN TRAN;

IF COL_LENGTH('dbo.PanelMenu', 'PermissionKey') IS NULL
    THROW 50010, N'اول 01_panel_authorization.sql را اجرا کن.', 1;

DECLARE @cat TABLE
(
    [Key]      VARCHAR(150) NOT NULL PRIMARY KEY,
    ParentKey  VARCHAR(150) NULL,
    Kind       TINYINT      NOT NULL,
    Title      NVARCHAR(150) NOT NULL,
    Url        VARCHAR(200) NULL,
    SortOrder  INT          NOT NULL
);

INSERT @cat ([Key], ParentKey, Kind, Title, Url, SortOrder) VALUES
-- داشبورد پیش‌خرید ----------------------------------------------------
 ('bnpl.dashboard.view',      NULL,                   1, N'داشبورد پیش‌خرید اقساطی',        '/bnpl/dashboard', 2000),

-- قراردادها -----------------------------------------------------------
 ('bnpl.requests.view',       NULL,                   1, N'قراردادهای پیش‌خرید اقساطی',     '/bnpl/requests',  2100),
 ('bnpl.requests.terminate',  'bnpl.requests.view',   2, N'فسخ قرارداد',                    NULL,              2101),
 ('bnpl.penalties.waive',     'bnpl.requests.view',   2, N'بخشودگی جریمهٔ دیرکرد',           NULL,              2102),

-- پرداخت‌ها -----------------------------------------------------------
 ('bnpl.payments.view',       NULL,                   1, N'پرداخت‌های پیش‌خرید اقساطی',      '/bnpl/payments',  2200),
 ('bnpl.payments.manual',     'bnpl.payments.view',   2, N'ثبت پرداخت دستی قسط',            NULL,              2201),
 ('bnpl.payments.refund',     'bnpl.payments.view',   2, N'ثبت برگشت وجه',                  NULL,              2202),

-- تنظیمات و طرح‌ها ----------------------------------------------------
 ('bnpl.settings.view',       NULL,                   1, N'تنظیمات پیش‌خرید اقساطی',        '/bnpl/settings',  2300),
 ('bnpl.settings.edit',       'bnpl.settings.view',   2, N'ویرایش تنظیمات و طرح‌های اقساط',  NULL,              2301),

-- کدهای تخفیف ---------------------------------------------------------
 ('bnpl.discounts.view',      NULL,                   1, N'کدهای تخفیف پیش‌خرید',           '/bnpl/discounts', 2400),
 ('bnpl.discounts.edit',      'bnpl.discounts.view',  2, N'تعریف و ویرایش کد تخفیف',        NULL,              2401);

/* ردیف‌های ریشه (تب‌ها) اول، تا ParentId فرزندها قابل تنظیم باشد */
MERGE dbo.PanelMenu AS t
USING (SELECT * FROM @cat WHERE ParentKey IS NULL) AS s
ON t.PermissionKey = s.[Key]
WHEN MATCHED THEN
    UPDATE SET t.Title = s.Title, t.Url = s.Url, t.[Kind] = s.Kind,
               t.SortOrder = s.SortOrder, t.IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (ParentId, Title, Url, IsRootMenu, IsInternalMenu, CreationTime, IsActive, PermissionKey, [Kind], SortOrder)
    VALUES (NULL, s.Title, s.Url, 1, 0, SYSUTCDATETIME(), 1, s.[Key], s.Kind, s.SortOrder);

MERGE dbo.PanelMenu AS t
USING (
    SELECT c.[Key], c.Title, c.Url, c.Kind, c.SortOrder, p.Id AS ParentId
    FROM @cat c
    JOIN dbo.PanelMenu p ON p.PermissionKey = c.ParentKey
    WHERE c.ParentKey IS NOT NULL
) AS s
ON t.PermissionKey = s.[Key]
WHEN MATCHED THEN
    UPDATE SET t.Title = s.Title, t.Url = s.Url, t.[Kind] = s.Kind,
               t.SortOrder = s.SortOrder, t.ParentId = s.ParentId, t.IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (ParentId, Title, Url, IsRootMenu, IsInternalMenu, CreationTime, IsActive, PermissionKey, [Kind], SortOrder)
    VALUES (s.ParentId, s.Title, s.Url, 0, 0, SYSUTCDATETIME(), 1, s.[Key], s.Kind, s.SortOrder);

/* نقش سیستمی ادمین همهٔ مجوزهای BNPL را می‌گیرد. بقیهٔ نقش‌ها را ادمین از پنل دسترسی می‌دهد. */
DECLARE @added TABLE (RoleId INT NOT NULL);

MERGE dbo.RolePanelMenu AS t
USING (
    SELECT r.Id AS RoleId, pm.Id AS PanelMenuId
    FROM @cat c
    JOIN dbo.PanelMenu pm ON pm.PermissionKey = c.[Key]
    JOIN dbo.[Role]    r  ON r.Title = N'admin' AND r.IsSystem = 1
) AS s
ON t.RoleId = s.RoleId AND t.PanelMenuId = s.PanelMenuId
WHEN NOT MATCHED THEN
    INSERT (RoleId, PanelMenuId) VALUES (s.RoleId, s.PanelMenuId)
OUTPUT inserted.RoleId INTO @added;

/* مجوز نقش ادمین عوض شد ← نشست‌های فعلی آن کاربران باید باطل شوند تا مجوز تازه را بگیرند */
UPDATE u
SET u.PermissionVersion = u.PermissionVersion + 1,
    u.LastPermissionChangeAt = SYSUTCDATETIME()
FROM dbo.PanelUser u
WHERE EXISTS (
    SELECT 1 FROM dbo.PanelUserRole ur
    WHERE ur.PanelUserId = u.Id AND ur.RoleId IN (SELECT RoleId FROM @added)
);

COMMIT;
GO
