using System.Net;
using BnplService.Infrastructure;
using BnplService.Infrastructure.Services.Sandbox;

namespace BnplService.Api.Controllers.V2.Sandbox;

/// <summary>
/// صفحهٔ درگاه آزمایشی (فقط وقتی Bnpl:Providers:Gateway = Sandbox؛ در Production ثبت نمی‌شود).
/// PayUrl پرداخت درگاهی به این صفحه می‌آید؛ «پرداخت موفق» یا «ناموفق» نتیجه را ثبت و مثل درگاه واقعی به کال‌بک هدایت می‌کند.
/// </summary>
[Route(RoutePrefix + "/Sandbox/Pay")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SandboxController(SandboxStore store, IConfiguration configuration) : BnplControllerBase
{
    [HttpGet("{reference}")]
    public IActionResult Page(string reference)
    {
        if (!BnplInfrastructureRegistration.IsSandboxGateway(configuration)) return NotFound();
        var payment = store.FindPayment(reference);
        if (payment is null) return NotFound();

        var action = Url.Content($"~/{RoutePrefix}/Sandbox/Pay/{WebUtility.UrlEncode(reference)}");
        var html = $$"""
            <!doctype html>
            <html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>درگاه آزمایشی</title>
            <style>body{font-family:Tahoma,sans-serif;background:#f4f5f7;margin:0;padding:24px}main{max-width:420px;margin:auto;background:#fff;border-radius:12px;padding:24px;box-shadow:0 1px 4px #0002}
            h1{font-size:18px}p{color:#444}b{font-size:20px}form{display:flex;gap:12px;margin-top:24px}button{flex:1;padding:12px;border:0;border-radius:8px;font-size:15px;cursor:pointer}
            .ok{background:#1f8f4e;color:#fff}.no{background:#e5e7eb}</style></head>
            <body><main>
            <h1>درگاه پرداخت آزمایشی (Sandbox)</h1>
            <p>{{WebUtility.HtmlEncode(payment.Description)}}</p>
            <p>مبلغ: <b>{{payment.Amount:N0}}</b> ریال</p>
            <p>وضعیت: {{payment.State}}</p>
            <form method="post" action="{{action}}">
              <button class="ok" name="result" value="paid">پرداخت موفق</button>
              <button class="no" name="result" value="failed">انصراف / ناموفق</button>
            </form>
            </main></body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPost("{reference}")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult Complete(string reference, [FromForm] string result)
    {
        if (!BnplInfrastructureRegistration.IsSandboxGateway(configuration)) return NotFound();
        var payment = store.FindPayment(reference);
        if (payment is null) return NotFound();
        if (payment.State == SandboxStore.PaymentState.Pending)
            payment.State = result == "paid" ? SandboxStore.PaymentState.Paid : SandboxStore.PaymentState.Failed;
        return Redirect(payment.CallbackUrl);
    }
}
