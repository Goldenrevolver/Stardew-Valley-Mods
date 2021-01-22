namespace ForageFantasy
{
    using StardewValley;

    internal class MushroomQualityLogic
    {
        public static void OnDayStarted(ForageFantasy mod, GameLocation location)
        {
            // removed this to ensure compatibility with Craftable Mushroom Boxes // && location is FarmCave && Game1.player.caveChoice == 2)
            if (mod.Config.MushroomCaveQuality)
            {
                foreach (Object o in location.objects.Values)
                {
                    if (o != null && o.bigCraftable && o.ParentSheetIndex == 128)
                    {
                        mod.MushroomsToWatch.Add(o);
                    }
                }
            }
        }

        public static void CalculateMushroomQuality(ForageFantasy mod, Farmer player, bool botanistOverride = false)
        {
            foreach (var o in mod.MushroomsToWatch)
            {
                if (o != null && o.minutesUntilReady <= 0 && o.heldObject != null && o.heldObject.Value != null)
                {
                    mod.CheckMushrooms = true;
                    o.heldObject.Value.quality.Value = ForageFantasy.DetermineForageQuality(player, botanistOverride);
                }
            }
        }
    }
}