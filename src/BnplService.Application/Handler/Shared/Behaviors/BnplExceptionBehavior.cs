namespace BnplService.Application.Handler.Shared;

/// <summary>
/// BnplDomainException و خطای همزمانی EF را به BnplMessageResponse با StatusCode مناسب تبدیل می‌کند
/// تا Handlerها با throw قاعده را اعلام کنند (مثل FacilityExceptionBehavior).
/// </summary>
public sealed class BnplExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (BnplDomainException ex) when (IsMessageResponse)
        {
            return Fail(ex.StatusCode, ex.Message);
        }
        catch (DbUpdateConcurrencyException) when (IsMessageResponse)
        {
            return Fail(409, BnplDomainException.Conflict().Message);
        }
        catch (BnplProviderException ex) when (IsMessageResponse)
        {
            return Fail(502, ex.Message);
        }
    }

    private static readonly bool IsMessageResponse =
        typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(BnplMessageResponse<>);

    private static TResponse Fail(int status, string message)
    {
        var response = Activator.CreateInstance(typeof(TResponse))!;
        typeof(TResponse).GetProperty(nameof(BnplMessageResponse<object>.StatusCode))!.SetValue(response, status);
        typeof(TResponse).GetProperty(nameof(BnplMessageResponse<object>.Message))!.SetValue(response, message);
        return (TResponse)response;
    }
}
