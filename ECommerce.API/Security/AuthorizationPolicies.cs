namespace ECommerce.API.Security;

public static class AuthorizationPolicies
{
    /// <summary>SuperAdmin or Admin. Required for every catalog write.</summary>
    public const string CatalogAdmin = "CatalogAdmin";
}

public static class OutputCacheTags
{
    /// <summary>Every cached product page. Evicted on any catalog write.</summary>
    public const string Products = "products";
}

public static class RateLimitPolicies
{
    /// <summary>Fixed window per client IP on the credential endpoints.</summary>
    public const string Auth = "auth";
}
