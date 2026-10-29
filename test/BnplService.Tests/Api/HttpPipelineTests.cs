using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BnplService.Tests.Api;

/// <summary>سناریوهای کامل از روی HTTP: احراز مشتری و پنل، درگاه Sandbox، کال‌بک و هدایت به اپ.</summary>
public class HttpPipelineTests
{
    private const string Requests = "v2/bnpl/Requests";

    /// <summary>ثبت ← تأیید شرایط ← امضا؛ شناسهٔ درخواست.</summary>
    private static async Task<long> SignedAsync(HttpClient client, long amount = 50_000_000)
    {
        var created = await ApiTestHost.Data(await client.PostAsJsonAsync(Requests, new { amount }));
        var id = created.GetProperty("summary").GetProperty("id").GetInt64();
        await ApiTestHost.Data(await client.PostAsync($"{Requests}/{id}/AcceptTerms", null));
        await ApiTestHost.Data(await client.PostAsync($"{Requests}/{id}/Sign", null));
        return id;
    }

    /// <summary>پرداخت درگاهی یک قسط تا برگشت به اپ؛ آدرس نهایی اپ.</summary>
    private static async Task<Uri> PayByGatewayAsync(HttpClient client, long id, int number, string result = "paid")
    {
        var start = await ApiTestHost.Data(await client.PostAsJsonAsync($"{Requests}/{id}/Payments",
            new { installmentNumber = number, method = 1, gatewayId = 5, returnUrl = ApiTestHost.ReturnUrl }));
        var payUrl = new Uri(start.GetProperty("payUrl").GetString()!);
        Assert.StartsWith($"{ApiTestHost.PublicBaseUrl}/v2/bnpl/Sandbox/Pay/", payUrl.ToString());

        var page = await client.GetAsync(payUrl.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("درگاه پرداخت آزمایشی", await page.Content.ReadAsStringAsync());

        var submitted = await client.PostAsync(payUrl.PathAndQuery, new FormUrlEncodedContent([new("result", result)]));
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        var callback = submitted.Headers.Location!;
        Assert.Equal($"/v2/bnpl/Payments/{start.GetProperty("paymentId").GetInt64()}/Callback", callback.AbsolutePath);

        var back = await client.GetAsync(callback.PathAndQuery);
        Assert.Equal(HttpStatusCode.Redirect, back.StatusCode);
        return back.Headers.Location!;
    }

    private static int Status(JsonElement details) => details.GetProperty("summary").GetProperty("status").GetInt32();

    [Fact]
    public async Task Customer_endpoints_need_a_valid_app_session()
    {
        await using var host = await ApiTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client().GetAsync("v2/bnpl/Config")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client("wrong").GetAsync("v2/bnpl/Config")).StatusCode);

