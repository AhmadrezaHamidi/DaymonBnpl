using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BnplService.Application.Handler.Customer.Requests.Shared;

/// <summary>پایهٔ Commandهای یک درخواست مشخص مشتری: Id از مسیر، RowVersion از آخرین پاسخ (همزمانی خوش‌بینانه).</summary>
public abstract record CustomerRequestCommand : BnplBaseCommand<BnplMessageResponse<BnplRequestDetailsDto>>
{
    [JsonIgnore, BindNever] public long Id { get; init; }

    /// <summary>اختیاری؛ اگر آمد و با نسخهٔ فعلی نخواند ← 409.</summary>
    [StringLength(20)] public string? RowVersion { get; init; }
}

/// <summary>بارگذاری درخواست خودِ مشتری، منقضی کردن در صورت گذشتن مهلت، چک نسخه.</summary>
public abstract class CustomerRequestHandlerBase(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time) : BnplHandlerBase(db, time)
{
    protected BnplCustomerRef Me => customer.Value;

    protected async Task<BnplRequest> MyOpenRequestAsync(CustomerRequestCommand command, CancellationToken ct)
    {
        var request = await RequestAsync(command.Id, ct, Me.UserId);
        if (await ExpireIfDueAsync(request, ct))
        {
            await Db.SaveChangesAsync(ct);
            throw new BnplDomainException("مهلت این درخواست تمام شده است؛ درخواست جدید ثبت کنید.");
        }
        if (command.RowVersion is not null) request.EnsureRowVersion(command.RowVersion);
        return request;
    }
}
