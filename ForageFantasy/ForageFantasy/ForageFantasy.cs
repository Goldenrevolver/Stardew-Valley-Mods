namespace ForageFantasy
{
    using Microsoft.Xna.Framework;
    using StardewModdingAPI;
    using StardewModdingAPI.Events;
    using StardewValley;
    using StardewValley.Locations;
    using StardewValley.TerrainFeatures;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ForageFantasy : Mod, IAssetEditor
    {
        private readonly List<StardewValley.Object> mushroomsToWatch = new List<StardewValley.Object>();

        private readonly List<Tuple<StardewValley.Object, GameLocation>> tappersToWatch = new List<Tuple<StardewValley.Object, GameLocation>>();

        private readonly List<Vector2> forageLocationsToWatch = new List<Vector2>();

        private bool checkMushrooms;

        private bool checkTappers;

        /// <summary>
        /// The current config file
        /// </summary>
        private ForageFantasyConfig config;

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<ForageFantasyConfig>();

            ForageFantasyConfig.VerifyConfigValues(config, this);

            Helper.Events.GameLoop.DayStarted += delegate { OnDayStarted(); };

            Helper.Events.GameLoop.DayEnding += delegate { OnDayEnded(); };

            Helper.Events.Input.ButtonPressed += WatchMushrooms;

            Helper.Events.Input.ButtonPressed += WatchTappers;

            Helper.Events.GameLoop.UpdateTicked += delegate { ResetWatchedObjects(); };

            Helper.Events.GameLoop.SaveLoaded += delegate { ChangeBundle(); };

            Helper.Events.GameLoop.GameLaunched += delegate { ForageFantasyConfig.SetUpModConfigMenu(config, this); };
        }

        /// <summary>
        /// Get whether this instance can edit the given asset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (config.CommonFiddleheadFern)
            {
                if (asset.AssetNameEquals("Data/Locations"))
                {
                    return true;
                }
            }

            if (config.ForageSurvivalBurger)
            {
                if (asset.AssetNameEquals("Data/CookingRecipes"))
                {
                    return true;
                }
                else if (asset.AssetNameEquals("Data/CraftingRecipes"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Edit a matched asset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        public void Edit<T>(IAssetData asset)
        {
            if (config.CommonFiddleheadFern && asset.AssetNameEquals("Data/Locations"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                var keys = data.Keys.ToList();

                for (int i = 0; i < keys.Count; i++)
                {
                    string location = keys[i];
                    string[] fields = data[location].Split('/');

                    switch (location)
                    {
                        case "BusStop":
                            fields[1] = "396 .6 398 .6 402 .6";
                            break;

                        case "Forest":
                            fields[1] = "396 .8 402 .8 259 0.8";
                            break;

                        case "Mountain":
                            fields[1] = "396 .7 398 .7 259 0.7";
                            break;

                        case "Backwoods":
                            fields[1] = "396 .7 398 .7 259 .7";
                            break;

                        case "Railroad":
                            fields[1] = "396 .6 259 .6 398 .6";
                            break;

                        case "Woods":
                            fields[1] = "259 .4 420 .6";
                            break;
                    }

                    data[location] = string.Join("/", fields);
                }
            }

            if (config.ForageSurvivalBurger)
            {
                if (asset.AssetNameEquals("Data/CookingRecipes"))
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                    data.Remove("Survival Burger");
                    data.Add("Survival Burger (Sp)", "216 1 16 1 20 1 22 1/70 1/241 2/s Foraging 2/Survival Burger (Sp)");
                    data.Add("Survival Burger (Su)", "216 1 398 1 396 1 259 1/70 1/241 2/s Foraging 2/Survival Burger (Su)");
                    data.Add("Survival Burger (Fa)", "216 1 404 1 406 1 408 1/70 1/241 2/s Foraging 2/Survival Burger (Fa)");
                    data.Add("Survival Burger (Wi)", "216 1 412 1 414 1 416 1/70 1/241 2/s Foraging 2/Survival Burger (Wi)");
                }

                if (asset.AssetNameEquals("Data/CraftingRecipes"))
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                    data.Add("Survival Burger (Sp)", "216 1 16 1 20 1 22 1/Field/241/false/s Foraging 2/Survival Burger (Sp)");
                    data.Add("Survival Burger (Su)", "216 1 398 1 396 1 259 1/Field/241/false/s Foraging 2/Survival Burger (Su)");
                    data.Add("Survival Burger (Fa)", "216 1 404 1 406 1 408 1/Field/241/false/s Foraging 2/Survival Burger (Fa)");
                    data.Add("Survival Burger (Wi)", "216 1 412 1 414 1 416 1/Field/241/false/s Foraging 2/Survival Burger (Wi)");
                }
            }
        }

        /// <summary>
        /// Small helper method to log to the console because I keep forgetting the signature
        /// </summary>
        /// <param name="o">the object I want to log as a string</param>
        public void DebugLog(object o)
        {
            this.Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        private void OnDayStarted()
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            foreach (var location in Game1.locations)
            {
                if (config.MushroomCaveQuality && location is FarmCave && Game1.player.caveChoice == 2 && Game1.getFarm().farmCaveReady)
                {
                    foreach (StardewValley.Object o in location.objects.Values)
                    {
                        if (o.bigCraftable && o.heldObject.Value != null && o.ParentSheetIndex == 128)
                        {
                            mushroomsToWatch.Add(o);
                        }
                    }
                }

                if (config.CommonFiddleheadFern && Game1.currentSeason == "summer")
                {
                    foreach (var vec in forageLocationsToWatch)
                    {
                        if (location.objects.ContainsKey(vec))
                        {
                            StardewValley.Object o = location.objects[vec];

                            if (o.IsSpawnedObject && o.CanBeGrabbed)
                            {
                                RerandomizeWildSeedForage(vec, location);
                            }
                        }
                    }
                }

                if (config.TapperQualityOptions > 0)
                {
                    foreach (StardewValley.Object o in location.objects.Values)
                    {
                        if (o.bigCraftable && o.heldObject.Value != null && (o.ParentSheetIndex == 105 || o.parentSheetIndex == 264))
                        {
                            tappersToWatch.Add(new Tuple<StardewValley.Object, GameLocation>(o, location));
                        }
                    }
                }

                foreach (var terrainfeature in location.terrainFeatures.Pairs)
                {
                    switch (terrainfeature.Value)
                    {
                        case Tree tree:
                            string moddata;
                            tree.modData.TryGetValue($"{this.ModManifest.UniqueID}/treeAge", out moddata);

                            if (!string.IsNullOrEmpty(moddata))
                            {
                                int age = int.Parse(moddata);
                                tree.modData[$"{this.ModManifest.UniqueID}/treeAge"] = (age + 1).ToString();
                            }
                            else
                            {
                                tree.modData[$"{this.ModManifest.UniqueID}/treeAge"] = 1.ToString();
                            }

                            break;
                    }
                }
            }
        }

        private void ChangeBundle()
        {
            if (!config.CommonFiddleheadFern)
            {
                return;
            }

            Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;

            // Summer Foraging
            string key = "Crafts Room/14";

            string[] bundle = bundleData[key].Split('/');

            if (!bundle[2].Contains("259 1 0"))
            {
                bundle[2] += " 259 1 0";
            }

            bundleData[key] = string.Join("/", bundle);
        }

        private void OnDayEnded()
        {
            if (!Context.IsMainPlayer || !config.CommonFiddleheadFern || Game1.currentSeason != "summer")
            {
                return;
            }

            forageLocationsToWatch.Clear();

            foreach (var location in Game1.locations)
            {
                foreach (var terrainfeature in location.terrainFeatures.Pairs)
                {
                    switch (terrainfeature.Value)
                    {
                        case HoeDirt hoeDirt:
                            if (hoeDirt.crop != null)
                            {
                                Crop crop = hoeDirt.crop;

                                if (crop.isWildSeedCrop())
                                {
                                    forageLocationsToWatch.Add(terrainfeature.Key);
                                }
                            }

                            break;
                    }
                }
            }
        }

        private void ResetWatchedObjects()
        {
            if (checkMushrooms && config.MushroomCaveQuality)
            {
                foreach (var o in mushroomsToWatch)
                {
                    if (o != null && o.heldObject != null && o.heldObject.Value != null)
                    {
                        o.heldObject.Value.quality.Value = 0;
                    }
                }
            }

            if (checkTappers && config.TapperQualityOptions > 0)
            {
                foreach (var t in tappersToWatch)
                {
                    var o = t.Item1;
                    if (o != null && o.heldObject != null && o.heldObject.Value != null)
                    {
                        o.heldObject.Value.quality.Value = 0;
                    }
                }
            }

            checkMushrooms = false;
            checkTappers = false;
        }

        private void WatchMushrooms(object sender, ButtonPressedEventArgs args)
        {
            if (!config.MushroomCaveQuality || !Context.IsWorldReady || !(Game1.currentLocation is FarmCave))
            {
                return;
            }

            if (args.Button.IsUseToolButton() || args.Button.IsActionButton())
            {
                foreach (var o in mushroomsToWatch)
                {
                    if (o != null && o.minutesUntilReady <= 0 && o.heldObject != null && o.heldObject.Value != null)
                    {
                        checkMushrooms = true;
                        o.heldObject.Value.quality.Value = DetermineForageQuality(Game1.player);
                    }
                }
            }
        }

        private void WatchTappers(object sender, ButtonPressedEventArgs args)
        {
            if (config.TapperQualityOptions <= 0 || !Context.IsWorldReady)
            {
                return;
            }

            if (args.Button.IsUseToolButton() || args.Button.IsActionButton())
            {
                foreach (var t in tappersToWatch)
                {
                    var o = t.Item1;
                    if (o != null && o.minutesUntilReady <= 0 && o.heldObject != null && o.heldObject.Value != null)
                    {
                        checkTappers = true;

                        int option = config.TapperQualityOptions;

                        if (option == 1 || option == 2)
                        {
                            // has tapper profession
                            if (!config.TapperQualityRequiresTapperPerk || Game1.player.professions.Contains(15))
                            {
                                o.heldObject.Value.quality.Value = DetermineForageQuality(Game1.player, config.TapperQualityOptions == 1);
                            }
                        }
                        else if (option == 3 || option == 4)
                        {
                            // quality increase once a year
                            o.heldObject.Value.quality.Value = DetermineTreeQuality(t);
                        }
                    }
                }
            }
        }

        private int DetermineForageQuality(Farmer farmer, bool allowBotanist = true)
        {
            if (allowBotanist && farmer.professions.Contains(16))
            {
                return 4;
            }
            else
            {
                if (Game1.random.NextDouble() < farmer.ForagingLevel / 30f)
                {
                    return 2;
                }
                else if (Game1.random.NextDouble() < farmer.ForagingLevel / 15f)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int DetermineTreeQuality(Tuple<StardewValley.Object, GameLocation> tuple)
        {
            if (tuple.Item2.terrainFeatures.ContainsKey(tuple.Item1.TileLocation))
            {
                var obj = tuple.Item2.terrainFeatures[tuple.Item1.TileLocation];

                if (obj != null && obj is Tree tree)
                {
                    string moddata;
                    tree.modData.TryGetValue($"{this.ModManifest.UniqueID}/treeAge", out moddata);

                    if (!string.IsNullOrEmpty(moddata))
                    {
                        int age = int.Parse(moddata);

                        bool useMonths = config.TapperQualityOptions == 3;

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

        private void RerandomizeWildSeedForage(Vector2 vec, GameLocation location)
        {
            location.objects.Remove(vec);
            location.objects.Add(vec, new StardewValley.Object(vec, GetWildSeedSummerForage(), 1) { IsSpawnedObject = true, CanBeGrabbed = true });
        }

        private int GetWildSeedSummerForage()
        {
            int ran = Game1.random.Next(4);

            switch (ran)
            {
                case 0:
                    return 259;

                case 1:
                    return 396;

                case 2:
                    return 398;

                default:
                    return 402;
            }
        }
    }
}