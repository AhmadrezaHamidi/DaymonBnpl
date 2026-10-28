namespace DayaDiamond.Client;

/// <summary>
/// نگهداری توکن JWT بین درخواست‌ها. به صورت Singleton ثبت می‌شود تا کلاینت (که Typed HttpClient است) بتواند Transient باشد.
/// </summary>
public sealed class DayaDiamondTokenStore
{
    internal SemaphoreSlim Lock { get; } = new(1, 1);

    internal string? Token { get; private set; }

    internal DateTimeOffset ExpiresAt { get; private set; } = DateTimeOffset.MinValue;

    internal void Set(string token, DateTimeOffset expiresAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
    }

    internal bool IsValid(int skewSeconds)
        => Token is not null && DateTimeOffset.UtcNow.AddSeconds(skewSeconds) < ExpiresAt;
}
