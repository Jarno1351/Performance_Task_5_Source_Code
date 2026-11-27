using System.Text.Json;
using System.Text.Json.Serialization;

namespace HubLink.Services;

/// <summary>
/// Configuration: appsettings.json (next to the executable), overridden by environment variables
/// HUBLINK_BASE_URL, HUBLINK_BRANCH_CODE, HUBLINK_ENDPOINT_PATH, HUBLINK_EXTRA_QUERY, HUBLINK_TIMEOUT_SECONDS.
/// </summary>
public sealed class AppSettings
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:5080";
    public string BranchCode { get; set; } = "BR-001";
    public string EndpointPath { get; set; } = "/api/shipments";
    public string ExtraQuery { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 8;

    [JsonIgnore] public string? LoadWarning { get; private set; }

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (loaded != null) settings = loaded;
            }
            else settings.LoadWarning = "appsettings.json not found; using default settings.";
        }
        catch (Exception ex)
        {
            settings = new AppSettings { LoadWarning = $"Could not read appsettings.json ({ex.Message}); using default settings." };
        }

        static string? Env(string n) => Environment.GetEnvironmentVariable(n) is { Length: > 0 } v ? v : null;
        settings.BaseUrl = Env("HUBLINK_BASE_URL") ?? settings.BaseUrl;
        settings.BranchCode = Env("HUBLINK_BRANCH_CODE") ?? settings.BranchCode;
        settings.EndpointPath = Env("HUBLINK_ENDPOINT_PATH") ?? settings.EndpointPath;
        settings.ExtraQuery = Env("HUBLINK_EXTRA_QUERY") ?? settings.ExtraQuery;
        if (int.TryParse(Env("HUBLINK_TIMEOUT_SECONDS"), out var t)) settings.TimeoutSeconds = t;
        if (settings.TimeoutSeconds <= 0) settings.TimeoutSeconds = 8;
        return settings;
    }
}
