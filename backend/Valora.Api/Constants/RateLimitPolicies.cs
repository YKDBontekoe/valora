namespace Valora.Api.Constants;

/// <summary>
/// Centralized constants for rate limit policy names used across the API.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// A fixed window rate limit policy. Typically used for general API endpoints.
    /// Limits requests per a defined time window per user or IP.
    /// </summary>
    public const string Fixed = "fixed";

    /// <summary>
    /// A strict rate limit policy. Typically used for resource-intensive endpoints.
    /// Has lower limits to prevent abuse of expensive operations.
    /// </summary>
    public const string Strict = "strict";

    /// <summary>
    /// A rate limit policy specifically for authentication endpoints.
    /// Limits login, registration, and token refresh attempts to prevent brute force attacks.
    /// </summary>
    public const string Auth = "auth";
}
