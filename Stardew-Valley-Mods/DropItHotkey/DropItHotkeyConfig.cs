namespace DropItHotkey
{
    using StardewModdingAPI;
    using StardewModdingAPI.Utilities;
    using System;

    public interface IGenericModConfigMenuAPI
    {
        void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

        void RegisterModConfig(IManifest mod, Action revertToDefault, Action saveToFile);

        void SetTitleScreenOnlyForNextOptions(IManifest mod, bool titleScreenOnly);
    }

    public class Config
    {
        public KeybindList DropKey { get; set; } = KeybindList.Parse("LeftStick");

        public static void SetUpModConfigMenu(Config config, DropItHotkey mod)
        {
            IGenericModConfigMenuAPI api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

            if (api == null)
            {
                return;
            }

            var manifest = mod.ModManifest;

            api.RegisterModConfig(
                manifest,
                () => config = new Config(),
                delegate
                {
                    mod.Helper.WriteConfig(config);
                });

            api.SetTitleScreenOnlyForNextOptions(manifest, false);

            api.AddKeybindList(manifest, () => config.DropKey, (KeybindList keybindList) => config.DropKey = keybindList, () => "Drop Key");
        }
    }
}