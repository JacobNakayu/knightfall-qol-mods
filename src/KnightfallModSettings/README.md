# Knightfall Mod Settings

Knightfall Mod Settings adds a **MOD SETTINGS** entry to Knightfall's main menu. It lists installed mods, lets the user enable or disable them for the next launch, and opens a configuration menu for mods that expose the self-contained compatibility contract below.

The startup-banner metadata is at the top of `KnightfallModSettingsMod.cs`:

```csharp
[assembly: MelonInfo(typeof(KnightfallModSettings.KnightfallModSettingsMod), "Knightfall Mod Settings", "0.1.0", "Salamander")]
```

The quoted values after the type are the displayed mod name, version, and author.

## Build and install

Copy `GameDir.props.user.example` to `GameDir.props.user` and set `GameDir` to
Knightfall's installation folder (or set the `KNIGHTFALL_DIR` environment variable), then run:

```powershell
dotnet build .\src\KnightfallModSettings\KnightfallModSettings.csproj
```

The completed `KnightfallModSettings.dll` is copied to the repository's
`Downloadables` folder and deployed to `<Knightfall>\Mods`. To build only the
downloadable artifact, append `-p:DeployToGame=false`.

## Make another mod configurable

A compatible mod does **not** reference `KnightfallModSettings.dll`. Instead, it exposes one public source type named `ModSettingsDefinition` in its own assembly. Knightfall Mod Settings discovers that type when the MOD SETTINGS menu opens.

Create one file named `ModSettingsDefinition.cs` and keep all user-facing setting labels, defaults, control models, and persistence mapping there. The mod must expose:

1. A public static `ModSettingsDefinition` type.
2. A public static parameterless `CreateProvider()` method.
3. A returned provider object with public `DisplayName`, `Settings`, and `Save()` members.
4. A settings collection whose entries expose public `Key` and `Label` plus one of the value models below.
5. A `Save()` implementation that applies the edited model values to the mod's own settings and persists them.

Minimal compatible toggle example:

```csharp
public static class ModSettingsDefinition
{
    public static ExampleProvider CreateProvider() => new();

    public sealed class ExampleProvider
    {
        private readonly ToggleSetting _enabled = new("enabled", "Enable feature", true);

        public string DisplayName => "Example Mod";
        public IReadOnlyList<ToggleSetting> Settings => new[] { _enabled };

        public void Save()
        {
            // Copy _enabled.BoolValue into the mod's runtime/config value, then save it.
        }
    }

    public sealed class ToggleSetting
    {
        public ToggleSetting(string key, string label, bool value) => (Key, Label, BoolValue) = (key, label, value);
        public string Key { get; }
        public string Label { get; }
        public bool BoolValue { get; set; }
    }
}
```

The provider's assembly name must match the DLL filename shown in the mod list. The manager uses that identity to open the correct configuration menu when the user clicks a mod.

### Control models

The configuration screen uses Knightfall's native `SettingsCell` controls: labels appear on the left, and the matching editor appears on the right. It has no Settings/Keybindings tabs; the standard Back button returns to the main menu.

| Value model | Required public properties | Editor |
| --- | --- | --- |
| Boolean | `bool BoolValue { get; set; }` | Off/On arrows |
| Choice | `IReadOnlyList<string> Options`, `int SelectedIndex { get; set; }` | Previous/next arrows |
| Number | Numeric `FloatValue { get; set; }`, numeric `Minimum`, numeric `Maximum` | Slider plus numeric input |

Numeric properties may be `float`, `double`, `int`, or `long`. Optional `float Step` snaps slider values, while `bool DisplayAsInteger` uses whole-number presentation. A numeric setting must have finite bounds; if `Minimum` or `Maximum` is omitted it defaults to `0` and `100`.

Example numeric/choice definitions:

```csharp
public float FloatValue { get; set; } = 1.5f;
public float Minimum => 0.5f;
public float Maximum => 3f;
public float Step => 0.25f;

public IReadOnlyList<string> Options => new[] { "Low", "Medium", "High" };
public int SelectedIndex { get; set; }
```

## How discovery works

When the MOD SETTINGS menu is opened, `KnightfallModSettingsMod.ScanMods()` calls `DiscoverCompatibleSettingsProviders()`. It examines every loaded Melon mod assembly for `ModSettingsDefinition.CreateProvider()`, registers returned providers under their **source mod assembly name**, and opens a settings menu only when that provider has exposed settings. Mods without a compatible provider continue to show only the enable/disable action.

## Enable/disable behavior

The manager lists `Mods\*.dll` and `Mods\*.dll.disabled`. Disabling renames a DLL with the `.disabled` suffix; enabling reverses it. Changes require a restart. The settings manager does not disable itself.
