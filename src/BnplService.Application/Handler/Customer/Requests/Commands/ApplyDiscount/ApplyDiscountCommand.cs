using System.ComponentModel.DataAnnotations;
using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.ApplyDiscount;

/// <summary>«کد تخفیف» صفحهٔ قرارداد؛ Code خالی یعنی حذف کد. قیمت توکن هم تازه می‌شود. فقط قبل از امضا.</summary>
public sealed record ApplyDiscountCommand : CustomerRequestCommand
{
    [StringLength(30)] public string? Code { get; init; }
}
