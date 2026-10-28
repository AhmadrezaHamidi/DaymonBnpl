namespace BnplService.Application.Authorization;

/// <summary>
/// کلیدهای مجوز پنل BNPL. باید با ردیف‌های PanelMenu در Identity یکی باشد
/// (Database/identity/bnpl_panel_permissions.sql آن‌ها را می‌سازد). کلید منتشرشده هرگز تغییر نام نمی‌دهد.
/// </summary>
public static class BnplPermissionKeys
{
    public const string DashboardView = "bnpl.dashboard.view";

    public const string RequestsView = "bnpl.requests.view";
    public const string RequestsTerminate = "bnpl.requests.terminate";
    public const string PenaltiesWaive = "bnpl.penalties.waive";

    public const string PaymentsView = "bnpl.payments.view";
    public const string PaymentsManual = "bnpl.payments.manual";
    public const string PaymentsRefund = "bnpl.payments.refund";

    public const string SettingsView = "bnpl.settings.view";
    public const string SettingsEdit = "bnpl.settings.edit";

    public const string DiscountsView = "bnpl.discounts.view";
    public const string DiscountsEdit = "bnpl.discounts.edit";

    public static IReadOnlyList<string> All { get; } =
    [
        DashboardView, RequestsView, RequestsTerminate, PenaltiesWaive, PaymentsView, PaymentsManual, PaymentsRefund,
        SettingsView, SettingsEdit, DiscountsView, DiscountsEdit,
    ];
}
