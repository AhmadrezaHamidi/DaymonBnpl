namespace BnplService.Domain.Entity.Common;

/// <summary>انجام‌دهندهٔ عملیات در لحظهٔ عملیات (عکس فوری، نه کلید خارجی): مشتری، کاربر پنل یا سیستم.</summary>
public sealed record Actor(ActorType Type, string Id, string Name)
{
    public static Actor System { get; } = new(ActorType.System, "system", "سامانه");

    public static Actor Customer(BnplCustomerRef customer) => new(ActorType.Customer, customer.UserId, customer.DisplayName);
}

/// <summary>مشتری، همان‌طور که Identity برمی‌گرداند. موبایل کلید ارتباط با دایادایموند است.</summary>
public sealed record BnplCustomerRef(string UserId, string MobileNumber, string? FullName)
{
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? MobileNumber : FullName!;
}
