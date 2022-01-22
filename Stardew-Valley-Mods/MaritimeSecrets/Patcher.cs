using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using System;
using xTile.Dimensions;
using StardewObject = StardewValley.Object;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace MaritimeSecrets
{
    internal class Patcher
    {
        private static MaritimeSecrets mod;

        private static readonly string summerForageCalendarKey = $"{mod?.ModManifest?.UniqueID}/SummerForageCalendar";
        private static readonly string receivedLifeSaverKey = $"{mod?.ModManifest?.UniqueID}/ReceivedLifeSaver";
        private static readonly string wormBinUpgradeKey = $"{mod?.ModManifest?.UniqueID}/WormBinUpgrade";
        private static readonly XnaRectangle summerForage = new(144, 256, 16, 16);
        private const int driftWoodId = 169;

        public static void PatchAll(MaritimeSecrets maritimeSecrets)
        {
            mod = maritimeSecrets;

            var harmony = new Harmony(mod.ModManifest.UniqueID);

            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(Beach), nameof(Beach.checkAction)),
                    prefix: new HarmonyMethod(typeof(Patcher), nameof(Beach_CheckAction_Prefix)));

                harmony.Patch(original: AccessTools.Method(typeof(Billboard), nameof(Billboard.performHoverAction)),
                    postfix: new HarmonyMethod(typeof(Patcher), nameof(Patcher.Billboard_PerformHoverAction_Postfix)));

                harmony.Patch(original: AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.getFish)),
                    postfix: new HarmonyMethod(typeof(Patcher), nameof(Patcher.BoatTunnel_GetFish_Postfix)));

                harmony.Patch(
                   original: AccessTools.Method(typeof(Billboard), nameof(Billboard.draw), new Type[] { typeof(SpriteBatch) }),
                   postfix: new HarmonyMethod(typeof(Patcher), nameof(Billboard_Draw_Postfix)));

                harmony.Patch(
                   original: AccessTools.Method(typeof(StardewObject), nameof(StardewObject.performObjectDropInAction), new[] { typeof(Item), typeof(bool), typeof(Farmer) }),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(WormBin_PerformObjectDropInAction)));
            }
            catch (Exception e)
            {
                mod.ErrorLog("Error while trying to setup required patches:", e);
            }
        }

        public static bool WormBin_PerformObjectDropInAction(ref StardewObject __instance, ref bool __result, ref Item dropInItem, ref bool probe, ref Farmer who)
        {
            try
            {
                if (!probe && __instance.name.Equals("Worm Bin") && who?.modData?.ContainsKey(wormBinUpgradeKey) == true)
                {
                    if (__instance.heldObject.Value != null && !__instance.readyForHarvest.Value && __instance.heldObject.Value.Stack <= 5)
                    {
                        if (dropInItem is StardewObject o && o.ParentSheetIndex == driftWoodId && who.IsLocalPlayer)
                        {
                            __instance.ConsumeInventoryItem(who, driftWoodId, 1);

                            who.currentLocation.playSound("slimeHit", NetAudio.SoundContext.Default);

                            if (__instance.heldObject.Value.Stack < 4)
                            {
                                __instance.heldObject.Value.Stack = 4;
                            }

                            __instance.heldObject.Value.Stack *= 2;

                            __result = false;
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public static void Billboard_PerformHoverAction_Postfix(ref Billboard __instance, int x, int y, ref string ___hoverText)
        {
            try
            {
                if (__instance.calendarDays != null && Game1.player?.modData?.ContainsKey(summerForageCalendarKey) == true)
                {
                    for (int day = 0; day < __instance.calendarDays.Count;)
                    {
                        ClickableTextureComponent c = __instance.calendarDays[day++];

                        if (c.bounds.Contains(x, y))
                        {
                            if (Game1.IsSummer && 12 <= day && day <= 14)
                            {
                                if (___hoverText.Length > 0)
                                {
                                    ___hoverText += Environment.NewLine;
                                }

                                ___hoverText += mod.Helper.Translation.Get("GreenOcean");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static void Billboard_Draw_Postfix(Billboard __instance, SpriteBatch b, bool ___dailyQuestBoard, string ___hoverText)
        {
            try
            {
                if (!___dailyQuestBoard)
                {
                    for (int day = 1; day <= 28; day++)
                    {
                        XnaRectangle toDraw = XnaRectangle.Empty;

                        if (Game1.IsSummer && 12 <= day && day <= 14 && Game1.player?.modData?.ContainsKey(summerForageCalendarKey) == true)
                        {
                            toDraw = summerForage;
                        }

                        if (toDraw != XnaRectangle.Empty)
                        {
                            Utility.drawWithShadow(b, Game1.objectSpriteSheet, new Vector2((float)(__instance.calendarDays[day - 1].bounds.X + 12), (float)(__instance.calendarDays[day - 1].bounds.Y + 60) - Game1.dialogueButtonScale / 2f), toDraw, Color.White, 0f, Vector2.Zero, 2f, false, 1f, -1, -1, 0.35f);

                            Game1.mouseCursorTransparency = 1f;
                            __instance.drawMouse(b);
                            if (___hoverText.Length > 0)
                            {
                                IClickableMenu.drawHoverText(b, ___hoverText, Game1.dialogueFont);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static void BoatTunnel_GetFish_Postfix(Farmer who, StardewObject __result)
        {
            try
            {
                // if is lifesaver item
                if (__result is Furniture furniture && furniture.ParentSheetIndex == 2418)
                {
                    if (mod?.ModManifest?.UniqueID != null)
                    {
                        who.modData[receivedLifeSaverKey] = "true";
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static bool Beach_CheckAction_Prefix(Beach __instance, Location tileLocation, Farmer who, NPC ___oldMariner, ref bool __result)
        {
            try
            {
                if (who != null && ___oldMariner != null && ___oldMariner.getTileX() == tileLocation.X && ___oldMariner.getTileY() == tileLocation.Y && mod?.ModManifest?.UniqueID != null)
                {
                    if (!who.modData.ContainsKey(mod.talkedToMarinerTodayKey))
                    {
                        who.modData[mod.talkedToMarinerTodayKey] = "true";

                        __result = true;

                        if (!who.modData.ContainsKey(summerForageCalendarKey))
                        {
                            who.modData[summerForageCalendarKey] = "true";
                            Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("SummerForageCalendar")));
                            return false;
                        }

                        // unlike the netfield, the fishing level property also adds buffs and enchantments, crafting recipe OR check is for mod compatibility
                        if ((who.fishingLevel.Value >= 8 || who.craftingRecipes.ContainsKey("Worm Bin")) && !who.modData.ContainsKey(wormBinUpgradeKey))
                        {
                            who.modData[wormBinUpgradeKey] = "true";
                            Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("WormBinUpgrade")));
                            return false;
                        }

                        if (who.hasMagnifyingGlass && !who.secretNotesSeen.Contains(15))
                        {
                            who.secretNotesSeen.Add(15);
                            Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("MermaidSecretNote")));
                            return false;
                        }

                        if (Game1.whichFarm == 6 && !who.mailReceived.Contains("gotBoatPainting"))
                        {
                            Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("BeachFarmBoatPainting")));
                            return false;
                        }

                        if (Utility.doesAnyFarmerHaveOrWillReceiveMail("seenBoatJourney") && !who.modData.ContainsKey(receivedLifeSaverKey))
                        {
                            Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("WillyLifeSaver")));
                            return false;
                        }

                        // arbitrary max number (it's about 500*2)
                        var whichSupplyCrate = Game1.random.Next(922, 925);
                        int maxTries = __instance.Map.Layers[0].LayerWidth * __instance.Map.Layers[0].LayerHeight * 2;
                        int tries = 0;
                        for (; tries < maxTries; tries++)
                        {
                            Vector2 randomPosition = __instance.getRandomTile();
                            randomPosition.Y /= 2f;
                            randomPosition.X /= 2f;

                            if (randomPosition.X < 10)
                            {
                                randomPosition.X += 10;
                            }

                            if (randomPosition.Y < 10)
                            {
                                randomPosition.Y += 30;
                            }

                            string prop = __instance.doesTileHaveProperty((int)randomPosition.X, (int)randomPosition.Y, "Type", "Back");

                            if (__instance.isTileLocationTotallyClearAndPlaceable(randomPosition) && (prop == null || !prop.Equals("Wood")))
                            {
                                __instance.dropObject(new StardewObject(whichSupplyCrate, 1, false, -1, 0)
                                {
                                    Fragility = 2,
                                    MinutesUntilReady = 3
                                }, randomPosition * 64f, Game1.viewport, true, null);
                                break;
                            }
                        }

                        // if placed a crate
                        if (tries < maxTries)
                        {
                            Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("BeachSupplyCrate")));
                            return false;
                        }

                        // if somehow failed to place a crate
                        Game1.drawObjectDialogue(Game1.parseText(mod.Helper.Translation.Get("NoSecret")));
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }
    }
}