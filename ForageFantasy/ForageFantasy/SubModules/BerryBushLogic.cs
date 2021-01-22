namespace ForageFantasy
{
    using Microsoft.Xna.Framework;
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using System.Linq;

    internal class BerryBushLogic
    {
        public static void OnDayStarted(ForageFantasy mod, GameLocation location)
        {
            if (mod.Config.BerryBushQuality && (Game1.currentSeason == "spring" || Game1.currentSeason == "fall"))
            {
                foreach (Bush bush in location.largeTerrainFeatures.OfType<Bush>())
                {
                    if (bush != null && !bush.townBush && bush.tileSheetOffset == 1 && bush.inBloom(Game1.GetSeasonForLocation(bush.currentLocation), Game1.dayOfMonth))
                    {
                        if (bush.size != 3 && bush.size != 4)
                        {
                            mod.BushesReadyForHarvest.Add(bush);
                        }
                    }
                }
            }
        }

        public static void FindBushShake(ForageFantasy mod)
        {
            // this feature is unnecessary if you are foraging level 10 and have the botanist perk
            if (!Context.IsWorldReady || Game1.player.professions.Contains(16))
            {
                return;
            }

            for (int i = mod.BushesReadyForHarvest.Count - 1; i >= 0; i--)
            {
                Bush bush = mod.BushesReadyForHarvest[i];

                if (bush != null && bush.tileSheetOffset.Value == 0)
                {
                    mod.BushesReadyForHarvest.RemoveAt(i);

                    var maxShake = mod.Helper.Reflection.GetField<float>(bush, "maxShake");

                    if (maxShake.GetValue() > 0f)
                    {
                        ChangeBerryQualityAndRewardXP(bush, mod);
                    }
                }
            }
        }

        private static void ChangeBerryQualityAndRewardXP(Bush bush, ForageFantasy mod)
        {
            Farmer closestPlayer = null;
            float smallestDistance = float.MaxValue;

            foreach (Farmer player in Game1.getOnlineFarmers())
            {
                if (player != null && player.currentLocation == bush.currentLocation)
                {
                    float currentDistance = Vector2.Distance(player.getTileLocation(), bush.tilePosition);

                    if (currentDistance < smallestDistance)
                    {
                        closestPlayer = player;
                        smallestDistance = currentDistance;
                    }
                }
            }

            if (closestPlayer != null && closestPlayer == Game1.player)
            {
                double chance = mod.Config.BerryBushChanceToGetXP / 100.0;

                if (mod.Config.BerryBushXPAmount > 0 && Game1.random.NextDouble() < chance)
                {
                    Game1.player.gainExperience(2, mod.Config.BerryBushXPAmount);
                }

                if (mod.Config.BerryBushQuality)
                {
                    int shakeOff = -1;

                    if (Game1.currentSeason == "spring")
                    {
                        shakeOff = 296;
                    }
                    else if (Game1.currentSeason == "fall")
                    {
                        shakeOff = 410;
                    }

                    foreach (var item in bush.currentLocation.debris)
                    {
                        if (item.item.ParentSheetIndex == shakeOff)
                        {
                            int quality = ForageFantasy.DetermineForageQuality(closestPlayer);

                            ((Object)item.item).Quality = quality;
                        }
                    }
                }
            }
        }
    }
}