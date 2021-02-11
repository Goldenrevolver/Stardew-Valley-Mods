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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;

    public class Patcher
    {
        private static HorseOverhaul mod;

        public static float HeadPosition => mod.Config.ThinHorse ? 16f : 48f;

        public static Vector2 HatVector => mod.Config.ThinHorse ? new Vector2(-8f, 0f) : Vector2.Zero;

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
                   original: AccessTools.Method(typeof(Horse), "checkAction"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(CheckForPetting))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Stable), "performActionOnDemolition"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(SaveItemsFromDemolition))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), "setMount"),
                    postfix: new HarmonyMethod(typeof(Patcher), nameof(FixSetMount))
                 );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Utility), "iterateChestsAndStorage"),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(IterateOverSaddles))
                );

                // thin horse patches

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
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(PreventBaseEmoteDraw))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "draw", new Type[] { typeof(SpriteBatch) }),
                   postfix: new HarmonyMethod(typeof(Patcher), nameof(DrawEmote))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "draw", new Type[] { typeof(SpriteBatch) }),
                   transpiler: new HarmonyMethod(typeof(Patcher), nameof(FixHeadAndHatPosition))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(Horse), "update", new Type[] { typeof(GameTime), typeof(GameLocation) }),
                   prefix: new HarmonyMethod(typeof(Patcher), nameof(DoMountingAnimation))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), "setMount"),
                    postfix: new HarmonyMethod(typeof(Patcher), nameof(FixSetMount))
                 );
            }
            catch (Exception e)
            {
                mod.ErrorLog("Error while trying to setup required patches:", e);
            }
        }

        public static void IterateOverSaddles(ref Action<Item> action)
        {
            try
            {
                var farmItems = Game1.getFarm().Objects.Values;

                // do this even if saddle bags are disabled
                foreach (var horse in mod.Horses)
                {
                    // check if it is placed on the farm, then it was checked already from the overridden method
                    if (horse != null && horse.SaddleBag != null && !farmItems.Contains(horse.SaddleBag))
                    {
                        foreach (Item item in horse.SaddleBag.items)
                        {
                            if (item != null)
                            {
                                action(item);
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
                if (HorseOverhaul.IsGarage(__instance))
                {
                    return true;
                }

                HorseWrapper horseW = null;

                mod.Horses.Where(x => x.Horse == __instance.getStableHorse()).Do(x => horseW = x);

                if (horseW != null && horseW.SaddleBag != null)
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
                        __instance.doEmote(Character.heartEmote);
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

        public static bool PreventBaseEmoteDraw(ref Horse __instance, ref bool __state)
        {
            try
            {
                if (mod.Config.ThinHorse)
                {
                    __state = __instance.IsEmoting;
                    __instance.IsEmoting = false;
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public static void DrawEmote(ref Horse __instance, ref SpriteBatch b, ref bool __state)
        {
            try
            {
                if (!mod.Config.ThinHorse || HorseOverhaul.IsTractor(__instance))
                {
                    return;
                }

                __instance.IsEmoting = __state;

                Horse horse = __instance;

                if (horse.IsEmoting)
                {
                    Vector2 emotePosition = horse.getLocalPosition(Game1.viewport);

                    emotePosition.Y -= 96f;

                    switch (horse.FacingDirection)
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

                    b.Draw(Game1.emoteSpriteSheet, emotePosition, new Microsoft.Xna.Framework.Rectangle?(new Rectangle((horse.CurrentEmoteIndex * 16) % Game1.emoteSpriteSheet.Width, horse.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, horse.getStandingY() / 10000f);
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        public static bool DoNothing()
        {
            try
            {
                return !mod.Config.ThinHorse;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
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
                        // up
                        __instance.FarmerSprite.setCurrentSingleFrame(113, 32000, false, false);
                        __instance.xOffset = 4f; // old: -6f, diff: +10
                        break;

                    case 1:
                        // right
                        __instance.FarmerSprite.setCurrentSingleFrame(106, 32000, false, false);
                        __instance.xOffset = 7f; // old: -3f, diff: +10
                        break;

                    case 2:
                        // down
                        __instance.FarmerSprite.setCurrentSingleFrame(107, 32000, false, false);
                        __instance.xOffset = 4f; // old: -6f, diff: +10
                        break;

                    case 3:
                        // left
                        __instance.FarmerSprite.setCurrentSingleFrame(106, 32000, false, true);
                        __instance.xOffset = -2f; // old: -12f, diff: +10
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

        public static bool DoMountingAnimation(ref Horse __instance)
        {
            try
            {
                Horse horse = __instance;

                // all the vanilla conditions to get to the case in question
                if (!mod.Config.ThinHorse || horse.rider == null || horse.rider.mount != null || !horse.rider.IsLocalPlayer || !horse.mounting || (horse.rider != null && horse.rider.hidden))
                {
                    return true;
                }

                var dir = __instance.FacingDirection;

                if (dir == 3)
                {
                    __instance.rider.xOffset = 0f;
                }
                else
                {
                    __instance.rider.xOffset = 4f;
                }

                var distance = horse.rider.Position.X - horse.Position.X;

                if (Math.Abs(distance) < 4)
                {
                    horse.rider.position.X = horse.Position.X;
                }
                else if (distance < 0)
                {
                    horse.rider.position.X += 4f;
                }
                else if (distance > 0)
                {
                    horse.rider.position.X -= 4f;
                }

                // invert whatever the overridden method will do
                if (horse.rider.Position.X < (horse.GetBoundingBox().X + 16 - 4))
                {
                    horse.rider.position.X -= 4f;
                }
                else if (horse.rider.Position.X > (horse.GetBoundingBox().X + 16 + 4))
                {
                    horse.rider.position.X += 4f;
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public static void FixSetMount(ref Farmer __instance, ref Horse mount)
        {
            try
            {
                if (mod.Config.ThinHorse && mount != null)
                {
                    var dir = __instance.mount.FacingDirection;

                    switch (dir)
                    {
                        case 1:
                            // counteracts the +8 from the horse update method to arrive at +4
                            __instance.xOffset = -4f;
                            break;

                        case 3:
                            __instance.xOffset = 0;
                            break;

                        default:
                            __instance.xOffset = 4f;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        private static IEnumerable<CodeInstruction> FixHeadAndHatPosition(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var list = instructions.ToList();

                bool foundHead = false;
                bool foundHat = false;

                for (int i = 0; i < list.Count; i++)
                {
                    if (!foundHead && list[i].opcode == OpCodes.Ldc_R4 && (float)list[i].operand >= 47.9f && (float)list[i].operand <= 48.1f)
                    {
                        var info = typeof(Patcher).GetProperty(nameof(HeadPosition)).GetGetMethod();
                        list[i] = new CodeInstruction(OpCodes.Call, info);

                        foundHead = true;
                    }

                    if (!foundHat && list[i].opcode == OpCodes.Call && list[i].operand.ToString().ToLower().Contains("get_zero"))
                    {
                        if (list[i + 1].opcode == OpCodes.Stloc_1 && list[i + 2].opcode == OpCodes.Ldarg_0)
                        {
                            var info = typeof(Patcher).GetProperty(nameof(HatVector)).GetGetMethod();
                            list[i] = new CodeInstruction(OpCodes.Call, info);

                            foundHat = true;
                        }
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return instructions;
            }
        }
    }
}