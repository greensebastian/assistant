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
    public void Validate()
    {
        if (AllowedOrigins.Length == 0) throw new ArgumentNullException(nameof(AllowedOrigins));
        if (AllowedMethods.Length == 0) throw new ArgumentNullException(nameof(AllowedMethods));
        if (AllowedHeaders.Length == 0) throw new ArgumentNullException(nameof(AllowedHeaders));
    }
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
}
