namespace EnhancedInventory;

/// <summary>
/// Enhanced Inventory's complete Mod Settings contract. Keep user-adjustable values,
/// labels, and control types here; the gameplay code continues to read Settings.
/// Knightfall Mod Settings discovers this file's compiled type by convention, without
/// Enhanced Inventory referencing its assembly.
/// </summary>
public static class ModSettingsDefinition
{
    public static EnhancedInventorySettingsProvider CreateProvider() => new();

    public sealed class EnhancedInventorySettingsProvider
    {
        private readonly ModSettingDefinition _scrollAllInventoryItems = new(
            "ScrollAllInventoryItems",
            "Scroll all inventory items",
            EnhancedInventory.Settings.ScrollAllInventoryItems);

        public string DisplayName => "Enhanced Inventory";
        public IReadOnlyList<ModSettingDefinition> Settings => new[] { _scrollAllInventoryItems };

        public void Save()
        {
            EnhancedInventory.Settings.ScrollAllInventoryItems = _scrollAllInventoryItems.BoolValue;
            EnhancedInventory.Settings.Save();
        }
    }

    /// <summary>Boolean setting model understood by the compatible-mod convention.</summary>
    public sealed class ModSettingDefinition
    {
        public ModSettingDefinition(string key, string label, bool value)
        {
            Key = key;
            Label = label;
            BoolValue = value;
        }

        public string Key { get; }
        public string Label { get; }
        public bool BoolValue { get; set; }
    }
}
