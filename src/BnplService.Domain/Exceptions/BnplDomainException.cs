namespace BnplService.Domain.Exceptions;

/// <summary>نقض قاعدهٔ کسب‌وکار. پیام فارسی مستقیم به کاربر نشان داده می‌شود.</summary>
public class BnplDomainException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static BnplDomainException NotFound(string message) => new(message, 404);
    public static BnplDomainException Forbidden(string message = "به این مورد دسترسی ندارید.") => new(message, 403);
    public static BnplDomainException Conflict(string message = "این رکورد هم‌زمان تغییر کرده است؛ صفحه را دوباره بارگذاری کنید.") => new(message, 409);
}
