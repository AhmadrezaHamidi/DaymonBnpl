using System.Net;
using System.Text.Json;

namespace DayaDiamond.Client;

public sealed class DayaDiamondException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    /// <summary>فیلد error پاسخ، به همان شکلی که سرور برگردانده (ساختارش در مستند مشخص نیست).</summary>
    public JsonElement? Error { get; }

    public string? RawBody { get; }

    public DayaDiamondException(string message, HttpStatusCode? statusCode = null, JsonElement? error = null, string? rawBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        Error = error;
        RawBody = rawBody;
    }
}
