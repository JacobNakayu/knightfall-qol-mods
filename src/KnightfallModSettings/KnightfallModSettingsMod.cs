using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2CppLandfall.UI.SettingsUI;

[assembly: MelonInfo(typeof(KnightfallModSettings.KnightfallModSettingsMod), "Knightfall Mod Settings", "0.1.0", "Salamander")]
[assembly: MelonGame(null, null)]

namespace KnightfallModSettings;

public sealed class KnightfallModSettingsMod : MelonMod
{
    private const string ManagerAssembly = "KnightfallModSettings";
    private readonly List<ModFile> _mods = new();
    private bool _visible;
    private static Sprite? _icon;
    private static Transform? _modButtonTransform;
    private static Transform? _customGameButtonTransform;
    private static Button? _menuButtonTemplate;
    private static Button? _nativeSettingsButton;
    private static SettingsMenu? _nativeSettingsMenu;
    private readonly List<Button> _modListButtons = new();
    private readonly List<MenuRow> _menuRows = new();
    private int _scrollRows;
    private ModFile? _selectedMod;
    private IModSettingsProvider? _selectedProvider;
    private bool _openNativeSettingsRequested;
    private int _nativeSettingsWaitFrames;
    private NativeSettingsSession? _nativeSettingsSession;

    public override void OnInitializeMelon()
    {
        ScanMods();
        // This is the generated IL2CPP proxy name, not the native Unity namespace.
        // Avoid AccessTools.TypeByName here: it scans every generated game type and can fail
        // if an unrelated generated assembly is blocked by Windows.
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(x => x.GetName().Name == "Assembly-CSharp");
        var pageType = assembly?.GetType("Il2CppLandfall.UI.MainMenu.MainMenuMainPage", throwOnError: false);
        var awake = pageType?.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (awake is null)
        {
            LoggerInstance.Warning("Main menu was not found. The settings button will not be added.");
            return;
        }
        var harmony = new HarmonyLib.Harmony("knightfall.modsettings.menu");
        harmony.Patch(awake, postfix: new HarmonyLib.HarmonyMethod(typeof(KnightfallModSettingsMod), nameof(AfterMainMenuAwake)));
        var settingsMenuType = assembly?.GetType("Il2CppLandfall.UI.SettingsUI.SettingsMenu", throwOnError: false);
        var settingsStart = settingsMenuType?.GetMethod("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (settingsStart is not null) harmony.Patch(settingsStart, postfix: new HarmonyLib.HarmonyMethod(typeof(KnightfallModSettingsMod), nameof(AfterSettingsMenuStart)));
        LoggerInstance.Msg("Patched MainMenuMainPage.Awake; Mod Settings button will be added when the main menu opens.");
    }

    public static void AfterMainMenuAwake(object __instance)
    {
        try
        {
            // Clone the last existing item so the new entry follows the same vertical menu format.
            var source = __instance.GetType().GetProperty("m_customGameButton")?.GetValue(__instance) as Button;
            if (source is null || GameObject.Find("KnightfallModSettingsButton") is not null) return;
            var button = UnityEngine.Object.Instantiate(source, source.transform.parent);
            button.gameObject.name = "KnightfallModSettingsButton";
            _modButtonTransform = button.transform;
            _customGameButtonTransform = source.transform;
            _menuButtonTemplate = source;
            _nativeSettingsButton = __instance.GetType().GetProperty("m_settingsButton")?.GetValue(__instance) as Button;
            PositionMainMenuButton();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((UnityAction)ToggleWindow);
            SetButtonLabel(button, "MOD SETTINGS");
        }
        catch (Exception ex) { MelonLogger.Warning($"Could not add Mod Settings button: {ex.Message}"); }
    }

    public static void AfterSettingsMenuStart(SettingsMenu __instance) => _nativeSettingsMenu = __instance;

    private static void ApplyIcon(Button button)
    {
        if (_icon is null)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KnightfallModSettings.msettings.png");
            if (stream is null) { MelonLogger.Warning("Embedded msettings.png icon is missing."); return; }
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, new Il2CppStructArray<byte>(bytes.ToArray())))
            {
                MelonLogger.Warning("Mod Settings icon could not be decoded.");
                return;
            }
            _icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        // Keep the root's white ring, but replace every child icon layer (including the bomb).
        foreach (var iconImage in button.GetComponentsInChildren<Image>(true).Where(x => x.gameObject != button.gameObject))
        {
            iconImage.sprite = _icon;
            iconImage.color = Color.white;
        }
    }

    private static void SetButtonLabel(Button button, string value, Color? color = null)
    {
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.text = value;
            if (color.HasValue) label.color = color.Value;
        }
    }

    private static void PositionMainMenuButton()
    {
        if (_modButtonTransform is null || _customGameButtonTransform is null) return;
        // The menu's layout rewrites RectTransform anchors. localPosition is available on
        // every generated Transform and applying it after layout keeps this fifth entry below Custom Game.
        _modButtonTransform.localPosition = _customGameButtonTransform.localPosition + new Vector3(0, -140, 0);
    }

    private static void ToggleWindow()
    {
        var mod = MelonMod.RegisteredMelons.FirstOrDefault(x => x.Info.Name == "Knightfall Mod Settings") as KnightfallModSettingsMod;
        if (mod is null) return;
        if (mod._visible) mod.CloseModList();
        else mod.OpenModList();
    }

    private void OpenModList()
    {
        if (_menuButtonTemplate is null) return;
        ScanMods();
        _visible = true;
        _scrollRows = 0;
        RebuildModList();
    }

    private void RebuildModList()
    {
        if (_menuButtonTemplate is null) return;
        foreach (var row in _modListButtons) UnityEngine.Object.Destroy(row.gameObject);
        _modListButtons.Clear();
        _menuRows.Clear();

        foreach (var entry in _mods)
        {
            var color = entry.Enabled ? new Color(0.65f, 1f, 0.65f) : new Color(1f, 0.65f, 0.65f);
            _menuRows.Add(new MenuRow($"{(entry.Enabled ? "ON" : "OFF")}\n{entry.Name}", color, () => HandleModClick(entry)));
        }

        _scrollRows = Math.Clamp(_scrollRows, 0, Math.Max(0, _menuRows.Count - 1));
        for (var index = 0; index < _menuRows.Count; index++)
        {
            var menuRow = _menuRows[index];
            var row = UnityEngine.Object.Instantiate(_menuButtonTemplate, _menuButtonTemplate.transform.parent);
            row.gameObject.name = $"KnightfallModSettingsEntry_{index}";
            row.transform.localPosition = _menuButtonTemplate.transform.localPosition + new Vector3(240, 150 - (index - _scrollRows) * 100, 0);
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener((UnityAction)menuRow.OnClick);
            SetButtonLabel(row, menuRow.Label, menuRow.Color);
            _modListButtons.Add(row);
        }
    }

    public override void OnUpdate()
    {
        if (_nativeSettingsSession is { } session && !session.Menu.m_settingsListUI.gameObject.activeInHierarchy)
            RestoreNativeSettingsMenu();
        if (_nativeSettingsSession is { } configuredSession && configuredSession.FramesBeforeEditorSetup > 0 && --configuredSession.FramesBeforeEditorSetup == 0)
            foreach (var configure in configuredSession.DeferredEditorSetup) configure();
        if (_openNativeSettingsRequested && _nativeSettingsSession is null)
            TryBuildNativeSettingsMenu();
        if (!_visible) return;
        var wheel = Input.mouseScrollDelta.y;
        if (_menuRows.Count == 0) return;
        if (Math.Abs(wheel) < 0.01f) return;
        var next = Math.Clamp(_scrollRows - Math.Sign(wheel), 0, Math.Max(0, _menuRows.Count - 1));
        if (next == _scrollRows) return;
        _scrollRows = next;
        RebuildModList();
    }

    private void CloseModList()
    {
        _visible = false;
        foreach (var row in _modListButtons) UnityEngine.Object.Destroy(row.gameObject);
        _modListButtons.Clear();
        RestoreNativeSettingsMenu();
    }

    private void HandleModClick(ModFile entry)
    {
        if (ModSettingsRegistry.TryGet(entry.AssemblyName, out var provider) && provider is not null && provider.Settings.Count > 0)
        {
            _selectedMod = entry;
            _selectedProvider = provider;
            try
            {
                foreach (var row in _modListButtons) UnityEngine.Object.Destroy(row.gameObject);
                _modListButtons.Clear();
                _visible = false;
                _openNativeSettingsRequested = true;
                _nativeSettingsWaitFrames = 0;
                if (_nativeSettingsButton is null) throw new InvalidOperationException("The game's Settings button was not found.");
                _nativeSettingsButton.onClick.Invoke();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Could not open settings for '{entry.Name}': {ex}");
                _selectedMod = null;
                _selectedProvider = null;
                _openNativeSettingsRequested = false;
                RebuildModList();
            }
            return;
        }

        // Mods that expose no editable settings keep the concise one-click enable/disable behavior.
        ToggleMod(entry);
    }

    private void ToggleMod(ModFile entry)
    {
        SetEnabled(entry, !entry.Enabled);
        ScanMods();
        RebuildModList();
    }

    /// <summary>
    /// Reuses the game's Settings page rather than imitating it. Existing populated cells are
    /// cloned as templates, preserving the game's layout, arrows, sliders, focus behavior, and
    /// controller navigation while replacing only their value callbacks.
    /// </summary>
    private void TryBuildNativeSettingsMenu()
    {
        if (_selectedProvider is null) return;
        var menu = _nativeSettingsMenu ?? Resources.FindObjectsOfTypeAll<SettingsMenu>().FirstOrDefault();
        if (menu?.m_settingsListUI is null || menu.m_settingsListUI.spawnedCells is null)
        {
            ReportNativeSettingsWait("SettingsMenu or its SettingsListUI is not ready.");
            return;
        }

        var originals = menu.m_settingsListUI.spawnedCells.ToArray();
        var enumTemplate = originals.FirstOrDefault(cell => cell.GetComponentInChildren<SettingsEnumTypeUI>(true) is not null);
        var floatTemplate = originals.FirstOrDefault(cell => cell.GetComponentInChildren<SettingsFloatTypeUI>(true) is not null);
        if (enumTemplate is null || _selectedProvider.Settings.Any(setting => setting.Kind != ModSettingKind.Toggle && setting.Kind != ModSettingKind.Choice && floatTemplate is null))
        {
            ReportNativeSettingsWait($"The native list has {originals.Length} cells but is missing the required editor template.");
            return;
        }

        var session = new NativeSettingsSession(menu, originals);
        foreach (var cell in originals) cell.gameObject.SetActive(false);
        HideSettingsTabsAndSetTitle(session, _selectedProvider.DisplayName);

        // This manager-owned row replaces the one-click action shown for mods without
        // settings. It is deliberately first so it is never obscured by a long config list.
        var enabledCellObject = UnityEngine.Object.Instantiate(enumTemplate.gameObject, enumTemplate.transform.parent);
        enabledCellObject.name = "KnightfallModSettingsCell_ModEnabled";
        enabledCellObject.SetActive(true);
        session.CreatedCells.Add(enabledCellObject);
        var enabledCell = enabledCellObject.GetComponent<SettingsCellUI>();
        enabledCell.m_settingName.text = "Mod enabled (restart required)";
        session.DeferredEditorSetup.Add(() => ConfigureManagedEnableCell(enabledCell));

        foreach (var setting in _selectedProvider.Settings)
        {
            var template = setting.Kind is ModSettingKind.Toggle or ModSettingKind.Choice ? enumTemplate : floatTemplate!;
            var clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
            clone.name = $"KnightfallModSettingsCell_{setting.Key}";
            clone.SetActive(true);
            session.CreatedCells.Add(clone);
            var cell = clone.GetComponent<SettingsCellUI>();
            cell.m_settingName.text = setting.Label;
            if (setting.Kind is ModSettingKind.Toggle or ModSettingKind.Choice)
                session.DeferredEditorSetup.Add(() => ConfigureEnumCell(cell, setting, _selectedProvider));
            else
                session.DeferredEditorSetup.Add(() => ConfigureNumericCell(cell, setting, _selectedProvider));
        }

        _nativeSettingsSession = session;
        _openNativeSettingsRequested = false;
    }

    private void ReportNativeSettingsWait(string reason)
    {
        _nativeSettingsWaitFrames++;
        if (_nativeSettingsWaitFrames == 120)
            LoggerInstance.Warning($"Mod Settings is waiting for the native settings screen: {reason}");
    }

    private static void ConfigureEnumCell(SettingsCellUI cell, ModSetting setting, IModSettingsProvider provider)
    {
        var editor = cell.GetComponentInChildren<SettingsEnumTypeUI>(true);
        if (editor is null) return;
        var options = setting.Kind == ModSettingKind.Toggle ? new[] { "Off", "On" } : setting.Options.ToArray();
        var selected = setting.Kind == ModSettingKind.Toggle ? (setting.BoolValue ? 1 : 0) : setting.SelectedIndex;
        editor.m_leftButton.onClick.RemoveAllListeners();
        editor.m_rightButton.onClick.RemoveAllListeners();
        editor.m_dropdown.onValueChanged.RemoveAllListeners();

        void Apply(int value)
        {
            selected = (value % options.Length + options.Length) % options.Length;
            if (setting.Kind == ModSettingKind.Toggle) setting.BoolValue = selected == 1;
            else setting.SelectedIndex = selected;
            editor.m_dropdown.captionText.text = options[selected];
            provider.Save();
        }

        editor.m_dropdown.captionText.text = options[selected];
        editor.m_leftButton.onClick.AddListener((UnityAction)(() => Apply(selected - 1)));
        editor.m_rightButton.onClick.AddListener((UnityAction)(() => Apply(selected + 1)));
    }

    private void ConfigureManagedEnableCell(SettingsCellUI cell)
    {
        var editor = cell.GetComponentInChildren<SettingsEnumTypeUI>(true);
        if (editor is null || _selectedMod is null) return;
        var enabled = _selectedMod.Enabled;
        editor.m_leftButton.onClick.RemoveAllListeners();
        editor.m_rightButton.onClick.RemoveAllListeners();
        editor.m_dropdown.onValueChanged.RemoveAllListeners();

        void Apply(bool nextEnabled)
        {
            if (_selectedMod is null || nextEnabled == enabled) return;
            var assemblyName = _selectedMod.AssemblyName;
            SetEnabled(_selectedMod, nextEnabled);
            ScanMods();
            _selectedMod = _mods.FirstOrDefault(mod => mod.AssemblyName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
            enabled = _selectedMod?.Enabled ?? nextEnabled;
            editor.m_dropdown.captionText.text = enabled ? "On" : "Off";
        }

        editor.m_dropdown.captionText.text = enabled ? "On" : "Off";
        editor.m_leftButton.onClick.AddListener((UnityAction)(() => Apply(false)));
        editor.m_rightButton.onClick.AddListener((UnityAction)(() => Apply(true)));
    }

    private static void ConfigureNumericCell(SettingsCellUI cell, ModSetting setting, IModSettingsProvider provider)
    {
        var editor = cell.GetComponentInChildren<SettingsFloatTypeUI>(true);
        if (editor is null) return;
        var slider = editor.slider;
        var input = editor.inputField;
        slider.onValueChanged.RemoveAllListeners();
        input.onEndEdit.RemoveAllListeners();
        slider.minValue = setting.Minimum;
        slider.maxValue = setting.Maximum;
        slider.wholeNumbers = setting.DisplayAsInteger;

        void Apply(float value)
        {
            value = Mathf.Clamp(value, setting.Minimum, setting.Maximum);
            if (setting.Step > 0f) value = setting.Minimum + Mathf.Round((value - setting.Minimum) / setting.Step) * setting.Step;
            setting.FloatValue = value;
            slider.SetValueWithoutNotify(value);
            input.SetTextWithoutNotify(FormatNumeric(setting));
            provider.Save();
        }

        slider.SetValueWithoutNotify(setting.FloatValue);
        input.SetTextWithoutNotify(FormatNumeric(setting));
        slider.onValueChanged.AddListener((UnityAction<float>)Apply);
        input.onEndEdit.AddListener((UnityAction<string>)(text =>
        {
            if (float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)) Apply(value);
            else input.SetTextWithoutNotify(FormatNumeric(setting));
        }));
    }

    private static string FormatNumeric(ModSetting setting) => setting.DisplayAsInteger
        ? Mathf.RoundToInt(setting.FloatValue).ToString(System.Globalization.CultureInfo.InvariantCulture)
        : setting.FloatValue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static void HideSettingsTabsAndSetTitle(NativeSettingsSession session, string title)
    {
        var labels = session.Menu.GetComponentsInChildren<TMP_Text>(true);
        var heading = labels.Where(label => label.text.Equals("SETTINGS", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(label => label.fontSize).FirstOrDefault();
        if (heading is not null)
        {
            session.Title = heading;
            session.OriginalTitle = heading.text;
            heading.text = title;
        }
        foreach (var label in labels.Where(label => label.text.Equals("SETTINGS", StringComparison.OrdinalIgnoreCase) || label.text.Equals("KEYBINDINGS", StringComparison.OrdinalIgnoreCase)))
        {
            var tab = label.GetComponentInParent<Button>();
            if (tab is not null && session.HiddenObjects.All(item => item.Object != tab.gameObject))
            {
                session.HiddenObjects.Add((tab.gameObject, tab.gameObject.activeSelf));
                tab.gameObject.SetActive(false);
            }
        }
        var keybindings = session.Menu.m_keybindingsPage;
        if (keybindings is not null && session.HiddenObjects.All(item => item.Object != keybindings.gameObject))
        {
            session.HiddenObjects.Add((keybindings.gameObject, keybindings.gameObject.activeSelf));
            keybindings.gameObject.SetActive(false);
        }
    }

    private void RestoreNativeSettingsMenu()
    {
        _openNativeSettingsRequested = false;
        _nativeSettingsWaitFrames = 0;
        if (_nativeSettingsSession is not { } session) return;
        foreach (var created in session.CreatedCells) UnityEngine.Object.Destroy(created);
        foreach (var original in session.OriginalCells) if (original is not null) original.gameObject.SetActive(true);
        foreach (var hidden in session.HiddenObjects) if (hidden.Object is not null) hidden.Object.SetActive(hidden.WasActive);
        if (session.Title is not null) session.Title.text = session.OriginalTitle;
        _nativeSettingsSession = null;
        _selectedMod = null;
        _selectedProvider = null;
    }

    private void ScanMods()
    {
        _mods.Clear();
        var directory = MelonEnvironment.ModsDirectory;
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            var disabled = path.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase);
            if (!disabled && !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
            string name;
            try { name = AssemblyName.GetAssemblyName(path).Name ?? Path.GetFileNameWithoutExtension(path); }
            catch { name = Path.GetFileNameWithoutExtension(disabled ? Path.GetFileNameWithoutExtension(path) : path); }
            if (name.Equals(ManagerAssembly, StringComparison.OrdinalIgnoreCase)) continue;
            _mods.Add(new ModFile(path, name, !disabled));
        }
        _mods.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        DiscoverCompatibleSettingsProviders();
    }

    /// <summary>
    /// Finds self-contained settings definitions exposed by loaded mods. A compatible
    /// mod provides one public type named ModSettingsDefinition with a public static
    /// CreateProvider() method. The returned provider is adapted by ModSettingsRegistry
    /// without a compile-time reference to the mod.
    /// </summary>
    private void DiscoverCompatibleSettingsProviders()
    {
        foreach (var melon in MelonMod.RegisteredMelons)
        {
            var assembly = melon.GetType().Assembly;
            if (assembly == typeof(KnightfallModSettingsMod).Assembly) continue;

            try
            {
                var definition = assembly.GetTypes().FirstOrDefault(type => type.Name == "ModSettingsDefinition");
                var factory = definition?.GetMethod("CreateProvider", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (factory?.Invoke(null, null) is { } provider)
                    ModSettingsRegistry.Register(provider);
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not inspect settings exposed by '{assembly.GetName().Name}': {ex.Message}");
            }
        }
    }

    private void SetEnabled(ModFile entry, bool enabled)
    {
        try
        {
            var target = enabled ? entry.Path[..^".disabled".Length] : entry.Path + ".disabled";
            File.Move(entry.Path, target);
        }
        catch (Exception ex) { LoggerInstance.Error($"Could not change '{entry.Name}': {ex.Message}"); }
    }

    private sealed record ModFile(string Path, string AssemblyName, bool Enabled) { public string Name => AssemblyName; }
    private sealed record MenuRow(string Label, Color Color, Action OnClick);
    private sealed class NativeSettingsSession
    {
        public NativeSettingsSession(SettingsMenu menu, SettingsCellUI[] originalCells) => (Menu, OriginalCells) = (menu, originalCells);
        public SettingsMenu Menu { get; }
        public SettingsCellUI[] OriginalCells { get; }
        public List<GameObject> CreatedCells { get; } = new();
        // Unity runs Start on cloned child controls after this method returns; wait until
        // those template listeners have finished initializing before replacing them.
        public int FramesBeforeEditorSetup { get; set; } = 2;
        public List<Action> DeferredEditorSetup { get; } = new();
        public List<(GameObject Object, bool WasActive)> HiddenObjects { get; } = new();
        public TMP_Text? Title { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
    }
}
