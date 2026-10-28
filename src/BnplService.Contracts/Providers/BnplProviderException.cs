namespace BnplService.Contracts.Providers;

/// <summary>ارائه‌دهندهٔ بیرونی (دایادایموند، درگاه) خطا داد یا در دسترس نبود. پیام فارسی به کاربر نشان داده می‌شود (502).</summary>
public sealed class BnplProviderException(string message, Exception? inner = null) : Exception(message, inner);
