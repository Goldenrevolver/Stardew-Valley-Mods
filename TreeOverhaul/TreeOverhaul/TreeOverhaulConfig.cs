namespace TreeOverhaul
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
    public class TreeOverhaulConfig
    {
        public bool StopShadeSaplingGrowth { get; set; } = true;

        public bool GrowthIgnoresStumps { get; set; } = false;

        public int SaveSprouts { get; set; } = 0;

        public bool NormalTreesGrowInWinter { get; set; } = true;

        public bool MushroomTreesGrowInWinter { get; set; } = false;

        public bool FruitTreesDontGrowInWinter { get; set; } = false;

        public bool BuffMahoganyTrees { get; set; } = false;

        public int ShakingSeedChance { get; set; } = 5;

        public bool FasterNormalTreeGrowth { get; set; } = false;

        public int FruitTreeGrowth { get; set; } = 0;

        private static string[] BoolChoices { get; set; } = new string[] { "Disabled", "Enabled" };

        private static string[] SSChoices { get; set; } = new string[] { "Disabled", "Hoe and pickaxe", "Hoe, pickaxe and scythe", "Hoe, pickaxe and all melee weapons" };

        private static string[] FTChoices { get; set; } = new string[] { "Default", "Twice as fast", "Half as fast" };

        public static void VerifyConfigValues(TreeOverhaulConfig config, TreeOverhaul mod)
        {
            bool invalidConfig = false;

            if (config.SaveSprouts < 0 || config.SaveSprouts > 3)
            {
                invalidConfig = true;
                config.SaveSprouts = 0;
            }

            if (config.FruitTreeGrowth < 0 || config.FruitTreeGrowth > 2)
            {
                invalidConfig = true;
                config.FruitTreeGrowth = 0;
            }

            if (config.ShakingSeedChance < 0)
            {
                invalidConfig = true;
                config.ShakingSeedChance = 0;
            }

            if (config.ShakingSeedChance > 100)
            {
                invalidConfig = true;
                config.ShakingSeedChance = 100;
            }

            if (invalidConfig)
            {
                mod.DebugLog("A config value was out of range and was reset.");
                mod.Helper.WriteConfig(config);
            }
        }

        public static void SetUpModConfigMenu(TreeOverhaulConfig config, TreeOverhaul mod)
        {
            GenericModConfigMenuAPI api = mod.Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.RegisterModConfig(manifest, () => config = new TreeOverhaulConfig(), () => mod.Helper.WriteConfig(config));

            api.RegisterLabel(manifest, "General Tweaks", null);

            api.RegisterChoiceOption(manifest, "Stop Seed Growth In Shade", "Seeds don't sprout in the 8 surrounding tiles of a tree", () => BoolToString(config.StopShadeSaplingGrowth), (string val) => config.StopShadeSaplingGrowth = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Growth Ignores Stumps", "Trees can grow even if a small stump is next to them", () => BoolToString(config.GrowthIgnoresStumps), (string val) => config.GrowthIgnoresStumps = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Save Sprouts From Tools", "Normal and fruit trees can't be killed by the selected tools", () => GetElementFromConfig(SSChoices, config.SaveSprouts), (string val) => config.SaveSprouts = GetIndexFromArrayElement(SSChoices, val), SSChoices);

            api.RegisterLabel(manifest, "Winter Tweaks", null);

            api.RegisterChoiceOption(manifest, "Normal Trees Grow In Winter", null, () => BoolToString(config.NormalTreesGrowInWinter), (string val) => config.NormalTreesGrowInWinter = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Mushroom Trees Grow In Winter", null, () => BoolToString(config.MushroomTreesGrowInWinter), (string val) => config.MushroomTreesGrowInWinter = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Fruit Trees Don't Grow In Winter", null, () => BoolToString(config.FruitTreesDontGrowInWinter), (string val) => config.FruitTreesDontGrowInWinter = StringToBool(val), BoolChoices);

            api.RegisterLabel(manifest, "Buffs and Nerfs", null);
            api.RegisterChoiceOption(manifest, "Buff Mahogany Tree Growth", "20% unfertilized and 100% fertilized (from 15% and 60%)", () => BoolToString(config.BuffMahoganyTrees), (string val) => config.BuffMahoganyTrees = StringToBool(val), BoolChoices);
            api.RegisterClampedOption(manifest, "Seed Chance From Shaking", "Chance that a seed drops from shaking a tree (default: 5%)", () => config.ShakingSeedChance, (int val) => config.ShakingSeedChance = val, 0, 100);
            api.RegisterChoiceOption(manifest, "Faster Normal Tree Growth", "Normal trees try to grow twice every day, still random whether they succeed", () => BoolToString(config.FasterNormalTreeGrowth), (string val) => config.FasterNormalTreeGrowth = StringToBool(val), BoolChoices);
            api.RegisterChoiceOption(manifest, "Fruit Tree Growth Options", null, () => GetElementFromConfig(FTChoices, config.FruitTreeGrowth), (string val) => config.FruitTreeGrowth = GetIndexFromArrayElement(FTChoices, val), FTChoices);
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
                return FTChoices[0];
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