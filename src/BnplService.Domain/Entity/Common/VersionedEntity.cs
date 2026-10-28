namespace BnplService.Domain.Entity.Common;

/// <summary>
/// همزمانی خوش‌بینانه (مثل Facility): Version با هر تغییر یک واحد زیاد می‌شود و EF آن را Concurrency Token می‌گیرد.
/// دو پرداخت هم‌زمان برای یک قسط، یا پرداخت هم‌زمان با لغو، یکی‌شان 409 می‌گیرد.
/// </summary>
public abstract class VersionedEntity
{
    public int Version { get; protected set; } = 1;

    public string RowVersion => Version.ToString();

    protected void Bump() => Version++;

    public void EnsureRowVersion(string? rowVersion)
    {
        if (rowVersion != RowVersion) throw BnplDomainException.Conflict();
    }
}
