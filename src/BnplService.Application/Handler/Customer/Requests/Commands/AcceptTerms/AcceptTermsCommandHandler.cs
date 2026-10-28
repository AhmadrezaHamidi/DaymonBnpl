using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.AcceptTerms;

public sealed class AcceptTermsCommandHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : CustomerRequestHandlerBase(db, customer, time), IRequestHandler<AcceptTermsCommand, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(AcceptTermsCommand command, CancellationToken ct)
    {
        var request = await MyOpenRequestAsync(command, ct);
        request.AcceptTerms(Actor.Customer(Me), Now);
        await Db.SaveChangesAsync(ct);
        return Ok(request.ToDetails(Today), "شرایط تأیید شد.");
    }
}
