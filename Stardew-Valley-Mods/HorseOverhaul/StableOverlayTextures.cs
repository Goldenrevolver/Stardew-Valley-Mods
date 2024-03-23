using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Buffers;
using System.Collections.Generic;

namespace HorseOverhaul
{
    internal class StableOverlayTextures
    {
        public static Texture2D GetCurrentStableTexture(HorseOverhaul mod)
        {
            if (mod.UsingMyStableTextures)
            {
                return mod.Helper.ModContent.Load<Texture2D>("assets/stable.png");
            }
            else
            {
                var baseGameTexture = mod.Helper.GameContent.Load<Texture2D>("Buildings/Stable");

                int count = baseGameTexture.Width * baseGameTexture.Height;

                var stableCopy = new Texture2D(Game1.graphics.GraphicsDevice, baseGameTexture.Width, baseGameTexture.Height) { Name = mod.ModManifest.UniqueID + ".StableCopy" };

                var textureData = ArrayPool<Color>.Shared.Rent(count);
                baseGameTexture.GetData(textureData, 0, count);
                stableCopy.SetData(textureData, 0, count);
                ArrayPool<Color>.Shared.Return(textureData);

                return stableCopy;
            }
        }

        internal static Texture2D MergeTextures(IRawTextureData overlay, Texture2D oldTexture)
        {
            if (overlay == null || oldTexture == null)
            {
                return oldTexture;
            }

            int count = oldTexture.Width * oldTexture.Height;
            var newData = overlay.Data;

            var origData = ArrayPool<Color>.Shared.Rent(count);
            oldTexture.GetData(origData, 0, count);

            if (newData == null || origData == null)
            {
                ArrayPool<Color>.Shared.Return(origData);
                return oldTexture;
            }

            for (int i = 0; i < count; i++)
            {
                ref Color newValue = ref newData[i];
                if (newValue.A != 0)
                {
                    origData[i] = newValue;
                }
            }

            oldTexture.SetData(origData, 0, count);

            ArrayPool<Color>.Shared.Return(origData);
            return oldTexture;
        }

        internal static void SetOverlays(HorseOverhaul mod)
        {
            if (mod.Config.SaddleBag && mod.Config.VisibleSaddleBags != SaddleBagOption.Disabled.ToString())
            {
                mod.SaddleBagOverlay = mod.Helper.ModContent.Load<Texture2D>($"assets/saddlebags_{mod.Config.VisibleSaddleBags.ToLower()}.png");
                mod.IsUsingHorsemanship = mod.Helper.ModRegistry.IsLoaded("red.horsemanship");
            }

            if (!mod.Config.Water || mod.Config.DisableStableSpriteChanges)
            {
                return;
            }

            mod.SeasonalVersion = SeasonalVersion.None;

            mod.UsingMyStableTextures = false;

            mod.FilledTroughOverlay = null;

            if (mod.Helper.ModRegistry.IsLoaded("sonreirblah.JBuildings"))
            {
                // seasonal overlays are assigned in LateDayStarted
                mod.EmptyTroughOverlay = null;

                mod.SeasonalVersion = SeasonalVersion.Sonr;
                return;
            }

            if (mod.Helper.ModRegistry.IsLoaded("Oklinq.CleanStable"))
            {
                mod.EmptyTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/overlay_empty.png");

                return;
            }

            if (mod.Helper.ModRegistry.IsLoaded("Elle.SeasonalBuildings"))
            {
                var data = mod.Helper.ModRegistry.Get("Elle.SeasonalBuildings");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var list = mod.ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "color palette", "stable" }, data.Manifest.Name, false);

                    if (list["stable"].ToLower() != "false")
                    {
                        mod.EmptyTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/elle/overlay_empty_{list["color palette"]}.png");

                        return;
                    }
                }
            }

            if (mod.Helper.ModRegistry.IsLoaded("Elle.SeasonalVanillaBuildings"))
            {
                var data = mod.Helper.ModRegistry.Get("Elle.SeasonalVanillaBuildings");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var list = mod.ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "stable" }, data.Manifest.Name, false);

                    if (list["stable"].ToLower() == "true")
                    {
                        mod.FilledTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/overlay_filled_tone.png");
                        mod.EmptyTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/overlay_empty_tone.png");

                        return;
                    }
                }
            }

            if (mod.Helper.ModRegistry.IsLoaded("Gweniaczek.Medieval_stables"))
            {
                IModInfo data = mod.Helper.ModRegistry.Get("Gweniaczek.Medieval_stables");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var dict = mod.ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "stableOption" }, data.Manifest.Name, false);

                    SetupGwenTextures(mod, dict);

                    return;
                }
            }

            if (mod.Helper.ModRegistry.IsLoaded("Gweniaczek.Medieval_buildings"))
            {
                var data = mod.Helper.ModRegistry.Get("Gweniaczek.Medieval_buildings");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var dict = mod.ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "buildingsReplaced", "stableOption" }, data.Manifest.Name, false);

                    if (dict["buildingsReplaced"].Contains("stable"))
                    {
                        SetupGwenTextures(mod, dict);

                        return;
                    }
                }
            }

            if (mod.Helper.ModRegistry.IsLoaded("magimatica.SeasonalVanillaBuildings") || mod.Helper.ModRegistry.IsLoaded("red.HudsonValleyBuildings"))
            {
                mod.EmptyTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/overlay_empty_no_bucket.png");

                mod.SeasonalVersion = SeasonalVersion.Magimatica;

                return;
            }

            // no compatible texture mod found, so we will use mine
            mod.UsingMyStableTextures = true;

            mod.EmptyTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/overlay_empty.png");
        }

        private static void SetupGwenTextures(HorseOverhaul mod, Dictionary<string, string> dict)
        {
            if (dict["stableOption"] == "4")
            {
                mod.FilledTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/gwen/overlay_{dict["stableOption"]}_full.png");
            }

            mod.EmptyTroughOverlay = mod.Helper.ModContent.Load<IRawTextureData>($"assets/gwen/overlay_{dict["stableOption"]}.png");

            mod.SeasonalVersion = SeasonalVersion.Gwen;
            mod.GwenOption = dict["stableOption"];
        }
    }
}