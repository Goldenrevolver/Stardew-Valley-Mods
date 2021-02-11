namespace PermanentCookoutKit
{
    using Harmony;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.BellsAndWhistles;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using StardewObject = StardewValley.Object;

    public class PermanentCookoutKit : Mod, IAssetEditor
    {
        public CookoutKitConfig Config { get; set; }

        private static PermanentCookoutKit mod;

        public override void Entry(IModHelper helper)
        {
            mod = this;

            Config = Helper.ReadConfig<CookoutKitConfig>();

            CookoutKitConfig.VerifyConfigValues(Config, this);

            Helper.Events.GameLoop.GameLaunched += delegate { CookoutKitConfig.SetUpModConfigMenu(Config, this); };

            Helper.Events.GameLoop.DayEnding += delegate { SaveCookingKits(); };

            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);

            try
            {
                harmony.Patch(
                   original: AccessTools.Method(typeof(Torch), "draw", new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                   postfix: new HarmonyMethod(typeof(PermanentCookoutKit), nameof(Draw_Post))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Torch), "updateWhenCurrentLocation"),
                   postfix: new HarmonyMethod(typeof(PermanentCookoutKit), nameof(UpdateWhenCurrentLocation_Post))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Torch), "checkForAction"),
                   prefix: new HarmonyMethod(typeof(PermanentCookoutKit), nameof(CheckForAction_Pre))
                );
            }
            catch (Exception e)
            {
                ErrorLog("Error while trying to setup required patches:", e);
            }
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Data/CraftingRecipes");
        }

        public void Edit<T>(IAssetData asset)
        {
            if (asset.AssetNameEquals("Data/CraftingRecipes"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                data["Cookout Kit"] = "390 10 388 10 771 10 382 3 335 1/Field/926/false/Foraging 6/Cookout Kit";
            }
        }

        public void DebugLog(object o)
        {
            Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        public void ErrorLog(object o, Exception e = null)
        {
            string baseMessage = o == null ? "null" : o.ToString();

            string errorMessage = e == null ? string.Empty : $"\n{e.Message}\n{e.StackTrace}";

            Monitor.Log(baseMessage + errorMessage, LogLevel.Error);
        }

        private void SaveCookingKits()
        {
            foreach (var location in Game1.locations)
            {
                foreach (var item in location.Objects.Values)
                {
                    if (item.ParentSheetIndex == 278)
                    {
                        // turns out the fire, doesn't truly remove it
                        item.performRemoveAction(item.tileLocation, location);

                        item.destroyOvernight = false;
                    }
                }
            }
        }

        public static void Draw_Post(Torch __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            try
            {
                if (!Game1.eventUp || (Game1.currentLocation != null && Game1.currentLocation.currentEvent != null && Game1.currentLocation.currentEvent.showGroundObjects) || Game1.currentLocation.IsFarm)
                {
                    float draw_layer = Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f;

                    // draw the upper half of the cookout kit even if it's off
                    if (__instance.parentSheetIndex == 278 && !__instance.isOn)
                    {
                        Rectangle r = StardewObject.getSourceRectForBigCraftable(__instance.ParentSheetIndex + 1);
                        r.Height -= 16;
                        Vector2 scaleFactor = __instance.getScale();
                        scaleFactor *= 4f;
                        Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(x * 64), (float)(y * 64 - 64 + 12)));
                        Rectangle destination = new Rectangle((int)(position.X - scaleFactor.X / 2f) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(position.Y - scaleFactor.Y / 2f) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(64f + scaleFactor.X), (int)(64f + scaleFactor.Y / 2f));
                        spriteBatch.Draw(Game1.bigCraftableSpriteSheet, destination, new Rectangle?(r), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, draw_layer + 0.0028f);
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static void UpdateWhenCurrentLocation_Post(Torch __instance, GameLocation environment)
        {
            try
            {
                // remove the smoke from cookout kits that are off
                if (__instance.parentSheetIndex == 278 && !__instance.IsOn)
                {
                    // the condition for smoke to spawn in the overridden method
                    if (mod.Helper.Reflection.GetField<float>(__instance, "smokePuffTimer").GetValue() == 1000f)
                    {
                        // make sure it really is the smoke that was just spawned and then remove it
                        if (environment.temporarySprites.Any() && environment.temporarySprites.Last().initialPosition == __instance.tileLocation.Value * 64f + new Vector2(32f, -32f))
                        {
                            environment.temporarySprites.RemoveAt(environment.temporarySprites.Count - 1);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static bool CheckForAction_Pre(Torch __instance, Farmer who, bool justCheckingForActivity)
        {
            try
            {
                if (justCheckingForActivity)
                {
                    return true;
                }

                // check for ignition
                if (__instance.parentSheetIndex == 278 && !__instance.IsOn && who != null)
                {
                    int coalCount = mod.Config.CoalNeeded;
                    int fiberCount = mod.Config.FiberNeeded;
                    int woodCount = mod.Config.WoodNeeded;

                    float driftwoodMult = mod.Config.DriftwoodMultiplier;
                    float hardwoodMult = mod.Config.HardwoodMultiplier;

                    int driftwoodCount = (int)(woodCount / driftwoodMult);
                    int hardwoodCount = (int)(woodCount / hardwoodMult);

                    var coal = new StardewObject(382, 0);
                    var fiber = new StardewObject(771, 0);
                    var wood = new StardewObject(388, 0);

                    bool hasAllButWood = who.hasItemInInventory(coal.ParentSheetIndex, coalCount) && who.hasItemInInventory(fiber.ParentSheetIndex, fiberCount);

                    bool hasWood = false;

                    if (hasAllButWood)
                    {
                        hasWood = true;

                        // ordering is important
                        if (who.hasItemInInventory(169, driftwoodCount))
                        {
                            who.removeItemsFromInventory(169, driftwoodCount);
                        }
                        else if (who.hasItemInInventory(388, woodCount))
                        {
                            who.removeItemsFromInventory(388, woodCount);
                        }
                        else if (who.hasItemInInventory(709, hardwoodCount))
                        {
                            who.removeItemsFromInventory(709, hardwoodCount);
                        }
                        else
                        {
                            hasWood = false;
                        }
                    }

                    if (hasAllButWood && hasWood)
                    {
                        who.removeItemsFromInventory(coal.ParentSheetIndex, coalCount);
                        who.removeItemsFromInventory(fiber.ParentSheetIndex, fiberCount);

                        __instance.isOn.Value = true;

                        if (__instance.bigCraftable)
                        {
                            Game1.playSound("fireball");

                            __instance.initializeLightSource(__instance.tileLocation, false);
                            AmbientLocationSounds.addSound(__instance.tileLocation, 1);
                        }
                    }
                    else
                    {
                        Game1.showRedMessage($"{coalCount} {coal.DisplayName}, {fiberCount} {fiber.DisplayName}, {woodCount} {wood.DisplayName}");
                    }

                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }
    }
}