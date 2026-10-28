namespace BnplService.Application.Authorization;

public enum PanelScopeType : byte { Global = 0, Branch = 1, Company = 2 }

public enum PanelPermissionEffect : byte { Allow = 1, Deny = 2 }

/// <summary>یک مجوز با دامنه، همان‌طور که Identity در Internal/Authorization/Resolve برمی‌گرداند.</summary>
public sealed record PanelGrant(string Key, PanelPermissionEffect Effect, PanelScopeType ScopeType, string? ScopeId);

/// <summary>
/// ارزیابی مجوز با همان قاعدهٔ Identity: Allow می‌دهد، Deny سراسری می‌گیرد.
/// منابع BNPL شعبه/شرکت ندارند، پس فقط «کلید را در جایی دارد» مهم است (مثل HasAnywhere در Facility).
/// </summary>
public sealed class PanelPermissions(IReadOnlyList<PanelGrant> grants)
{
    public IReadOnlyList<PanelGrant> Grants { get; } = grants;

    public bool Has(string key) =>
        Grants.Any(g => g.Effect == PanelPermissionEffect.Allow && Same(g.Key, key)) &&
        !Grants.Any(g => g.Effect == PanelPermissionEffect.Deny && Same(g.Key, key) && g.ScopeType == PanelScopeType.Global);

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>کاربر پنل جاری که فیلتر [RequireBnplPermission] از Identity گرفته است.</summary>
public sealed record BnplPanelUser(int PanelUserId, string UserName, string? FullName, int PermissionVersion, PanelPermissions Permissions)
{
    public Actor ToActor() => new(ActorType.Admin, PanelUserId.ToString(), FullName ?? UserName);
}

public interface IBnplCurrentUser
{
    BnplPanelUser Value { get; }
}

/// <summary>مشتری جاری که فیلتر [RequireBnplCustomer] از Identity گرفته است.</summary>
public interface IBnplCurrentCustomer
{
    BnplCustomerRef Value { get; }
}
