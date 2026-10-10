namespace SchoolERP.Domain.Constants;

public static class ReservedTenantSlugs
{
    private static readonly HashSet<string> Values = new(StringComparer.Ordinal)
    {
        "admin", "api", "app", "www", "support", "login", "signup", "register",
        "system", "platform", "superadmin", "health"
    };

    public static IReadOnlySet<string> All => Values;
    public static bool Contains(string slug) => Values.Contains(slug);
}
