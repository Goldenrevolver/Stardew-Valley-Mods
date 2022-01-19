namespace RingOverhaul
{
    using StardewModdingAPI;
    using System;
    using System.Diagnostics.CodeAnalysis;

    public interface IGenericModConfigMenuApi
    {
        void RegisterModConfig(IManifest mod, Action revertToDefault, Action saveToFile);

        void RegisterLabel(IManifest mod, string labelName, string labelDesc);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

        void AddParagraph(IManifest mod, Func<string> text);
    }

    /// <summary>
    /// Config file for the mod
    /// </summary>
    public class RingConfig
    {
        public bool CraftableGemRings { get; set; } = true;

        public bool MinorRingCraftingChanges { get; set; } = true;

        public bool RemoveCrabshellRingAndImmunityBandTooltipFromCombinedRing { get; set; } = true;

        public bool RemoveLuckyTooltipFromCombinedRing { get; set; } = false;

        public bool OldGlowStoneRingRecipe { get; set; } = false;

        public bool OldIridiumBandRecipe { get; set; } = false;

        public static void VerifyConfigValues(RingOverhaul mod)
        {
            try
            {
                mod.Helper.Content.InvalidateCache("Data/CraftingRecipes");
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

            api.AddBoolOption(manifest, () => config.CraftableGemRings, (bool val) => config.CraftableGemRings = val, () => mod.Helper.Translation.Get("ConfigCraftableGemRings"));
            api.AddBoolOption(manifest, () => config.MinorRingCraftingChanges, (bool val) => config.MinorRingCraftingChanges = val, () => mod.Helper.Translation.Get("ConfigMinorRingCraftingChanges"));
            api.AddBoolOption(manifest, () => config.RemoveCrabshellRingAndImmunityBandTooltipFromCombinedRing, (bool val) => config.RemoveCrabshellRingAndImmunityBandTooltipFromCombinedRing = val, () => mod.Helper.Translation.Get("ConfigRemoveCITooltip"));
            api.AddBoolOption(manifest, () => config.RemoveLuckyTooltipFromCombinedRing, (bool val) => config.RemoveLuckyTooltipFromCombinedRing = val, () => mod.Helper.Translation.Get("ConfigRemoveLTooltip"));
            api.AddBoolOption(manifest, () => config.OldGlowStoneRingRecipe, (bool val) => config.OldGlowStoneRingRecipe = val, () => mod.Helper.Translation.Get("ConfigOldGlowStoneRingRecipe"));
            api.AddBoolOption(manifest, () => config.OldIridiumBandRecipe, (bool val) => config.OldIridiumBandRecipe = val, () => mod.Helper.Translation.Get("ConfigOldIridiumBandRecipe"));

            api.AddParagraph(manifest, () => mod.Helper.Translation.Get("ConfigFeedback"));
        }
    }
}