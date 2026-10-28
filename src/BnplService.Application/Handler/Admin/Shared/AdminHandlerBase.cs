namespace BnplService.Application.Handler.Admin.Shared;

/// <summary>پایهٔ Handlerهای پنل: کاربر پنل جاری به‌عنوان Actor.</summary>
public abstract class AdminHandlerBase(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time) : BnplHandlerBase(db, time)
{
    protected BnplPanelUser User => user.Value;
    protected Actor Me => user.Value.ToActor();

    protected AdminRequestListItemDto ToAdminItem(BnplRequest r) =>
        new(r.ToListItem(Today), r.CustomerId, r.MobileNumber, r.CustomerName, r.UnpaidDebt(Today));

    protected async Task<AdminRequestDetailsDto> AdminDetailsAsync(BnplRequest r, CancellationToken ct)
    {
        var payments = await Db.Payments.Where(p => p.RequestId == r.Id).OrderByDescending(p => p.Id).ToListAsync(ct);
        return new AdminRequestDetailsDto(r.ToDetails(Today), r.CustomerId, payments.Select(PaymentReceiptDto.Of).ToList());
    }
}
