using System.Text.Json.Serialization;

namespace ScreenHopper.Models;

public sealed class PreferencesStore
{
    public List<string> BlacklistedProcesses { get; init; } = [];

    public Dictionary<string, AppPreferences> AppPreferences { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public HashSet<string> BlacklistedProcessSet => new(BlacklistedProcesses, StringComparer.OrdinalIgnoreCase);
}
