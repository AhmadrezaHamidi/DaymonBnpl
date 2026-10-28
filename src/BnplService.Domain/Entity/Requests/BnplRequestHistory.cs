namespace BnplService.Domain.Entity.Requests;

/// <summary>تاریخچهٔ وضعیت درخواست: چه کسی، کی، از چه وضعیتی به چه وضعیتی، با چه توضیحی.</summary>
public class BnplRequestHistory
{
    public long Id { get; private set; }
    public long RequestId { get; private set; }
    public BnplRequestStatus? FromStatus { get; private set; }
    public BnplRequestStatus ToStatus { get; private set; }
    public DateTime At { get; private set; }
    public Actor By { get; private set; } = null!;
    public string? Description { get; private set; }

    private BnplRequestHistory() { }

    internal BnplRequestHistory(BnplRequestStatus? from, BnplRequestStatus to, Actor by, DateTime at, string? description)
    {
        FromStatus = from;
        ToStatus = to;
        By = by;
        At = at;
        Description = description;
    }
}
