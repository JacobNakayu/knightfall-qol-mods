namespace KnightfallModSettings;

/// <summary>Optional contract for a loaded mod that wants to show editable values in Mod Settings.</summary>
public interface IModSettingsProvider
{
    string DisplayName { get; }
    IReadOnlyList<ModSetting> Settings { get; }
    void Save();
}

public sealed class ModSetting
{
    public ModSetting(string key, string label, bool value) { Key = key; Label = label; Kind = ModSettingKind.Toggle; BoolValue = value; }
    public ModSetting(string key, string label, float value, float minimum, float maximum, float step = 0f, bool displayAsInteger = false)
    {
        Key = key; Label = label; Kind = ModSettingKind.Slider; FloatValue = value;
        Minimum = minimum; Maximum = maximum; Step = step; DisplayAsInteger = displayAsInteger;
    }
    /// <summary>Creates a free-form numeric value.  Minimum and maximum are optional validation bounds.</summary>
    public ModSetting(string key, string label, float value, float minimum, float maximum, bool numericInput)
    {
        Key = key;
        Label = label;
        Kind = ModSettingKind.Number;
        FloatValue = value;
        Minimum = minimum; Maximum = maximum;
    }
    public ModSetting(string key, string label, IReadOnlyList<string> options, int selectedIndex = 0)
    {
        if (options is null || options.Count == 0) throw new ArgumentException("A choice needs at least one option.", nameof(options));
        Key = key; Label = label; Kind = ModSettingKind.Choice; Options = options.ToArray();
        SelectedIndex = Math.Clamp(selectedIndex, 0, Options.Count - 1);
    }
    public static ModSetting Number(string key, string label, float value, float minimum = float.NegativeInfinity, float maximum = float.PositiveInfinity)
        => new(key, label, value, minimum, maximum, numericInput: true);
    public static ModSetting Choice(string key, string label, IReadOnlyList<string> options, int selectedIndex = 0)
        => new(key, label, options, selectedIndex);
    public string Key { get; }
    public string Label { get; }
    public ModSettingKind Kind { get; }
    public bool BoolValue { get; set; }
    public float FloatValue { get; set; }
    public float Minimum { get; }
    public float Maximum { get; }
    public float Step { get; }
    public bool DisplayAsInteger { get; }
    public IReadOnlyList<string> Options { get; } = Array.Empty<string>();
    public int SelectedIndex { get; set; }
}

public enum ModSettingKind { Toggle, Slider, Number, Choice }

/// <summary>Call Register from a compatible mod's OnInitializeMelon method.</summary>
public static class ModSettingsRegistry
{
    private static readonly Dictionary<string, IModSettingsProvider> Providers = new(StringComparer.OrdinalIgnoreCase);
    public static void Register(IModSettingsProvider provider) => Providers[provider.GetType().Assembly.GetName().Name ?? provider.DisplayName] = provider;
    /// <summary>
    /// Registers a provider from a self-contained mod without requiring that mod to
    /// reference this assembly at compile time. The provider exposes public
    /// DisplayName, Settings, and Save members; each setting exposes Key, Label, and
    /// BoolValue members for a toggle. Numeric and choice values are also supported
    /// by the reflection convention; see the README for their property names.
    /// </summary>
    public static void Register(object provider)
    {
        if (provider is IModSettingsProvider typed)
        {
            Register(typed);
            return;
        }

        // The wrapper belongs to this assembly, but it represents the source mod. Use
        // the source assembly name so clicking that mod finds its settings provider.
        Providers[provider.GetType().Assembly.GetName().Name ?? provider.GetType().Name] = new ReflectionSettingsProvider(provider);
    }
    public static bool TryGet(string assemblyName, out IModSettingsProvider? provider) => Providers.TryGetValue(assemblyName, out provider);

    private sealed class ReflectionSettingsProvider : IModSettingsProvider
    {
        private readonly object _provider;
        private readonly System.Reflection.PropertyInfo _displayName;
        private readonly System.Reflection.MethodInfo _save;
        private readonly List<(ModSetting Setting, object Source)> _settings = new();

        public ReflectionSettingsProvider(object provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            var providerType = provider.GetType();
            _displayName = providerType.GetProperty("DisplayName")
                ?? throw new ArgumentException("Provider must expose DisplayName.", nameof(provider));
            _save = providerType.GetMethod("Save", Type.EmptyTypes)
                ?? throw new ArgumentException("Provider must expose Save().", nameof(provider));
            var sourceSettings = providerType.GetProperty("Settings")?.GetValue(provider) as System.Collections.IEnumerable
                ?? throw new ArgumentException("Provider must expose Settings.", nameof(provider));

            foreach (var source in sourceSettings)
            {
                if (source is null) continue;
                var type = source.GetType();
                var key = type.GetProperty("Key")?.GetValue(source) as string;
                var label = type.GetProperty("Label")?.GetValue(source) as string;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(label)) continue;
                var boolValue = type.GetProperty("BoolValue");
                if (boolValue?.PropertyType == typeof(bool))
                {
                    _settings.Add((new ModSetting(key, label, (bool)boolValue.GetValue(source)!), source));
                    continue;
                }

                var options = ReadStrings(type.GetProperty("Options")?.GetValue(source));
                var selectedIndex = type.GetProperty("SelectedIndex");
                if (options.Count > 0 && selectedIndex?.PropertyType == typeof(int))
                {
                    _settings.Add((new ModSetting(key, label, options, (int)selectedIndex.GetValue(source)!), source));
                    continue;
                }

                var floatValue = type.GetProperty("FloatValue");
                if (floatValue is null || !IsNumeric(floatValue.PropertyType)) continue;
                var value = Convert.ToSingle(floatValue.GetValue(source));
                var minimum = ReadFloat(type.GetProperty("Minimum"), source, 0f);
                var maximum = ReadFloat(type.GetProperty("Maximum"), source, 100f);
                var step = ReadFloat(type.GetProperty("Step"), source, 0f);
                var asInteger = type.GetProperty("DisplayAsInteger")?.GetValue(source) as bool? ?? floatValue.PropertyType != typeof(float) && floatValue.PropertyType != typeof(double);
                _settings.Add((new ModSetting(key, label, value, minimum, maximum, step, asInteger), source));
            }
        }

        public string DisplayName => (string?)_displayName.GetValue(_provider) ?? _provider.GetType().Assembly.GetName().Name ?? "Mod";
        public IReadOnlyList<ModSetting> Settings => _settings.Select(pair => pair.Setting).ToArray();

        public void Save()
        {
            foreach (var (setting, source) in _settings)
            {
                var type = source.GetType();
                type.GetProperty("BoolValue")?.SetValue(source, setting.BoolValue);
                type.GetProperty("SelectedIndex")?.SetValue(source, setting.SelectedIndex);
                var value = type.GetProperty("FloatValue");
                if (value is not null && IsNumeric(value.PropertyType))
                    value.SetValue(source, Convert.ChangeType(setting.FloatValue, value.PropertyType));
            }
            _save.Invoke(_provider, null);
        }

        private static bool IsNumeric(Type type) => type == typeof(float) || type == typeof(double) || type == typeof(int) || type == typeof(long);
        private static float ReadFloat(System.Reflection.PropertyInfo? property, object source, float fallback) => property is not null && IsNumeric(property.PropertyType) ? Convert.ToSingle(property.GetValue(source)) : fallback;
        private static List<string> ReadStrings(object? value) => value is System.Collections.IEnumerable values
            ? values.Cast<object?>().Where(x => x is not null).Select(x => x!.ToString()!).ToList() : new List<string>();
    }
}