        var config = await ApiTestHost.Data(await host.Client("good").GetAsync("v2/bnpl/Config"));
        Assert.Equal(15_010_000, config.GetProperty("tokenPrice").GetInt64());
    }

    [Fact]
    public async Task Full_contract_over_http_with_the_sandbox_gateway_and_token_payment()
    {
        await using var host = await ApiTestHost.CreateAsync();
        var client = host.Client("good");

        var quote = await ApiTestHost.Data(await client.GetAsync("v2/bnpl/Quote?amount=50000000"));
        Assert.Equal(6_916_660, quote.GetProperty("firstInstallmentAmount").GetInt64());

        var id = await SignedAsync(client);
        var app = await PayByGatewayAsync(client, id, 1);
        Assert.StartsWith(ApiTestHost.ReturnUrl, app.ToString());
        Assert.Contains("status=Succeeded", app.Query);

        var active = await ApiTestHost.Data(await client.GetAsync($"{Requests}/{id}"));
        Assert.Equal((int)BnplRequestStatus.Active, Status(active));

        // قسط دوم از موجودی توکن آزاد (Sandbox: ۱۰۰ توکن)
        var token = await ApiTestHost.Data(await client.PostAsJsonAsync($"{Requests}/{id}/Payments", new { installmentNumber = 2, method = 2 }));
        Assert.Equal((int)PaymentStatus.Succeeded, token.GetProperty("receipt").GetProperty("status").GetInt32());

        var payments = await ApiTestHost.Data(await client.GetAsync($"v2/bnpl/Payments?requestId={id}"));
        Assert.Equal(2, payments.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Failed_sandbox_payment_returns_to_the_app_and_keeps_the_contract_signed()
    {
        await using var host = await ApiTestHost.CreateAsync();
        var client = host.Client("good");
        var id = await SignedAsync(client);

        var app = await PayByGatewayAsync(client, id, 1, result: "failed");
        Assert.Contains("status=Failed", app.Query);
        var details = await ApiTestHost.Data(await client.GetAsync($"{Requests}/{id}"));
        Assert.Equal((int)BnplRequestStatus.Signed, Status(details));
    }

    [Fact]
    public async Task Callback_without_a_return_url_reveals_only_id_and_status()
    {
        await using var host = await ApiTestHost.CreateAsync(new() { ["Bnpl:DefaultReturnUrl"] = null });
        var client = host.Client("good");
        var id = await SignedAsync(client);
        var start = await ApiTestHost.Data(await client.PostAsJsonAsync($"{Requests}/{id}/Payments",
            new { installmentNumber = 1, method = 1, gatewayId = 5 }));
        var paymentId = start.GetProperty("paymentId").GetInt64();

        var callback = await host.Client().GetAsync($"v2/bnpl/Payments/{paymentId}/Callback");
        var data = await ApiTestHost.Data(callback);
        Assert.Equal(paymentId, data.GetProperty("paymentId").GetInt64());
        Assert.False(data.TryGetProperty("receipt", out _));
    }

    [Fact]
    public async Task Validation_errors_come_back_in_persian()
    {
        await using var host = await ApiTestHost.CreateAsync();
        var response = await host.Client("good").PostAsJsonAsync("v2/bnpl/Profile/Info", new { firstName = "ع" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ApiTestHost.Json);
        Assert.StartsWith("اطلاعات ورودی معتبر نیست.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Admin_endpoints_check_panel_permissions()
    {
        await using var host = await ApiTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client().GetAsync("v2/bnpl/Admin/Dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client(panelToken: "nobody").GetAsync("v2/bnpl/Admin/Dashboard")).StatusCode);

        var viewer = host.Client(panelToken: "viewer");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("v2/bnpl/Admin/Dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("v2/bnpl/Admin/Requests")).StatusCode);

        // توکن مشتری به پنل راه ندارد
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client("good").GetAsync("v2/bnpl/Admin/Dashboard")).StatusCode);
    }

    [Fact]
    public async Task Admin_sees_the_contract_and_terminates_it()
    {
        await using var host = await ApiTestHost.CreateAsync();
        var customer = host.Client("good");
        var id = await SignedAsync(customer);
        await PayByGatewayAsync(customer, id, 1);

        var admin = host.Client(panelToken: "admin");
        var dashboard = await ApiTestHost.Data(await admin.GetAsync("v2/bnpl/Admin/Dashboard"));
        Assert.Equal(1, dashboard.GetProperty("activeContracts").GetInt32());

        var list = await ApiTestHost.Data(await admin.GetAsync("v2/bnpl/Admin/Requests?term=09121111111"));
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());

        var details = await ApiTestHost.Data(await admin.GetAsync($"v2/bnpl/Admin/Requests/{id}"));
        var rowVersion = details.GetProperty("details").GetProperty("summary").GetProperty("rowVersion").GetString();
        var terminated = await ApiTestHost.Data(await admin.PostAsJsonAsync($"v2/bnpl/Admin/Requests/{id}/Terminate",
            new { reason = "درخواست مشتری برای فسخ", rowVersion }));
        Assert.Equal((int)BnplRequestStatus.Terminated, Status(terminated.GetProperty("details")));
    }

    [Fact]
    public async Task Admin_edits_settings_plans_and_discounts()
    {
        await using var host = await ApiTestHost.CreateAsync();
        var admin = host.Client(panelToken: "admin");

        var settings = await ApiTestHost.Data(await admin.GetAsync("v2/bnpl/Admin/Settings"));
        Assert.Equal(1, settings.GetProperty("plans").GetArrayLength());

        var plan = await ApiTestHost.Data(await admin.PostAsJsonAsync("v2/bnpl/Admin/Plans", new
        {
            title = "۶ قسط", installmentCount = 6, feePercent = 12, upfrontFeePercent = 2, latePenaltyDailyPercent = 1, sortOrder = 2,
        }));
        Assert.Equal(6, plan.GetProperty("installmentCount").GetInt32());

        var discount = await ApiTestHost.Data(await admin.PostAsJsonAsync("v2/bnpl/Admin/Discounts",
            new { code = "WELCOME", title = "خوش‌آمد", feePercent = 50 }));
        Assert.Equal("WELCOME", discount.GetProperty("code").GetString());
        var discounts = await ApiTestHost.Data(await admin.GetAsync("v2/bnpl/Admin/Discounts"));
        Assert.Equal(1, discounts.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Sandbox_pages_are_closed_when_the_gateway_is_real()
    {
        await using var host = await ApiTestHost.CreateAsync(new() { ["Bnpl:Providers:Gateway"] = "DayaDiamond", ["DayaDiamond:BaseUrl"] = "https://daya.invalid" });
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client().GetAsync("v2/bnpl/Sandbox/Pay/abc")).StatusCode);
    }
}
