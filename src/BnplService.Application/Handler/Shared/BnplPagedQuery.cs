using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Shared;

/// <summary>پایهٔ Queryهای صفحه‌بندی‌شده.</summary>
public abstract record BnplPagedQuery<TResponse> : BnplBaseQuery<TResponse>
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}
