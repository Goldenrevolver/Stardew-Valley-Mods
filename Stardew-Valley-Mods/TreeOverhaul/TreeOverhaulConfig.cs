namespace TreeOverhaul
{
    using StardewModdingAPI;
    using System;
    using System.Diagnostics.CodeAnalysis;

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string> formatValue = null, string fieldId = null);

        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string> tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string> formatValue = null, string fieldId = null);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);
    }

    /// <summary>
    /// Config file for the mod
    /// </summary>
    public class TreeOverhaulConfig
    {
        public bool StopShadeSaplingGrowth { get; set; } = true;

        public bool GrowthIgnoresStumps { get; set; } = true;

        public bool GrowthRespectsFruitTrees { get; set; } = true;

        public bool NormalTreesGrowInWinter { get; set; } = true;

        public bool MushroomTreesGrowInWinter { get; set; } = false;

        public bool BuffMahoganyTrees { get; set; } = false;

        public bool UseCustomShakingSeedChance { get; set; } = false;

        public bool UseCustomTreeGrowthChance { get; set; } = false;

        public int CustomTreeGrowthChance { get; set; } = 20;

        public int CustomShakingSeedChance { get; set; } = 5;

        public int SaveSprouts { get; set; } = 0;

        private static string[] SSChoices { get; set; } = new string[] { "Disabled", "Hoe And Pickaxe", "Hoe, Pickaxe And Scythe", "Hoe, Pickaxe And All Melee Weapons" };

        public static void VerifyConfigValues(TreeOverhaulConfig config, TreeOverhaul mod)
        {
            bool invalidConfig = false;

            if (config.SaveSprouts < 0 || config.SaveSprouts > 3)
            {
                invalidConfig = true;
                config.SaveSprouts = 0;
            }

            if (config.CustomTreeGrowthChance < 0)
            {
                invalidConfig = true;
                config.CustomTreeGrowthChance = 0;
            }

            if (config.CustomTreeGrowthChance > 100)
            {
                invalidConfig = true;
                config.CustomTreeGrowthChance = 100;
            }

            if (config.CustomShakingSeedChance < 0)
            {
                invalidConfig = true;
                config.CustomShakingSeedChance = 0;
            }

            if (config.CustomShakingSeedChance > 100)
            {
                invalidConfig = true;
                config.CustomShakingSeedChance = 100;
            }

            if (invalidConfig)
            {
                mod.DebugLog("At least one config value was out of range and was reset.");
                mod.Helper.WriteConfig(config);
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:CodeMustNotContainMultipleStatementsOnOneLine", Justification = "Reviewed.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Necessary.")]
        public static void SetUpModConfigMenu(TreeOverhaulConfig config, TreeOverhaul mod)
        {
            IGenericModConfigMenuApi api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.Register(
                mod: manifest,
                reset: () =>
                {
                    config = new TreeOverhaulConfig();
                    mod.Helper.GameContent.InvalidateCacheAndLocalized("Data/WildTrees");
                },
                save: () =>
                {
                    mod.Helper.WriteConfig(config);
                    VerifyConfigValues(config, mod);
                    mod.Helper.GameContent.InvalidateCacheAndLocalized("Data/WildTrees");
                }
            );

            api.AddSectionTitle(manifest, () => "General Tweaks", null);

            api.AddBoolOption(manifest, () => config.StopShadeSaplingGrowth, (bool val) => config.StopShadeSaplingGrowth = val, () => "Stop Seed Growth In Shade", () => "Seeds don't sprout in the 8 surrounding tiles of a tree");
            api.AddBoolOption(manifest, () => config.GrowthIgnoresStumps, (bool val) => config.GrowthIgnoresStumps = val, () => "Growth Ignores Stumps", () => "Trees can fully grow even if a small stump is next to them");
            api.AddBoolOption(manifest, () => config.GrowthRespectsFruitTrees, (bool val) => config.GrowthRespectsFruitTrees = val, () => "Growth Respects Fruit Trees", () => "Trees can't fully grow if a mature fruit tree is next to them");

            api.AddTextOption(manifest, () => GetElementFromConfig(SSChoices, config.SaveSprouts), (string val) => config.SaveSprouts = GetIndexFromArrayElement(SSChoices, val), () => "Save Sprouts From Tools", () => "Normal and fruit trees can't be killed by the selected tools", SSChoices);

            api.AddSectionTitle(manifest, () => "Winter Tweaks", null);

            api.AddBoolOption(manifest, () => config.NormalTreesGrowInWinter, (bool val) => config.NormalTreesGrowInWinter = val, () => "Normal Trees Grow In Winter", null);
            api.AddBoolOption(manifest, () => config.MushroomTreesGrowInWinter, (bool val) => config.MushroomTreesGrowInWinter = val, () => "Mushroom Trees Grow In Winter", null);

            api.AddSectionTitle(manifest, () => "Buffs And Nerfs", null);

            api.AddBoolOption(manifest, () => config.BuffMahoganyTrees, (bool val) => config.BuffMahoganyTrees = val, () => "Buff Mahogany Tree Growth", () => "20% unfertilized and 100% fertilized (from 15% and 60%)");
            api.AddBoolOption(manifest, () => config.UseCustomShakingSeedChance, (bool val) => config.UseCustomShakingSeedChance = val, () => "Use Custom Shaking Chance", () => "Changes chance that a seed drops from shaking a tree to the config 'Seed Chance From Shaking'.");
            api.AddNumberOption(manifest, () => config.CustomShakingSeedChance, (int val) => config.CustomShakingSeedChance = val, () => "Seed Chance From Shaking", () => "Chance that a seed drops from shaking a tree if 'Use Custom Shaking Chance' is true (default: 5%)", 0, 100);
            api.AddBoolOption(manifest, () => config.UseCustomTreeGrowthChance, (bool val) => config.UseCustomTreeGrowthChance = val, () => "Use Custom Tree Growth Chance", () => "Changes tree growth chance (including mahogany and mushroom trees) to the config 'Custom Tree Growth Chance'.");
            api.AddNumberOption(manifest, () => config.CustomTreeGrowthChance, (int val) => config.CustomTreeGrowthChance = val, () => "Custom Tree Growth Chance", () => "Changes tree growth chance (including mahogany and mushroom trees) if 'Enable Custom Tree Growth Chance' is true (default: 20%)", 0, 100);
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
            var index = Array.IndexOf(options, element);

            return index == -1 ? 0 : index;
        }
    }
}