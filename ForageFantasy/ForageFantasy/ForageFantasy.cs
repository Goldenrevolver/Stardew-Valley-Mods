namespace ForageFantasy
{
    using Microsoft.Xna.Framework;
    using StardewModdingAPI;
    using StardewModdingAPI.Events;
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using System;
    using System.Collections.Generic;

    public class ForageFantasy : Mod, IAssetEditor
    {
        public readonly List<StardewValley.Object> MushroomsToWatch = new List<StardewValley.Object>();

        public readonly List<Tuple<StardewValley.Object, GameLocation>> TappersToWatch = new List<Tuple<StardewValley.Object, GameLocation>>();

        public readonly List<Vector2> ForageLocationsToWatch = new List<Vector2>();

        public readonly List<Bush> BushesReadyForHarvest = new List<Bush>();

        public bool CheckMushrooms { get; set; }

        public bool CheckTappers { get; set; }

        public ForageFantasyConfig Config { get; set; }

        public static int DetermineForageQuality(Farmer farmer, bool forceBotanist = false, bool allowBotanist = true)
        {
            if (allowBotanist && (forceBotanist || farmer.professions.Contains(16)))
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

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ForageFantasyConfig>();

            ForageFantasyConfig.VerifyConfigValues(Config, this);

            Helper.Events.GameLoop.DayStarted += delegate { OnDayStarted(); };

            Helper.Events.GameLoop.DayEnding += delegate { FernAndBurgerLogic.OnDayEnded(this); };

            Helper.Events.Input.ButtonPressed += WatchMushrooms;

            Helper.Events.Input.ButtonPressed += WatchTappers;

            Helper.Events.GameLoop.UpdateTicked += delegate { ResetWatchedObjects(); BerryBushLogic.FindBushShake(this); };

            Helper.Events.GameLoop.SaveLoaded += delegate
            {
                ResetVariables();
                FernAndBurgerLogic.ChangeBundle(this);
            };

            Helper.Events.GameLoop.GameLaunched += delegate { ForageFantasyConfig.SetUpModConfigMenu(Config, this); };
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return FernAndBurgerLogic.CanEdit<T>(asset, Config);
        }

        public void Edit<T>(IAssetData asset)
        {
            FernAndBurgerLogic.Edit<T>(asset, Config);
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
            MushroomsToWatch.Clear();
            TappersToWatch.Clear();
            BushesReadyForHarvest.Clear();

            foreach (var location in Game1.locations)
            {
                MushroomQualityLogic.OnDayStarted(this, location);

                FernAndBurgerLogic.OnDayStarted(this, location);

                TapperQualityLogic.OnDayStarted(this, location);

                BerryBushLogic.OnDayStarted(this, location);
            }

            if (Config.CompatibilityMode)
            {
                UseCompatibibilityMode();
            }
        }

        private void ResetVariables()
        {
            CheckMushrooms = false;
            CheckTappers = false;

            MushroomsToWatch.Clear();
            TappersToWatch.Clear();
            ForageLocationsToWatch.Clear();
            BushesReadyForHarvest.Clear();
        }

        private void UseCompatibibilityMode()
        {
            // will be if any player has the botanist perk, not necessarily the one with the highest foraging level
            bool botanistOverride = false;
            Farmer bestPlayer = Game1.player;

            foreach (var item in Game1.getOnlineFarmers())
            {
                if (item != null)
                {
                    // if botanistOverride is true or the player has botanist
                    botanistOverride |= item.professions.Contains(16);

                    if (item.ForagingLevel > bestPlayer.ForagingLevel)
                    {
                        bestPlayer = item;
                    }
                }
            }

            MushroomQualityLogic.CalculateMushroomQuality(this, bestPlayer, botanistOverride);
            TapperQualityLogic.CalculateTapperQuality(this, bestPlayer, botanistOverride);
        }

        private void ResetWatchedObjects()
        {
            if (Config.CompatibilityMode)
            {
                return;
            }

            if (CheckMushrooms && Config.MushroomCaveQuality)
            {
                foreach (var o in MushroomsToWatch)
                {
                    if (o != null && o.heldObject != null && o.heldObject.Value != null)
                    {
                        o.heldObject.Value.quality.Value = 0;
                    }
                }
            }

            if (CheckTappers && Config.TapperQualityOptions > 0)
            {
                foreach (var t in TappersToWatch)
                {
                    var o = t.Item1;
                    if (o != null && o.heldObject != null && o.heldObject.Value != null)
                    {
                        o.heldObject.Value.quality.Value = 0;
                    }
                }
            }

            CheckMushrooms = false;
            CheckTappers = false;
        }

        private void WatchMushrooms(object sender, ButtonPressedEventArgs args)
        {
            // removed this to ensure compatibility with Craftable Mushroom Boxes // || !(Game1.currentLocation is FarmCave))
            if (!Config.MushroomCaveQuality || Config.CompatibilityMode || !Context.IsWorldReady)
            {
                return;
            }

            if (args.Button.IsUseToolButton() || args.Button.IsActionButton())
            {
                MushroomQualityLogic.CalculateMushroomQuality(this, Game1.player);
            }
        }

        private void WatchTappers(object sender, ButtonPressedEventArgs args)
        {
            if (Config.TapperQualityOptions <= 0 || Config.CompatibilityMode || !Context.IsWorldReady)
            {
                return;
            }

            if (args.Button.IsUseToolButton() || args.Button.IsActionButton())
            {
                TapperQualityLogic.CalculateTapperQuality(this, Game1.player);
            }
        }
    }
}