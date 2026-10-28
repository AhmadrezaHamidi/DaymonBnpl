namespace BnplService.Application.Handler.Admin.Dashboard.Queries.GetDashboard;

/// <summary>داشبورد پنل: تعداد قراردادها در هر وضعیت، اصل فعال، وصولی، مانده، معوق و برگشت‌های لازم.</summary>
public sealed record GetDashboardQuery : BnplBaseQuery<BnplMessageResponse<BnplDashboardDto>>;
