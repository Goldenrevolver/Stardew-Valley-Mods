﻿namespace ForageFantasy
{
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using StardewObject = StardewValley.Object;

    internal class BerryBushLogic
    {
        public static bool IsHarvestableBush(Bush bush)
        {
            return bush != null && !bush.townBush && bush.inBloom(Game1.GetSeasonForLocation(bush.currentLocation), Game1.dayOfMonth) && bush.size.Value != Bush.greenTeaBush && bush.size.Value != Bush.walnutBush;
        }

        public static void RewardBerryXP(ForageFantasy mod)
        {
            double chance = mod.Config.BerryBushChanceToGetXP / 100.0;

            if (mod.Config.BerryBushXPAmount > 0 && Game1.random.NextDouble() < chance)
            {
                Game1.player.gainExperience(2, mod.Config.BerryBushXPAmount);
            }
        }

        public static void ChangeBerryQualityAndGiveExp(Bush bush, ForageFantasy mod)
        {
            int shakeOff;

            string season = (bush.overrideSeason == -1) ? Game1.GetSeasonForLocation(bush.currentLocation) : Utility.getSeasonNameFromNumber(bush.overrideSeason);

            if (season == "spring")
            {
                shakeOff = 296;
            }
            else if (season == "fall")
            {
                shakeOff = 410;
            }
            else
            {
                return;
            }

            bool gaveExp = false;

            foreach (var item in bush.currentLocation.debris)
            {
                if (item != null && item.item != null && item.item.ParentSheetIndex == shakeOff)
                {
                    if (!gaveExp)
                    {
                        gaveExp = true;
                        RewardBerryXP(mod);
                    }

                    if (mod.Config.BerryBushQuality)
                    {
                        ((StardewObject)item.item).Quality = ForageFantasy.DetermineForageQuality(Game1.player);
                    }
                }
            }
        }
    }
}