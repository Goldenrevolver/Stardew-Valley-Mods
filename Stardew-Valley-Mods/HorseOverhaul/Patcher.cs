namespace HorseOverhaul
{
    using Harmony;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.Buildings;
    using StardewValley.Characters;
    using StardewValley.Tools;
    using System;
    using System.Linq;

    public class Patcher
    {
        private static HorseOverhaul mod;

        public static void PatchAll(HorseOverhaul horseOverhaul)
        {
            mod = horseOverhaul;

            var harmony = HarmonyInstance.Create(mod.ModManifest.UniqueID);

            try
            {
                harmony.Patch(
                   original: AccessTools.Method(typeof(Farm), "performToolAction"),
                   postfix: new HarmonyMethod(typeof(Patcher), nameof(CheckForWaterHit))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Farmer), "getMovementSpeed"),
                   postfix: new HarmonyMethod(typeof(Patcher), nameof(ChangeHorseMovementSpeed))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Farmer), "showRiding"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(FixRidingPosition))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "squeezeForGate"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(DoNothing))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "draw", new Type[] { typeof(SpriteBatch) }),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(PreventEmoteDraw))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "draw", new Type[] { typeof(SpriteBatch) }),
                   postfix: new HarmonyMethod(typeof(Patcher), nameof(DrawHorse))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "checkAction"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(CheckForPetting))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Stable), "performActionOnDemolition"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(SaveItemsFromDemolition))
                );
            }
            catch (Exception e)
            {
                mod.ErrorLog("Error while trying to setup required patches:", e);
            }
        }

        public static void CheckForWaterHit(ref Tool t, ref int tileX, ref int tileY)
        {
            try
            {
                if (!Context.IsWorldReady || !mod.Config.Water)
                {
                    return;
                }

                if (t is WateringCan && (t as WateringCan).WaterLeft > 0)
                {
                    foreach (Building building in Game1.getFarm().buildings)
                    {
                        if (building is Stable stable && !HorseOverhaul.IsGarage(stable))
                        {
                            bool doesXHit = stable.tileX.Value + 1 == tileX || stable.tileX.Value + 2 == tileX;

                            if (doesXHit && stable.tileY.Value == tileY)
                            {
                                if (!mod.Config.DisableStableSpriteChanges)
                                {
                                    stable.texture = mod.FilledTroughTexture;
                                }

                                mod.Horses.Where(x => x.Horse == stable.getStableHorse()).Do(x => x.JustGotWater());
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

        public static bool SaveItemsFromDemolition(Stable __instance)
        {
            try
            {
                if (!mod.Config.SaddleBag || HorseOverhaul.IsGarage(__instance))
                {
                    return true;
                }

                HorseWrapper horseW = null;

                mod.Horses.Where(x => x.Horse == __instance.getStableHorse()).Do(x => horseW = x);

                if (horseW.SaddleBag != null)
                {
                    if (horseW.SaddleBag.items.Count > 0)
                    {
                        foreach (var item in horseW.SaddleBag.items)
                        {
                            Game1.player.team.returnedDonations.Add(item);
                            Game1.player.team.newLostAndFoundItems.Value = true;
                        }

                        horseW.SaddleBag.items.Clear();
                    }

                    horseW.SaddleBag = null;
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public static bool CheckForPetting(ref Horse __instance, ref bool __result, ref Farmer who)
        {
            try
            {
                if (!mod.Config.Petting || HorseOverhaul.IsTractor(__instance))
                {
                    return true;
                }

                HorseWrapper horseW = null;

                foreach (var item in mod.Horses)
                {
                    if (item.Horse == __instance)
                    {
                        horseW = item;
                        break;
                    }
                }

                if (__instance.getOwner() == who && horseW != null && !horseW.WasPet)
                {
                    horseW.JustGotPetted();

                    if (mod.Config.ThinHorse)
                    {
                        __instance.doEmote(20);
                    }

                    __result = true;
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

        public static void PreventEmoteDraw(ref Horse __instance, ref bool __state)
        {
            try
            {
                if (mod.Config.ThinHorse)
                {
                    __state = __instance.IsEmoting;
                    __instance.IsEmoting = false;
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static void DrawHorse(ref Horse __instance, ref SpriteBatch b, ref bool __state)
        {
            try
            {
                if (!mod.Config.ThinHorse)
                {
                    return;
                }

                if (__state)
                {
                    __instance.IsEmoting = true;
                    __state = false;

                    Vector2 emotePosition = __instance.getLocalPosition(Game1.viewport);

                    emotePosition.Y -= 96f;

                    switch (__instance.FacingDirection)
                    {
                        case 0:
                            emotePosition.Y -= 40f;
                            break;

                        case 1:
                            emotePosition.X += 40f;
                            emotePosition.Y -= 30f;
                            break;

                        case 2:
                            emotePosition.Y += 5f;
                            break;

                        case 3:
                            emotePosition.X -= 40f;
                            emotePosition.Y -= 30f;
                            break;

                        default:
                            break;
                    }

                    b.Draw(Game1.emoteSpriteSheet, emotePosition, new Microsoft.Xna.Framework.Rectangle?(new Rectangle((__instance.CurrentEmoteIndex * 16) % Game1.emoteSpriteSheet.Width, __instance.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, __instance.getStandingY() / 10000f);
                }

                if (__instance.FacingDirection == 2 && __instance.rider != null && !HorseOverhaul.IsTractor(__instance))
                {
                    b.Draw(mod.HorseSpriteWithHead, __instance.getLocalPosition(Game1.viewport) + new Vector2(16f, -24f - __instance.rider.yOffset), new Rectangle?(new Rectangle(160, 96, 9, 15)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (__instance.Position.Y + 64f) / 10000f);
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static bool DoNothing()
        {
            return !mod.Config.ThinHorse;
        }

        public static bool FixRidingPosition(Farmer __instance)
        {
            try
            {
                if (!mod.Config.ThinHorse)
                {
                    return true;
                }

                if (!__instance.isRidingHorse())
                {
                    return false;
                }

                __instance.mount.forceOneTileWide.Value = true;

                switch (__instance.FacingDirection)
                {
                    case 0:
                        __instance.FarmerSprite.setCurrentSingleFrame(113, 32000, false, false);
                        __instance.xOffset = 4f;
                        break;

                    case 1:
                        __instance.FarmerSprite.setCurrentSingleFrame(106, 32000, false, false);
                        __instance.xOffset = 5f;
                        break;

                    case 2:
                        __instance.FarmerSprite.setCurrentSingleFrame(107, 32000, false, false);
                        __instance.xOffset = 4f;
                        break;

                    case 3:
                        __instance.FarmerSprite.setCurrentSingleFrame(106, 32000, false, true);
                        __instance.xOffset = 0f;
                        break;
                }

                if (!__instance.isMoving())
                {
                    __instance.yOffset = 0f;
                    return false;
                }

                switch (__instance.mount.Sprite.currentAnimationIndex)
                {
                    case 0:
                        __instance.yOffset = 0f;
                        return false;

                    case 1:
                        __instance.yOffset = -4f;
                        return false;

                    case 2:
                        __instance.yOffset = -4f;
                        return false;

                    case 3:
                        __instance.yOffset = 0f;
                        return false;

                    case 4:
                        __instance.yOffset = 4f;
                        return false;

                    case 5:
                        __instance.yOffset = 4f;
                        return false;

                    default:
                        return false;
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public static void ChangeHorseMovementSpeed(ref Farmer __instance, ref float __result)
        {
            try
            {
                if (mod.Config.MovementSpeed)
                {
                    Horse horse = __instance.mount;

                    if (horse != null && !HorseOverhaul.IsTractor(horse))
                    {
                        float addedMovementSpeed = 0f;
                        mod.Horses.Where(x => x.Horse == horse).Do(x => addedMovementSpeed = x.GetMovementSpeedBonus());

                        __result += addedMovementSpeed;
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }
    }
}