using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostBind.Core.Profiles;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        // Hard cap deserialization depth — defends against pathological hand-crafted JSON
        // that would otherwise stack-overflow the parser. 32 is plenty for our shapes.
        MaxDepth = 32,
    };

    public string Directory { get; }

    public ProfileStore(string? directory = null)
    {
        Directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GhostBind",
            "Profiles");
        System.IO.Directory.CreateDirectory(Directory);
    }

    public IReadOnlyList<string> ListNames() =>
        System.IO.Directory.GetFiles(Directory, "*.json")
            .Select(p => Path.GetFileNameWithoutExtension(p)!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public Profile Load(string name)
    {
        var path = PathFor(name);
        if (!File.Exists(path)) return new Profile { Name = name };
        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<Profile>(json, Options) ?? new Profile { Name = name };
        MergeMissingDefaults(profile);
        ClampValues(profile);
        return profile;
    }

    // Defense against hand-edited or corrupted profile JSON. The mapping engine
    // already clamps at apply time, but having sane stored values keeps the GUI
    // from showing nonsense (NaN sliders, negative deadzones, etc).
    private static void ClampValues(Profile p)
    {
        ClampStick(p.LeftStick);
        ClampStick(p.RightStick);
        ClampTrigger(p.LeftTrigger);
        ClampTrigger(p.RightTrigger);
    }

    private static void ClampStick(StickConfig s)
    {
        s.InnerDeadzone = ClampFinite(s.InnerDeadzone, 0, 0.5);
        s.OuterDeadzone = ClampFinite(s.OuterDeadzone, 0.5, 1.0);
        s.AntiDeadzone = ClampFinite(s.AntiDeadzone, 0, 0.5);
        s.Sensitivity = ClampFinite(s.Sensitivity, 0.1, 5.0);
        s.CurveExponent = ClampFinite(s.CurveExponent, 1.0, 5.0);
    }

    private static void ClampTrigger(TriggerConfig t)
    {
        t.Deadzone = ClampFinite(t.Deadzone, 0, 0.5);
        t.AntiDeadzone = ClampFinite(t.AntiDeadzone, 0, 0.5);
        t.Saturation = ClampFinite(t.Saturation, 0.5, 1.0);
        t.CurveExponent = ClampFinite(t.CurveExponent, 1.0, 5.0);
        t.DigitalThreshold = ClampFinite(t.DigitalThreshold, 0, 1);
    }

    private static double ClampFinite(double v, double min, double max)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return min;
        return Math.Clamp(v, min, max);
    }

    // Old profiles saved before a feature shipped won't have new mappings. Fill in any
    // missing default mappings rather than leaving the user with a half-mapped controller.
    private static void MergeMissingDefaults(Profile profile)
    {
        var defaults = ButtonMap.Default().Mappings;
        foreach (var kv in defaults)
        {
            if (!profile.ButtonMap.Mappings.ContainsKey(kv.Key))
                profile.ButtonMap.Mappings[kv.Key] = kv.Value;
        }
    }

    public void Save(Profile profile)
    {
        var path = PathFor(profile.Name);
        File.WriteAllText(path, JsonSerializer.Serialize(profile, Options));
    }

    public void Delete(string name)
    {
        var path = PathFor(name);
        if (File.Exists(path)) File.Delete(path);
    }

    private string PathFor(string name) => Path.Combine(Directory, $"{Sanitize(name)}.json");

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

        // Reject "." / ".." / empty — these resolve to the current/parent directory,
        // not a real filename. Replace with a safe stand-in instead of risking
        // Path.Combine producing a path outside the profiles directory.
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed == "." || trimmed == "..")
            return "_";
        return name;
    }
}
