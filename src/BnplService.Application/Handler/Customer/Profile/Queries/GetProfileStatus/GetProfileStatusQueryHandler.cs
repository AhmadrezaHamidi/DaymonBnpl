namespace BnplService.Application.Handler.Customer.Profile.Queries.GetProfileStatus;

public sealed class GetProfileStatusQueryHandler(ICustomerProfileProvider profiles, IBnplCurrentCustomer customer)
    : IRequestHandler<GetProfileStatusQuery, BnplMessageResponse<CustomerProfileStatusDto>>
{
    public async Task<BnplMessageResponse<CustomerProfileStatusDto>> Handle(GetProfileStatusQuery request, CancellationToken ct) =>
        MessageResponse.Generate(200, await profiles.GetOrAddAsync(customer.Value.MobileNumber, ct), "عملیات با موفقیت انجام شد.");
}
