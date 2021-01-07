namespace ForageFantasy
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
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
    public class ForageFantasyConfig
    {
        public bool MushroomCaveQuality { get; set; } = true;

        public bool CommonFiddleheadFern { get; set; } = true;

        public bool ForageSurvivalBurger { get; set; } = true;

        public int TapperQualityOptions { get; set; } = 1;

        public bool TapperQualityRequiresTapperPerk { get; set; } = false;

        private static string[] BoolChoices { get; set; } = new string[] { "Disabled", "Enabled" };

        private static string[] TQChoices { get; set; } = new string[] { "Disabled", "Forage Level Based", "Forage Level Based (No Botanist)", "Tree Age Based (Months)", "Tree Age Based (Years)" };

        public static void VerifyConfigValues(ForageFantasyConfig config, ForageFantasy mod)
        {
            bool invalidConfig = false;

            if (config.TapperQualityOptions < 0 || config.TapperQualityOptions > 4)
            {
                invalidConfig = true;
                config.TapperQualityOptions = 0;
            }

            if (invalidConfig)
            {
                mod.DebugLog("A config value was out of range and was reset.");
                mod.Helper.WriteConfig(config);
            }
        }

        public static void SetUpModConfigMenu(ForageFantasyConfig config, ForageFantasy mod)
        {
            GenericModConfigMenuAPI api = mod.Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.RegisterModConfig(manifest, () => config = new ForageFantasyConfig(), () => mod.Helper.WriteConfig(config));

            api.RegisterLabel(manifest, "General Tweaks", null);

            api.RegisterChoiceOption(manifest, "Mushroom Cave Quality", "Mushrooms have quality based on forage perk", () => BoolToString(config.MushroomCaveQuality), (string val) => config.MushroomCaveQuality = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Common Fiddlehead Fern", "Fiddlehead fern is a common forage,\nadded to wild seeds pack and summer forage bundle", () => BoolToString(config.CommonFiddleheadFern), (string val) => config.CommonFiddleheadFern = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Forage Survival Burger", "Forage based early game crafting recipes and even more efficient cooking recipes", () => BoolToString(config.ForageSurvivalBurger), (string val) => config.ForageSurvivalBurger = StringToBool(val), BoolChoices);

            api.RegisterLabel(manifest, "Tapper Quality", null);

            api.RegisterChoiceOption(manifest, "Tapper Quality Options", null, () => GetElementFromConfig(TQChoices, config.TapperQualityOptions), (string val) => config.TapperQualityOptions = GetIndexFromArrayElement(TQChoices, val), TQChoices);
            api.RegisterChoiceOption(manifest, "Tapper Perk Is Required", null, () => BoolToString(config.TapperQualityRequiresTapperPerk), (string val) => config.TapperQualityRequiresTapperPerk = StringToBool(val), BoolChoices);
        }

        private static bool StringToBool(string s)
        {
            return s == BoolChoices[1];
        }

        private static string BoolToString(bool b)
        {
            return BoolChoices[b ? 1 : 0];
        }

        private static string GetElementFromConfig(string[] options, int config)
        {
            if (config >= 0 && config < options.Length)
            {
                return options[config];
            }
            else
            {
                return options[0];
            }
        }

        private static int GetIndexFromArrayElement(string[] options, string element)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == element)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}