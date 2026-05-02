using System.ComponentModel.DataAnnotations;

namespace ServiceFlow.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "serviceflow";

    [Required]
    public string Audience { get; set; } = "serviceflow-clients";

    /// <summary>
    /// HMAC-SHA256 signing key. Must be >= 32 bytes / 256 bits. Store it in user secrets or env vars.
    /// </summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 60 * 24)]
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
