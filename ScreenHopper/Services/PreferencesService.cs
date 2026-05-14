using System.IO;
using System.Text.Json;
using ScreenHopper.Models;

namespace ScreenHopper.Services;

public sealed class PreferencesService
{
    private readonly string _preferencesFilePath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public PreferencesService(string? preferencesFilePath = null)
    {
        _preferencesFilePath = preferencesFilePath ?? Path.Combine(AppContext.BaseDirectory, "preferences.json");
    }

    public PreferencesStore Load()
    {
        if (!File.Exists(_preferencesFilePath))
        {
            return new PreferencesStore();
        }

        var json = File.ReadAllText(_preferencesFilePath);
        var store = JsonSerializer.Deserialize<PreferencesStore>(json, _serializerOptions);
        return store ?? new PreferencesStore();
    }

    public void Save(PreferencesStore store)
    {
        var blacklisted = store.BlacklistedProcessSet
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var preferences = new Dictionary<string, AppPreferences>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in store.AppPreferences.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            preferences[pair.Key] = pair.Value;
        }

        var normalized = new PreferencesStore
        {
            BlacklistedProcesses = blacklisted,
            AppPreferences = preferences
        };

        var json = JsonSerializer.Serialize(normalized, _serializerOptions);
        File.WriteAllText(_preferencesFilePath, json);
    }
}
