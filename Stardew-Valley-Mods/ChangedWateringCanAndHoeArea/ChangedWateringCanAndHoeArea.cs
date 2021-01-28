using Harmony;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;

namespace ChangedWateringCanAndHoeArea
{
    public class ChangedWateringCanAndHoeArea : Mod
    {
        private static ChangedWateringCanAndHoeArea mod;

        public override void Entry(IModHelper helper)
        {
            mod = this;
            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);

            try
            {
                harmony.Patch(
                   original: AccessTools.Method(typeof(Tool), "tilesAffected"),
                   prefix: new HarmonyMethod(typeof(ChangedWateringCanAndHoeArea), nameof(PatchedTilesAffected))
                );
            }
            catch (Exception e)
            {
                ErrorLog("Error while trying to setup required patches:", e);
            }
        }

        public static bool PatchedTilesAffected(ref Tool __instance, ref List<Vector2> __result, ref Vector2 tileLocation, ref int power, ref Farmer who)
        {
            try
            {
                if (__instance is WateringCan)
                {
                    __result = PatchedWateringCanTilesAffected(ref tileLocation, ref power, ref who);
                    return false;
                }
                else if (__instance is Hoe)
                {
                    __result = PatchedHoeTilesAffected(ref tileLocation, ref power, ref who);
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

        public static List<Vector2> PatchedWateringCanTilesAffected(ref Vector2 tileLocation, ref int power, ref Farmer who)
        {
            power++;
            List<Vector2> tileLocations = new List<Vector2>();

            if (power >= 6)
            {
                Vector2 extremePowerPosition = Vector2.Zero;

                switch (who.FacingDirection)
                {
                    case 0:
                        extremePowerPosition = new Vector2(tileLocation.X, tileLocation.Y - 2f);
                        break;

                    case 1:
                        extremePowerPosition = new Vector2(tileLocation.X + 2f, tileLocation.Y);
                        break;

                    case 2:
                        extremePowerPosition = new Vector2(tileLocation.X, tileLocation.Y + 2f);
                        break;

                    case 3:
                        extremePowerPosition = new Vector2(tileLocation.X - 2f, tileLocation.Y);
                        break;

                    default:
                        break;
                }

                int x = (int)extremePowerPosition.X - 2;
                while ((float)x <= extremePowerPosition.X + 2f)
                {
                    int y = (int)extremePowerPosition.Y - 2;
                    while ((float)y <= extremePowerPosition.Y + 2f)
                    {
                        tileLocations.Add(new Vector2((float)x, (float)y));
                        y++;
                    }
                    x++;
                }

                return tileLocations;
            }
            else
            {
                tileLocations.Add(Vector2.Zero);

                if (power >= 2)
                {
                    tileLocations.Add(new Vector2(1f, 0f));
                    tileLocations.Add(new Vector2(-1f, 0));
                }
                if (power >= 3)
                {
                    tileLocations.Add(new Vector2(0f, -1f));
                    tileLocations.Add(new Vector2(1f, -1f));
                    tileLocations.Add(new Vector2(-1f, -1f));
                }
                if (power >= 4)
                {
                    tileLocations.Add(new Vector2(0f, -2f));
                    tileLocations.Add(new Vector2(1f, -2f));
                    tileLocations.Add(new Vector2(-1f, -2f));
                }
                if (power >= 5)
                {
                    for (int j = tileLocations.Count - 1; j >= 0; j--)
                    {
                        tileLocations.Add(tileLocations[j] + new Vector2(0f, -3));
                    }
                }

                if (who.FacingDirection == 1)
                {
                    for (int i = 0; i < tileLocations.Count; i++)
                    {
                        tileLocations[i] = new Vector2(-tileLocations[i].Y, -tileLocations[i].X);
                    }
                }
                else if (who.FacingDirection == 2)
                {
                    for (int i = 0; i < tileLocations.Count; i++)
                    {
                        tileLocations[i] = -tileLocations[i];
                    }
                }
                else if (who.FacingDirection == 3)
                {
                    for (int i = 0; i < tileLocations.Count; i++)
                    {
                        tileLocations[i] = new Vector2(tileLocations[i].Y, tileLocations[i].X);
                    }
                }

                for (int i = 0; i < tileLocations.Count; i++)
                {
                    tileLocations[i] += tileLocation;
                }

                return tileLocations;
            }
        }

        public static List<Vector2> PatchedHoeTilesAffected(ref Vector2 tileLocation, ref int power, ref Farmer who)
        {
            power++;
            List<Vector2> tileLocations = new List<Vector2>();
            tileLocations.Add(tileLocation);
            Vector2 extremePowerPosition = Vector2.Zero;
            if (who.FacingDirection == 0)
            {
                if (power >= 6)
                {
                    extremePowerPosition = new Vector2(tileLocation.X, tileLocation.Y - 2f);
                }
                else
                {
                    if (power >= 2)
                    {
                        tileLocations.Add(tileLocation + new Vector2(0f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(0f, -2f));
                    }
                    if (power >= 3)
                    {
                        tileLocations.Add(tileLocation + new Vector2(0f, -3f));
                        tileLocations.Add(tileLocation + new Vector2(0f, -4f));
                        tileLocations.Add(tileLocation + new Vector2(0f, -5f));
                    }
                    if (power >= 4)
                    {
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.Add(tileLocation + new Vector2(1f, -2f));
                        tileLocations.Add(tileLocation + new Vector2(1f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(1f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, -2f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, 0f));
                    }
                    if (power >= 5)
                    {
                        for (int i = tileLocations.Count - 1; i >= 0; i--)
                        {
                            tileLocations.Add(tileLocations[i] + new Vector2(0f, -3f));
                        }
                    }
                }
            }
            else if (who.FacingDirection == 1)
            {
                if (power >= 6)
                {
                    extremePowerPosition = new Vector2(tileLocation.X + 2f, tileLocation.Y);
                }
                else
                {
                    if (power >= 2)
                    {
                        tileLocations.Add(tileLocation + new Vector2(1f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(2f, 0f));
                    }
                    if (power >= 3)
                    {
                        tileLocations.Add(tileLocation + new Vector2(3f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(4f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(5f, 0f));
                    }
                    if (power >= 4)
                    {
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.Add(tileLocation + new Vector2(0f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(1f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(2f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(0f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(1f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(2f, 1f));
                    }
                    if (power >= 5)
                    {
                        for (int j = tileLocations.Count - 1; j >= 0; j--)
                        {
                            tileLocations.Add(tileLocations[j] + new Vector2(3f, 0f));
                        }
                    }
                }
            }
            else if (who.FacingDirection == 2)
            {
                if (power >= 6)
                {
                    extremePowerPosition = new Vector2(tileLocation.X, tileLocation.Y + 2f);
                }
                else
                {
                    if (power >= 2)
                    {
                        tileLocations.Add(tileLocation + new Vector2(0f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(0f, 2f));
                    }
                    if (power >= 3)
                    {
                        tileLocations.Add(tileLocation + new Vector2(0f, 3f));
                        tileLocations.Add(tileLocation + new Vector2(0f, 4f));
                        tileLocations.Add(tileLocation + new Vector2(0f, 5f));
                    }
                    if (power >= 4)
                    {
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.Add(tileLocation + new Vector2(1f, 2f));
                        tileLocations.Add(tileLocation + new Vector2(1f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(1f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, 2f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, 0f));
                    }
                    if (power >= 5)
                    {
                        for (int k = tileLocations.Count - 1; k >= 0; k--)
                        {
                            tileLocations.Add(tileLocations[k] + new Vector2(0f, 3f));
                        }
                    }
                }
            }
            else if (who.FacingDirection == 3)
            {
                if (power >= 6)
                {
                    extremePowerPosition = new Vector2(tileLocation.X - 2f, tileLocation.Y);
                }
                else
                {
                    if (power >= 2)
                    {
                        tileLocations.Add(tileLocation + new Vector2(-1f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(-2f, 0f));
                    }
                    if (power >= 3)
                    {
                        tileLocations.Add(tileLocation + new Vector2(-3f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(-4f, 0f));
                        tileLocations.Add(tileLocation + new Vector2(-5f, 0f));
                    }
                    if (power >= 4)
                    {
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.RemoveAt(tileLocations.Count - 1);
                        tileLocations.Add(tileLocation + new Vector2(0f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(-2f, -1f));
                        tileLocations.Add(tileLocation + new Vector2(0f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(-1f, 1f));
                        tileLocations.Add(tileLocation + new Vector2(-2f, 1f));
                    }
                    if (power >= 5)
                    {
                        for (int l = tileLocations.Count - 1; l >= 0; l--)
                        {
                            tileLocations.Add(tileLocations[l] + new Vector2(-3f, 0f));
                        }
                    }
                }
            }
            if (power >= 6)
            {
                tileLocations.Clear();
                int x = (int)extremePowerPosition.X - 2;
                while ((float)x <= extremePowerPosition.X + 2f)
                {
                    int y = (int)extremePowerPosition.Y - 2;
                    while ((float)y <= extremePowerPosition.Y + 2f)
                    {
                        tileLocations.Add(new Vector2((float)x, (float)y));
                        y++;
                    }
                    x++;
                }
            }
            return tileLocations;
        }
    }
}