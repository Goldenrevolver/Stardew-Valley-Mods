namespace RingOverhaul
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using System;
    using System.Diagnostics.CodeAnalysis;

    public interface IGenericModConfigMenuApi
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

        void AddParagraph(IManifest mod, Func<string> text);
    }

    /// <summary>
    /// Config file for the mod
    /// </summary>
    public class RingConfig
    {
        public bool CraftableGemRings { get; set; } = true;

        public bool MinorRingCraftingChanges { get; set; } = true;

        public bool OldGlowStoneRingRecipe { get; set; } = false;

        public bool OldIridiumBandRecipe { get; set; } = false;

        public static void VerifyConfigValues(RingOverhaul mod)
        {
            try
            {
                // CommonFiddleheadFern and ForageSurvivalBurger
                mod.Helper.Content.InvalidateCache("Data/CraftingRecipes");

                // Tapper days needed changes
                mod.Helper.Content.InvalidateCache("Data/ObjectInformation");
            }
            catch (Exception e)
            {
                mod.DebugLog($"Exception when trying to invalidate cache on config change {e}");
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:CodeMustNotContainMultipleStatementsOnOneLine", Justification = "Reviewed.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Necessary.")]
        public static void SetUpModConfigMenu(RingConfig config, RingOverhaul mod)
        {
            IGenericModConfigMenuApi api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.RegisterModConfig(manifest, () => config = new RingConfig(), delegate { mod.Helper.WriteConfig(config); VerifyConfigValues(mod); });

            api.RegisterLabel(manifest, "Crafting", null);

            api.RegisterSimpleOption(manifest, "Craftable Gem Rings", null, () => config.CraftableGemRings, (bool val) => config.CraftableGemRings = val);
            api.RegisterSimpleOption(manifest, "Minor Ring Crafting Changes", null, () => config.MinorRingCraftingChanges, (bool val) => config.MinorRingCraftingChanges = val);
            api.RegisterSimpleOption(manifest, "Old Glow Stone Ring Recipe", null, () => config.OldGlowStoneRingRecipe, (bool val) => config.OldGlowStoneRingRecipe = val);
            api.RegisterSimpleOption(manifest, "Old Iridium Band Recipe", null, () => config.OldIridiumBandRecipe, (bool val) => config.OldIridiumBandRecipe = val);

            api.AddParagraph(manifest, () => "Everything else about the mod can currently not be configured. Feedback is welcome.");
        }
    }
}