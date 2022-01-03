namespace ForageFantasy
{
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using StardewObject = StardewValley.Object;

    public static class ExtensionMethods
    {
        public static bool IsMushroomBox(this StardewObject o)
        {
            return o != null && o.bigCraftable.Value && o.ParentSheetIndex == 128;
        }

        public static bool IsTapper(this StardewObject o)
        {
            return o != null && o.bigCraftable.Value && (o.ParentSheetIndex == 105 || o.ParentSheetIndex == 264);
        }
    }

    // TODO give exp to owner if exists for auto pickup mods instead of host (host as fallback)
    internal class TapperAndMushroomQualityLogic
    {
        public static void IncreaseTreeAgesAndReplaceGrapes(ForageFantasy mod)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            foreach (var location in Game1.locations)
            {
                foreach (var terrainfeature in location.terrainFeatures.Pairs)
                {
                    if (terrainfeature.Value is Tree tree)
                    {
                        IncreaseTreeAge(mod, tree);
                    }
                    else if (terrainfeature.Value is HoeDirt dirt && dirt?.crop?.netSeedIndex?.Value == 301 && dirt?.crop?.indexOfHarvest?.Value == 398)
                    {
                        // TODO add new item, rename old item
                        // TODO replace with better item and move to different method
                        dirt.crop.indexOfHarvest.Value = 399;
                    }
                }
            }
        }

        public static void IncreaseTreeAge(ForageFantasy mod, Tree tree)
        {
            tree.modData.TryGetValue($"{mod.ModManifest.UniqueID}/treeAge", out string moddata);

            if (!string.IsNullOrEmpty(moddata))
            {
                int age = int.Parse(moddata);
                tree.modData[$"{mod.ModManifest.UniqueID}/treeAge"] = (age + 1).ToString();
            }
            else
            {
                tree.modData[$"{mod.ModManifest.UniqueID}/treeAge"] = 1.ToString();
            }
        }

        public static void RewardMushroomBoxExp(ForageFantasy mod)
        {
            if (mod.Config.MushroomXPAmount > 0)
            {
                Game1.player.gainExperience(2, mod.Config.MushroomXPAmount);
            }
        }

        public static void RewardTapperExp(ForageFantasy mod)
        {
            if (mod.Config.TapperXPAmount > 0)
            {
                Game1.player.gainExperience(2, mod.Config.TapperXPAmount);
            }
        }

        public static int DetermineTapperQuality(ForageFantasy mod, Farmer player, Tree tree)
        {
            int option = mod.Config.TapperQualityOptions;

            if (option == 1 || option == 2)
            {
                // has tapper profession or it's not required
                if (!mod.Config.TapperQualityRequiresTapperPerk || player.professions.Contains(Farmer.tapper))
                {
                    return ForageFantasy.DetermineForageQuality(player, mod.Config.TapperQualityOptions == 1);
                }
            }
            else if (option == 3 || option == 4)
            {
                // quality increase once a year
                return DetermineTreeQuality(mod, tree);
            }

            // tapper perk required but doesn't have it or invalid option
            return 0;
        }

        private static int DetermineTreeQuality(ForageFantasy mod, Tree tree)
        {
            tree.modData.TryGetValue($"{mod.ModManifest.UniqueID}/treeAge", out string moddata);

            if (!string.IsNullOrEmpty(moddata))
            {
                int age = int.Parse(moddata);

                bool useMonths = mod.Config.TapperQualityOptions == 3;

                int timeForLevelUp = useMonths ? 28 : 28 * 4;

                if (age < timeForLevelUp)
                {
                    return 0;
                }
                else if (age < timeForLevelUp * 2)
                {
                    return 1;
                }
                else if (age < timeForLevelUp * 3)
                {
                    return 2;
                }
                else
                {
                    return 4;
                }
            }

            return 0;
        }
    }
}