using System.Diagnostics.CodeAnalysis;

namespace Assistant.ServiceDefaults.Cors;

public class CorsSettings
{
    public const string SectionName = "Cors";
    public string? DefaultPolicyName { get; set; } = null;
    public Dictionary<string, CorsPolicySettings> Policies { get; set; } = new();
}

public class CorsPolicySettings
{
    [MemberNotNull(nameof(AllowedOrigins), nameof(AllowedMethods), nameof(AllowedHeaders))]
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AllowedOrigins)) throw new ArgumentNullException(nameof(AllowedOrigins));
        if (string.IsNullOrWhiteSpace(AllowedMethods)) throw new ArgumentNullException(nameof(AllowedMethods));
        if (string.IsNullOrWhiteSpace(AllowedHeaders)) throw new ArgumentNullException(nameof(AllowedHeaders));
    }
    public string? AllowedOrigins { get; set; } = null;
    public string? AllowedMethods { get; set; } = null;
    public string? AllowedHeaders { get; set; } = null;
}
