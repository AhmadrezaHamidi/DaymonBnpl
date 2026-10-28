using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Customer.Requests.Commands.CreateRequest;

/// <summary>
/// «ثبت درخواست پیش‌خرید اقساطی». شرط‌ها: پروفایل و نشانی نزد دایادایموند کامل باشد، درخواست تکمیل‌نشدهٔ دیگری نباشد
/// («یک درخواست تکمیل نشده دارید») و از سقف قرارداد فعال مشتری بیشتر نشود.
/// </summary>
public sealed record CreateRequestCommand : BnplBaseCommand<BnplMessageResponse<BnplRequestDetailsDto>>
{
    [Range(1, long.MaxValue)] public long Amount { get; init; }

    /// <summary>خالی ← اولین طرح فعال.</summary>
    public int? PlanId { get; init; }
}
