namespace PermanentCookoutKit
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using System;

    public interface IGenericModConfigMenuAPI
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
    public class CookoutKitConfig
    {
        public int WoodNeeded { get; set; } = 5;

        public int CoalNeeded { get; set; } = 1;

        public int FiberNeeded { get; set; } = 1;

        public float DriftwoodMultiplier { get; set; } = 5;

        public float HardwoodMultiplier { get; set; } = 5;

        public static void VerifyConfigValues(CookoutKitConfig config, PermanentCookoutKit mod)
        {
            bool invalidConfig = false;

            if (config.WoodNeeded < 0)
            {
                invalidConfig = true;
                config.WoodNeeded = 0;
            }

            if (config.FiberNeeded < 0)
            {
                invalidConfig = true;
                config.FiberNeeded = 0;
            }

            if (config.CoalNeeded < 0)
            {
                invalidConfig = true;
                config.CoalNeeded = 0;
            }

            if (config.DriftwoodMultiplier < 1)
            {
                invalidConfig = true;
                config.DriftwoodMultiplier = 1;
            }

            if (config.HardwoodMultiplier < 1)
            {
                invalidConfig = true;
                config.HardwoodMultiplier = 1;
            }

            if (invalidConfig)
            {
                mod.DebugLog("At least one config value was out of range and was reset.");
                mod.Helper.WriteConfig(config);
            }
        }

        public static void SetUpModConfigMenu(CookoutKitConfig config, PermanentCookoutKit mod)
        {
            IGenericModConfigMenuAPI api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.RegisterModConfig(manifest, () => config = new CookoutKitConfig(), delegate { mod.Helper.WriteConfig(config); VerifyConfigValues(config, mod); });

            api.RegisterLabel(manifest, "Reignition Cost", null);

            api.RegisterSimpleOption(manifest, "Wood Needed", null, () => config.WoodNeeded, (int val) => config.WoodNeeded = val);
            api.RegisterSimpleOption(manifest, "Coal Needed", null, () => config.CoalNeeded, (int val) => config.CoalNeeded = val);
            api.RegisterSimpleOption(manifest, "Fiber Needed", null, () => config.FiberNeeded, (int val) => config.FiberNeeded = val);

            api.RegisterLabel(manifest, "Wood Multipliers", null);

            api.RegisterSimpleOption(manifest, "Driftwood Multiplier", null, () => config.DriftwoodMultiplier, (float val) => config.DriftwoodMultiplier = val);
            api.RegisterSimpleOption(manifest, "Hardwood Multiplier", null, () => config.HardwoodMultiplier, (float val) => config.HardwoodMultiplier = val);
        }
    }
}