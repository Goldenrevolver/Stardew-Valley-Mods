namespace HorseOverhaul
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using StardewModdingAPI.Utilities;
    using System;

    public interface GenericModConfigMenuAPI
    {
        void RegisterModConfig(IManifest mod, Action revertToDefault, Action saveToFile);

        void RegisterLabel(IManifest mod, string labelName, string labelDesc);

        void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<bool> optionGet, Action<bool> optionSet);

        void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet);

        void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet);

        void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet);

        void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<SButton> optionGet, Action<SButton> optionSet);

        void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet, int min, int max);

        void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet, float min, float max);

        void RegisterChoiceOption(IManifest mod, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet, string[] choices);

        void RegisterComplexOption(IManifest mod, string optionName, string optionDesc, Func<Vector2, object, object> widgetUpdate, Func<SpriteBatch, Vector2, object, object> widgetDraw, Action<object> onSave);
    }

    /// <summary>
    /// Config file for the mod
    /// </summary>
    public class HorseConfig
    {
        public bool ThinHorse { get; set; } = true;

        public bool MovementSpeed { get; set; } = true;

        public float MaxMovementSpeedBonus { get; set; } = 3f;

        public bool SaddleBag { get; set; } = true;

        public bool Petting { get; set; } = true;

        public bool Water { get; set; } = true;

        public bool Food { get; set; } = true;

        public KeybindList MenuKey { get; set; } = KeybindList.Parse("H");

        public bool DisableStableSpriteChanges { get; set; } = false;

        public static void VerifyConfigValues(HorseConfig config, HorseOverhaul mod)
        {
            bool invalidConfig = false;

            if (config.MaxMovementSpeedBonus < 0f)
            {
                config.MaxMovementSpeedBonus = 0f;
                invalidConfig = true;
            }

            if (invalidConfig)
            {
                mod.DebugLog("At least one config value was out of range and was reset.");
                mod.Helper.WriteConfig(config);
            }
        }

        public static void SetUpModConfigMenu(HorseConfig config, HorseOverhaul mod)
        {
            GenericModConfigMenuAPI api = mod.Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.RegisterModConfig(manifest, () => config = new HorseConfig(), delegate { mod.Helper.WriteConfig(config); VerifyConfigValues(config, mod); });

            api.RegisterLabel(manifest, "General", null);

            api.RegisterSimpleOption(manifest, "Thin Horse", null, () => config.ThinHorse, (bool val) => config.ThinHorse = val);
            api.RegisterSimpleOption(manifest, "Saddle Bag", null, () => config.SaddleBag, (bool val) => config.SaddleBag = val);

            api.RegisterLabel(manifest, "Friendship", null);

            api.RegisterSimpleOption(manifest, "Movement Speed (MS)", null, () => config.MovementSpeed, (bool val) => config.MovementSpeed = val);
            api.RegisterSimpleOption(manifest, "Maximum MS Bonus", null, () => config.MaxMovementSpeedBonus, (float val) => config.MaxMovementSpeedBonus = val);
            api.RegisterSimpleOption(manifest, "Petting", null, () => config.Petting, (bool val) => config.Petting = val);
            api.RegisterSimpleOption(manifest, "Water", null, () => config.Water, (bool val) => config.Water = val);
            api.RegisterSimpleOption(manifest, "Extra Food", null, () => config.Food, (bool val) => config.Food = val);

            api.RegisterLabel(manifest, "Other", null);

            api.RegisterSimpleOption(manifest, "Disable Stable Sprites", null, () => config.DisableStableSpriteChanges, (bool val) => config.DisableStableSpriteChanges = val);

            api.RegisterLabel(manifest, "(Menu Key Rebinding Only Available In Config File)", null);
        }
    }
}