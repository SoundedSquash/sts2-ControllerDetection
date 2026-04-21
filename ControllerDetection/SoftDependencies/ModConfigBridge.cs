// =============================================================================
// ModConfigBridge.cs — Drop-in Template for ModConfig Integration
// =============================================================================
// Copy this file into your mod's Scripts/ folder, then:
//   1. Replace "BetterControllerDetection" namespace and mod IDs with your own
//   2. Edit BuildEntries() to define your config items
//   3. Call ModConfigBridge.DeferredRegister() in your mod's Initialize()
//
// Zero DLL reference needed — everything is done via reflection.
// If ModConfig is not installed, your mod works normally (all GetValue calls
// return the fallback you provide).
// =============================================================================

using System.Reflection;
using Godot;
using SaveManager.Patches;

namespace ControllerDetection.SoftDependencies;

internal static class ModConfigBridge
{
    // ─── State ──────────────────────────────────────────────────
    private static bool _available;
    private static bool _registered;
    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    internal static bool IsAvailable => _available;

    // ─── Step 1: Call this in your Initialize() ─────────────────
    // ModConfig may load AFTER your mod (alphabetical order).
    // Deferring to the next frame ensures ModConfig is ready.

    internal static void DeferredRegister()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += OnNextFrame;
    }

    private static void OnNextFrame()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame -= OnNextFrame;
        Detect();
        if (_available) Register();
    }

    // ─── Step 2: Detect ModConfig via reflection ────────────────

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .ToArray();

            _apiType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigType");
            _available = _apiType != null && _entryType != null && _configTypeEnum != null;
        }
        catch
        {
            _available = false;
        }
    }

    // ─── Step 3: Register your config entries ───────────────────

    private static void Register()
    {
        if (_registered) return;
        _registered = true;

        try
        {
            var entries = BuildEntries();

            // Localized display name (shows in ModConfig's mod list)
            var displayNames = new Dictionary<string, string>
            {
                ["en"] = "Better Controller Detection",
                ["zhs"] = "你的模组名字",
            };

            // ModConfig has 2 overloads: 3-param (no i18n) and 4-param (with i18n).
            // We prefer 4-param when available.
            var registerMethod = _apiType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Register")
                .OrderByDescending(m => m.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(null, new object[]
                {
                    "soundedsquash.controllerdetection",          // Must match your mod's ID
                    displayNames["en"],     // Fallback display name
                    displayNames,           // Localized display names
                    entries
                });
            }
            else
            {
                registerMethod.Invoke(null, new object[]
                {
                    "soundedsquash.controllerdetection",
                    displayNames["en"],
                    entries
                });
            }
        }
        catch (Exception e)
        {
            // Log but don't crash — ModConfig is optional
            GD.PrintErr($"[BetterControllerDetection] ModConfig registration failed: {e}");
        }
    }

    // ─── Read/Write Config Values ───────────────────────────────

    /// <summary>Read a saved config value, with fallback if ModConfig absent.</summary>
    internal static T GetValue<T>(string key, T fallback)
    {
        if (!_available) return fallback;
        try
        {
            var result = _apiType!.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(typeof(T))
                ?.Invoke(null, new object[] { "soundedsquash.controllerdetection", key });
            return result != null ? (T)result : fallback;
        }
        catch { return fallback; }
    }

    /// <summary>
    /// Sync a value back to ModConfig (for persistence).
    /// Call this when your mod changes a setting outside ModConfig's UI
    /// (e.g. via hotkey or your own settings menu).
    /// </summary>
    internal static void SetValue(string key, object value)
    {
        if (!_available) return;
        try
        {
            _apiType!.GetMethod("SetValue", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { "soundedsquash.controllerdetection", key, value });
        }
        catch { }
    }

    // ═════════════════════════════════════════════════════════════
    //  EDIT BELOW: Define your config entries
    // ═════════════════════════════════════════════════════════════

    private static Array BuildEntries()
    {
        var list = new List<object>();

        // ─── Section Header (visual only) ───────────────────────

        list.Add(Entry(cfg =>
        {
            Set(cfg, "Label", "General");
            Set(cfg, "Labels", L("General", "常规设置"));
            Set(cfg, "Type", EnumVal("Header"));
        }));

        // ─── Slider (float) ────────────────────────────────────

        list.Add(Entry(cfg =>
        {
            Set(cfg, "Key", "delay");
            Set(cfg, "Label", "Mouse detection delay (s)");
            Set(cfg, "Labels", L("Mouse detection delay (s)", "鼠标检测延迟 (秒)"));
            Set(cfg, "Type", EnumVal("Slider"));
            Set(cfg, "DefaultValue", 1.1f);
            Set(cfg, "Min", 0f);
            Set(cfg, "Max", 5.0f);
            Set(cfg, "Step", 0.1f);
            Set(cfg, "Format", "F1");  // "F0"=no decimal, "F1"=1 decimal, "P0"=percent
            Set(cfg, "Description", "Adjust the delay before the mouse can be used for detection.");
            Set(cfg, "Descriptions", L("Adjust the delay before the mouse can be used for detection.", "调整鼠标触发检测前的延迟时间"));
            Set(cfg, "OnChanged", new Action<object>(v =>
            {
                var converted = Single.TryParse(v.ToString(), out var val);

                if (converted)
                {
                    NControllerManagerPatch.InitialSettleDelay = val;
                }
                else
                {
                    GD.PrintErr($"[BetterControllerDetection] ModConfig.delay failed to set value: {v}");
                }
            }));
        }));

        var result = Array.CreateInstance(_entryType!, list.Count);
        for (int i = 0; i < list.Count; i++)
            result.SetValue(list[i], i);
        return result;
    }

    // ═════════════════════════════════════════════════════════════
    //  Reflection helpers (don't need to modify these)
    // ═════════════════════════════════════════════════════════════

    private static object Entry(Action<object> configure)
    {
        var inst = Activator.CreateInstance(_entryType!)!;
        configure(inst);
        return inst;
    }

    private static void Set(object obj, string name, object value)
        => obj.GetType().GetProperty(name)?.SetValue(obj, value);

    private static Dictionary<string, string> L(string en, string zhs)
        => new() { ["en"] = en, ["zhs"] = zhs };

    private static object EnumVal(string name)
        => Enum.Parse(_configTypeEnum!, name);
}