namespace BnplService.Application.Handler.Customer.Payments.Queries.GetMyPayment;

/// <summary>«گزارش تراکنش» یک پرداخت مشتری.</summary>
public sealed record GetMyPaymentQuery(long Id) : BnplBaseQuery<BnplMessageResponse<PaymentReceiptDto>>;

public sealed class GetMyPaymentQueryHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetMyPaymentQuery, BnplMessageResponse<PaymentReceiptDto>>
{
    public async Task<BnplMessageResponse<PaymentReceiptDto>> Handle(GetMyPaymentQuery query, CancellationToken ct)
    {
        var me = customer.Value.UserId;
        var payment = await Db.Payments.FirstOrDefaultAsync(p => p.Id == query.Id && p.CustomerId == me, ct)
                      ?? throw BnplDomainException.NotFound("پرداخت یافت نشد.");
        return Ok(PaymentReceiptDto.Of(payment));
    }
}
