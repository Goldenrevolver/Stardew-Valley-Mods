﻿namespace MaritimeSecrets
{
    using StardewModdingAPI;
    using System;
    using System.Diagnostics.CodeAnalysis;

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);
    }

    /// <summary>
    /// Config file for the mod
    /// </summary>
    public class MaritimeSecretsConfig
    {
        public string MarinerNameOverwrite { get; set; } = string.Empty;

        public int MarinerSpeechType { get; set; } = 0;

        public bool ChangePendantPriceToPearl { get; set; } = false;

        public bool CanBuyPendantWithoutHeartsAndHouseUpgrade { get; set; } = false;

        private static string[] SpeechChoices { get; set; } = new string[] { "Dynamic", "Modern", "Sailor" };

        public static void VerifyConfigValues(MaritimeSecretsConfig config, MaritimeSecrets mod)
        {
            if (config.MarinerSpeechType < 0 || config.MarinerSpeechType >= SpeechChoices.Length)
            {
                config.MarinerSpeechType = 0;
                mod.DebugLog("At least one config value was out of range and was reset.");
                mod.Helper.WriteConfig(config);
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:CodeMustNotContainMultipleStatementsOnOneLine", Justification = "Reviewed.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Necessary.")]
        public static void SetUpModConfigMenu(MaritimeSecretsConfig config, MaritimeSecrets mod)
        {
            IGenericModConfigMenuApi api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.Register(
                mod: manifest,
                reset: () => config = new MaritimeSecretsConfig(),
                save: () => { mod.Helper.WriteConfig(config); VerifyConfigValues(config, mod); }
            );

            api.AddTextOption(manifest, () => config.MarinerNameOverwrite, (string val) => config.MarinerNameOverwrite = val, () => mod.Helper.Translation.Get("ConfigMarinerNameOverwrite"), () => mod.Helper.Translation.Get("ConfigMarinerNameOverwriteDescription"));
            api.AddTextOption(manifest, () => GetElementFromConfig(SpeechChoices, config.MarinerSpeechType), (string val) => config.MarinerSpeechType = GetIndexFromArrayElement(SpeechChoices, val), () => mod.Helper.Translation.Get("ConfigMarinerSpeechType"), null, SpeechChoices, (s) => TranslateSpeechChoice(s, mod));
            api.AddBoolOption(manifest, () => config.ChangePendantPriceToPearl, (bool val) => config.ChangePendantPriceToPearl = val, () => mod.Helper.Translation.Get("ConfigChangePendantPriceToPearl"));
            api.AddBoolOption(manifest, () => config.CanBuyPendantWithoutHeartsAndHouseUpgrade, (bool val) => config.CanBuyPendantWithoutHeartsAndHouseUpgrade = val, () => mod.Helper.Translation.Get("ConfigCanBuyPendantWithoutHeartsAndHouseUpgrade"));
        }

        private static string TranslateSpeechChoice(string englishValue, MaritimeSecrets mod)
        {
            return mod.Helper.Translation.Get("ConfigSpeechType" + englishValue);
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