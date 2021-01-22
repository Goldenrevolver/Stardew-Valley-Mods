namespace ForageFantasy
{
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using System;

    internal class TapperQualityLogic
    {
        public static void OnDayStarted(ForageFantasy mod, GameLocation location)
        {
            if (Context.IsMainPlayer)
            {
                IncreaseTreeAge(mod, location);
            }

            if (mod.Config.TapperQualityOptions > 0)
            {
                foreach (StardewValley.Object o in location.objects.Values)
                {
                    if (o != null && o.bigCraftable && (o.ParentSheetIndex == 105 || o.parentSheetIndex == 264))
                    {
                        mod.TappersToWatch.Add(new Tuple<StardewValley.Object, GameLocation>(o, location));
                    }
                }
            }
        }

        public static void CalculateTapperQuality(ForageFantasy mod, Farmer player, bool botanistOverride = false)
        {
            foreach (var t in mod.TappersToWatch)
            {
                var o = t.Item1;
                if (o != null && o.minutesUntilReady <= 0 && o.heldObject != null && o.heldObject.Value != null)
                {
                    mod.CheckTappers = true;

                    int option = mod.Config.TapperQualityOptions;

                    if (option == 1 || option == 2)
                    {
                        // has tapper profession or it's not required
                        if (!mod.Config.TapperQualityRequiresTapperPerk || player.professions.Contains(15))
                        {
                            bool forceBotanist = mod.Config.CompatibilityMode ? botanistOverride : false;
                            o.heldObject.Value.quality.Value = ForageFantasy.DetermineForageQuality(player, forceBotanist, mod.Config.TapperQualityOptions == 1);
                        }
                    }
                    else if (option == 3 || option == 4)
                    {
                        // quality increase once a year
                        o.heldObject.Value.quality.Value = DetermineTreeQuality(mod, t);
                    }
                }
            }
        }

        private static void IncreaseTreeAge(ForageFantasy mod, GameLocation location)
        {
            foreach (var terrainfeature in location.terrainFeatures.Pairs)
            {
                switch (terrainfeature.Value)
                {
                    case Tree tree:
                        string moddata;
                        tree.modData.TryGetValue($"{mod.ModManifest.UniqueID}/treeAge", out moddata);

                        if (!string.IsNullOrEmpty(moddata))
                        {
                            int age = int.Parse(moddata);
                            tree.modData[$"{mod.ModManifest.UniqueID}/treeAge"] = (age + 1).ToString();
                        }
                        else
                        {
                            tree.modData[$"{mod.ModManifest.UniqueID}/treeAge"] = 1.ToString();
                        }

                        break;
                }
            }
        }

        private static int DetermineTreeQuality(ForageFantasy mod, Tuple<StardewValley.Object, GameLocation> tuple)
        {
            if (tuple.Item2.terrainFeatures.ContainsKey(tuple.Item1.TileLocation))
            {
                var obj = tuple.Item2.terrainFeatures[tuple.Item1.TileLocation];

                if (obj != null && obj is Tree tree)
                {
                    string moddata;
                    tree.modData.TryGetValue($"{mod.ModManifest.UniqueID}/treeAge", out moddata);

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
                }
            }

            return 0;
        }
    }
}