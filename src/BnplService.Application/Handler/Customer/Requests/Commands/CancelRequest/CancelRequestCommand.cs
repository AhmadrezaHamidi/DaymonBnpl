using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.CancelRequest;

/// <summary>«آیا از لغو درخواست پیش‌خرید اقساطی اطمینان دارید؟ ← لغو میکنم».</summary>
public sealed record CancelRequestCommand : CustomerRequestCommand;
