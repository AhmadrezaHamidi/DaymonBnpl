using BnplService.Contracts.Common;
using BnplService.Contracts.Payments;
using BnplService.Contracts.Requests;

namespace BnplService.Contracts.Admin;

public sealed record StatusCountDto(BnplRequestStatus Status, string Title, int Count);

/// <summary>داشبورد پنل: وضعیت قراردادها و جریان پول.</summary>
public sealed record BnplDashboardDto(
    IReadOnlyList<StatusCountDto> ByStatus,
    int ActiveContracts,
    int OverdueContracts,
    long ActivePrincipal,
    long CollectedTotal,
    long CollectedPenalty,
    long OutstandingTotal,
    long OverdueTotal,
    decimal LockedTokens,
    int RefundRequiredPayments,
    long RefundRequiredTotal,
    int NewRequestsToday,
    int ActivatedToday);

public sealed record AdminRequestListItemDto(BnplRequestListItemDto Request, string CustomerId, string MobileNumber, string? CustomerName,
    long UnpaidDebt);

public sealed record AdminRequestDetailsDto(BnplRequestDetailsDto Details, string CustomerId, IReadOnlyList<PaymentReceiptDto> Payments);

public sealed record AdminPaymentListItemDto(PaymentReceiptDto Payment, long RequestId, string CustomerId, ActorDto By);
