namespace BnplService.Application.Common;

/// <summary>
/// قالب یکسان همهٔ پاسخ‌ها (مثل FacilityBaseMessageResponse):  { "statusCode": 200, "message": "...", "data": {...} }
/// Handlerها قاعده را با throw BnplDomainException اعلام می‌کنند و BnplExceptionBehavior آن را به همین قالب تبدیل می‌کند.
/// </summary>
public class BnplMessageResponse<T>
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

public abstract record BnplBaseCommand<TResponse> : IRequest<TResponse>;

public abstract record BnplBaseQuery<TResponse> : IRequest<TResponse>;

public static class MessageResponse
{
    public static BnplMessageResponse<T> Generate<T>(int statusCode, T? data, string? message) =>
        new() { StatusCode = statusCode, Data = data, Message = message };
}
