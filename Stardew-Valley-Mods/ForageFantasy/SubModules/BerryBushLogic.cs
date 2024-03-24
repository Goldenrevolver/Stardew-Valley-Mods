namespace ForageFantasy
{
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using StardewObject = StardewValley.Object;

    internal class BerryBushLogic
    {
        internal const string springBerries = "(O)296";
        internal const string fallBerries = "(O)410";

        public static bool IsHarvestableBush(Bush bush)
        {
            return bush != null && !bush.townBush.Value && bush.inBloom() && bush.size.Value != Bush.greenTeaBush && bush.size.Value != Bush.walnutBush;
        }

        public static void ChangeBerryQualityAndGiveExp(Bush bush, ForageFantasyConfig config)
        {
            if (!config.BerryBushQuality)
            {
                return;
            }

            string shakeOff;

            var season = bush.Location.GetSeason();

            switch (season)
            {
                case Season.Spring:
                    shakeOff = springBerries;
                    break;

                case Season.Fall:
                    shakeOff = fallBerries;
                    break;

                default:
                    return;
            }

            // change quality of every nearby matching berry debris
            foreach (var item in bush.Location.debris)
            {
                if (item?.item?.QualifiedItemId == shakeOff && item.timeSinceDoneBouncing == 0f)
                {
                    ((StardewObject)item.item).Quality = ForageFantasy.DetermineForageQuality(Game1.player);
                }
            }
        }
    }
}