﻿namespace ForageFantasy
{
    using StardewModdingAPI;
    using StardewModdingAPI.Events;
    using StardewValley;
    using StardewValley.TerrainFeatures;
    using System;
    using System.Linq;
    using StardewObject = StardewValley.Object;

    public class ForageFantasy : Mod
    {
        public ForageFantasyConfig Config { get; set; }

        public static IManifest Manifest { get; set; }

        internal static string FineGrapeAssetPath { get; private set; }

        internal static bool MushroomTreeTapperWorksInWinter { get; set; }

        // maybe TODO seasonal mushroom tappers, magma cap on ginger island for mushroom tree, log or box

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ForageFantasyConfig>();
            Manifest = this.ModManifest;

            ForageFantasyConfig.VerifyConfigValues(Config, this);

            FineGrapeAssetPath = Helper.ModContent.GetInternalAssetName("assets/fineGrape.png").BaseName;

            helper.Events.GameLoop.GameLaunched += delegate
            {
                ForageFantasyConfig.SetUpModConfigMenu(Config, this);
                DeluxeGrabberCompatibility.Setup(this);
            };

            helper.Events.GameLoop.DayStarted += delegate
            {
                TapperAndMushroomQualityLogic.IncreaseTreeAges(this);
                GrapeLogic.SetDropToNewGrapes(this);
                FernAndBurgerLogic.AllowSurvivalBurgerRecipeCompletion();
            };

            helper.Events.GameLoop.DayEnding += delegate { GrapeLogic.ResetGrapes(this); };

            helper.Events.GameLoop.SaveLoaded += delegate { FernAndBurgerLogic.UpdateExistingBundle(this); };

            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            helper.Events.Content.AssetRequested += OnAssetRequested;

            helper.Events.Content.AssetReady += OnAssetReady;

            Patcher.PatchAll(this);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            FernAndBurgerLogic.Apply(e, this.Config, this.Helper.Translation);

            GrapeLogic.Apply(e, this.Config, this.Helper.Translation);

            TapperAssetChanges.Apply(e, this.Config);
        }

        private void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/WildTrees"))
            {
                if (Tree.TryGetData(Tree.mushroomTree, out var mushroomTreeData))
                {
                    var defaultTap = mushroomTreeData.TapItems.Where((s) => s.Id == "Default").FirstOrDefault();

                    if (defaultTap?.Condition != null && defaultTap.Condition.Contains("!LOCATION_SEASON Target Winter"))
                    {
                        MushroomTreeTapperWorksInWinter = false;
                    }
                    else
                    {
                        MushroomTreeTapperWorksInWinter = !mushroomTreeData.IsStumpDuringWinter;
                    }
                }
            }
        }

        public static int DetermineForageQuality(Farmer farmer, Random r, bool allowBotanist = true)
        {
            if (allowBotanist && farmer.professions.Contains(Farmer.botanist))
            {
                return StardewObject.bestQuality;
            }
            else
            {
                if (r.NextDouble() < farmer.ForagingLevel / 30f)
                {
                    return StardewObject.highQuality;
                }
                else if (r.NextDouble() < farmer.ForagingLevel / 15f)
                {
                    return StardewObject.medQuality;
                }
                else
                {
                    return StardewObject.lowQuality;
                }
            }
        }

        /// <summary>
        /// Small helper method to log to the console because I keep forgetting the signature
        /// </summary>
        /// <param name="o">the object I want to log as a string</param>
        public void DebugLog(object o)
        {
            Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        /// <summary>
        /// Small helper method to log an error to the console because I keep forgetting the signature
        /// </summary>
        /// <param name="o">the object I want to log as a string</param>
        /// <param name="e">an optional error message to log additionally</param>
        public void ErrorLog(object o, Exception e = null)
        {
            string baseMessage = o == null ? "null" : o.ToString();

            string errorMessage = e == null ? string.Empty : $"\n{e.Message}\n{e.StackTrace}";

            Monitor.Log(baseMessage + errorMessage, LogLevel.Error);
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
            {
                return;
            }

            // this is done in buttonsChanged instead of buttonPressed as recommended
            // in the documentation: https://stardewcommunitywiki.com/Modding:Modder_Guide/APIs/Input#KeybindList
            if (Config.TreeMenuKey.JustPressed())
            {
                OpenTreeMenu(Game1.currentLocation);
            }
        }

        private void OpenTreeMenu(GameLocation currentLocation)
        {
            foreach (var terrainfeature in currentLocation.terrainFeatures.Pairs)
            {
                if (Game1.currentCursorTile != terrainfeature.Value.Tile)
                {
                    continue;
                }

                if (terrainfeature.Value is Tree tree)
                {
                    Game1.activeClickableMenu = new TreeMenu(this, tree);
                    return;
                }

                // fruit tree ages are negative
                if (terrainfeature.Value is FruitTree fruittree)
                {
                    Game1.activeClickableMenu = new TreeMenu(this, fruittree);
                    return;
                }
            }
        }
    }
}