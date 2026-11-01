namespace BnplService.Application.Handler.Customer.Profile.Queries.GetProvinces;

/// <summary>فهرست استان‌ها با پیش‌شمارهٔ تلفن ثابت برای فرم نشانی (US-3-02).</summary>
public sealed record GetProvincesQuery : BnplBaseQuery<BnplMessageResponse<IReadOnlyList<ProvinceDto>>>;

public sealed class GetProvincesQueryHandler : IRequestHandler<GetProvincesQuery, BnplMessageResponse<IReadOnlyList<ProvinceDto>>>
{
    private static readonly IReadOnlyList<ProvinceDto> Provinces = IranProvinces.All.Select(p => new ProvinceDto(p.Name, p.AreaCode)).ToList();

    public Task<BnplMessageResponse<IReadOnlyList<ProvinceDto>>> Handle(GetProvincesQuery query, CancellationToken ct) =>
        Task.FromResult(MessageResponse.Generate(200, Provinces, "عملیات با موفقیت انجام شد."));
}
