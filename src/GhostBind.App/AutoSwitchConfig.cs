using System.IO;
using System.Text.Json;

namespace GhostBind.App;

public sealed class AutoSwitchConfig
{
    public bool Enabled { get; set; }

    // Lower-cased exe name (without .exe) → profile name.
    // e.g. "cyberpunk2077" → "Cyberpunk", "r5apex" → "Apex".
    public Dictionary<string, string> ExeToProfile { get; set; } = new();

    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GhostBind", "autoswitch.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AutoSwitchConfig Load()
    {
        try
        {
            if (!File.Exists(Path)) return new AutoSwitchConfig();
            var json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize<AutoSwitchConfig>(json, Options) ?? new AutoSwitchConfig();
        }
        catch { return new AutoSwitchConfig(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, JsonSerializer.Serialize(this, Options));
        }
        catch { /* best effort */ }
    }
}
