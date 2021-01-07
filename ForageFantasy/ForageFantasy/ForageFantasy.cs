﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ForageFantasy
{
    public class ForageFantasy : Mod, IAssetEditor
    {
        private List<StardewValley.Object> mushroomsToWatch = new List<StardewValley.Object>();

        private List<Tuple<StardewValley.Object, GameLocation>> tappersToWatch = new List<Tuple<StardewValley.Object, GameLocation>>();

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

                if (config.CommonFiddleheadFern)
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
            if (!Context.IsMainPlayer || !config.CommonFiddleheadFern)
            {
                return;
            }

            Dictionary<string, string> BundleData = Game1.netWorldState.Value.BundleData;

            // Summer Foraging
            string key = "Crafts Room/14";

            string[] bundle = BundleData[key].Split('/');

            if (!bundle[2].Contains("259 1 0"))
            {
                bundle[2] += " 259 1 0";
            }

            BundleData[key] = string.Join("/", bundle);
        }

        /// <summary>
        /// Small helper method to log to the console because I keep forgetting the signature
        /// </summary>
        /// <param name="o">the object I want to log as a string</param>
        public void DebugLog(object o)
        {
            this.Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        private List<Vector2> forageLocationsToWatch = new List<Vector2>();

        private void OnDayEnded()
        {
            if (!Context.IsMainPlayer || !config.CommonFiddleheadFern)
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
            string season = Game1.currentSeason;

            location.objects.Remove(vec);
            location.objects.Add(vec, new StardewValley.Object(vec, GetWildSeedForage(season), 1)
            {
                IsSpawnedObject = true,
                CanBeGrabbed = true
            });
        }

        private int GetWildSeedForage(string season)
        {
            if (season == "spring")
            {
                return 16 + Game1.random.Next(4) * 2;
            }
            if (!(season == "summer"))
            {
                if (season == "fall")
                {
                    return 404 + Game1.random.Next(4) * 2;
                }
                if (!(season == "winter"))
                {
                    return 22;
                }
                return 412 + Game1.random.Next(4) * 2;
            }
            else
            {
                int ran = Game1.random.Next(4);

                if (ran == 0)
                {
                    return 259;
                }
                else if (ran == 1)
                {
                    return 396;
                }
                else if (ran == 2)
                {
                    return 402;
                }
                else
                {
                    return 398;
                }
            }
        }

        /// <summary>Get whether this instance can edit the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
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
                if (asset.AssetNameEquals("Data/CraftingRecipes"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Edit a matched asset.</summary>
        /// <param name="asset">A helper which encapsulates metadata about an asset and enables changes to it.</param>
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

                    if (location == "BusStop")
                    {
                        fields[1] = "396 .4 398 .4 402 .7";
                    }
                    else if (location == "Forest")
                    {
                        fields[1] = "396 .6 402 .9 259 0.9";
                    }
                    else if (location == "Town")
                    {
                        fields[1] = "402 .5 398 0.5";
                    }
                    else if (location == "Mountain")
                    {
                        fields[1] = "396 .5 398 .8 259 0.8";
                    }
                    else if (location == "Backwoods")
                    {
                        fields[1] = "396 .5 398 .5 259 .5";
                    }
                    else if (location == "Railroad")
                    {
                        fields[1] = "396 .4 259 .4 402 .7";
                    }
                    else if (location == "Woods")
                    {
                        fields[1] = "259 .4 420 .6";
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
    }
}