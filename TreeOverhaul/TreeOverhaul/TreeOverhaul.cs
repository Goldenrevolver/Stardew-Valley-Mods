using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

//tree types (default names)
//
//bushyTree = 1;
//leafyTree = 2;
//pineTree = 3;
//winterTree1 = 4;
//winterTree2 = 5;
//palmTree = 6;
//mushroomTree = 7;

namespace TreeOverhaul
{
    public class TreeOverhaul : Mod
    {
        public TreeOverhaulConfig treeOverhaulConfig;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            treeOverhaulConfig = helper.ReadConfig<TreeOverhaulConfig>();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            foreach (var location in Game1.locations)
            {
                foreach (var terrainfeature in location.terrainFeatures.Pairs)
                {
                    switch (terrainfeature.Value)
                    {
                        case Tree tree:
                            CheckTree(tree, location, terrainfeature.Key);
                            RevertSaplingGrowthInShade(tree, location, terrainfeature.Key);
                            break;

                        case FruitTree fruittree:
                            CheckFruitTree(fruittree, location, terrainfeature.Key);
                            break;
                    }
                }
            }
        }

        public void RevertSaplingGrowthInShade(Tree tree, GameLocation environment, Vector2 tileLocation)
        {
            if (tree.growthStage.Value != 1 || !treeOverhaulConfig.StopShadeSaplingGrowth)
                return;

            Rectangle growthRect = new Rectangle((int)((tileLocation.X - 1f) * 64f), (int)((tileLocation.Y - 1f) * 64f), 192, 192);

            using (NetDictionary<Vector2, TerrainFeature, NetRef<TerrainFeature>, SerializableDictionary<Vector2, TerrainFeature>, NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>>>.PairsCollection.Enumerator enumerator = environment.terrainFeatures.Pairs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    KeyValuePair<Vector2, TerrainFeature> t = enumerator.Current;
                    if (t.Value is Tree && !t.Value.Equals(this) && ((Tree)t.Value).growthStage >= 5 && t.Value.getBoundingBox(t.Key).Intersects(growthRect))
                    {
                        tree.growthStage.Set(0);
                        return;
                    }
                }
            }
        }

        public void CheckTree(Tree tree, GameLocation environment, Vector2 tileLocation)
        {
            if (Game1.IsWinter && tree.treeType.Value != 6 && !environment.IsGreenhouse && !tree.fertilized.Value)
            {
                if (tree.treeType.Value != 7 && treeOverhaulConfig.NormalTreesGrowInWinter)
                {
                    GrowTree(tree, environment, tileLocation);
                }
                if (tree.treeType.Value == 7 && treeOverhaulConfig.MushroomTreesGrowInWinter)
                {
                    FixMushroomStump(tree, environment, tileLocation);
                    GrowTree(tree, environment, tileLocation);
                }
            }

            if (!treeOverhaulConfig.FasterNormalTreeGrowth)
                return;

            if (Game1.IsWinter)
            {
                if (tree.treeType.Value == 6)
                    return;

                if (tree.treeType.Value != 7 && (treeOverhaulConfig.NormalTreesGrowInWinter || environment.IsGreenhouse))
                {
                    GrowTree(tree, environment, tileLocation);
                }
                if (tree.treeType.Value == 7 && (treeOverhaulConfig.MushroomTreesGrowInWinter || environment.IsGreenhouse))
                {
                    FixMushroomStump(tree, environment, tileLocation);
                    GrowTree(tree, environment, tileLocation);
                }
            }
            else
            {
                if (tree.treeType.Value == 6)
                    return;

                if (tree.treeType.Value == 7)
                {
                    FixMushroomStump(tree, environment, tileLocation);
                }
                GrowTree(tree, environment, tileLocation);
            }
        }

        public void GrowTree(Tree tree, GameLocation environment, Vector2 tileLocation)
        {
            Rectangle growthRect = new Rectangle((int)((tileLocation.X - 1f) * 64f), (int)((tileLocation.Y - 1f) * 64f), 192, 192);

            string s = environment.doesTileHaveProperty((int)tileLocation.X, (int)tileLocation.Y, "NoSpawn", "Back");
            if (s != null && (s.Equals("All") || s.Equals("Tree") || s.Equals("True")))
            {
                return;
            }
            if (tree.growthStage == 4)
            {
                using (NetDictionary<Vector2, TerrainFeature, NetRef<TerrainFeature>, SerializableDictionary<Vector2, TerrainFeature>, NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>>>.PairsCollection.Enumerator enumerator = environment.terrainFeatures.Pairs.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        KeyValuePair<Vector2, TerrainFeature> t = enumerator.Current;
                        if (t.Value is Tree && !t.Value.Equals(this) && ((Tree)t.Value).growthStage >= 5 && t.Value.getBoundingBox(t.Key).Intersects(growthRect))
                        {
                            return;
                        }
                    }
                    goto IL_1D9;
                }
            }
            if (tree.growthStage == 0 && environment.objects.ContainsKey(tileLocation))
            {
                return;
            }
        IL_1D9:
            if (Game1.random.NextDouble() < 0.2 || tree.fertilized.Value)
            {
                tree.growthStage.Set(tree.growthStage.Value + 1);
            }
        }

        public void FixMushroomStump(Tree tree, GameLocation location, Vector2 tileLocation)
        {
            if (Game1.IsWinter)
            {
                if (tree.stump.Value)
                {
                    tree.stump.Set(false);
                    tree.health.Set(10f);
                }
            }
        }

        public void CheckFruitTree(FruitTree fruittree, GameLocation location, Vector2 tileLocation)
        {
            if (treeOverhaulConfig.FruitTreesDontGrowInWinter)
            {
                if (Game1.IsWinter)
                {
                    if (location.IsGreenhouse)
                    {
                        CheckGrowthType(fruittree, location, tileLocation);
                    }
                    else
                    {
                        GrowFruitTree(fruittree, location, tileLocation, "plus");
                    }
                }
                else
                {
                    CheckGrowthType(fruittree, location, tileLocation);
                }
            }
            else
            {
                CheckGrowthType(fruittree, location, tileLocation);
            }
        }

        public void CheckGrowthType(FruitTree fruittree, GameLocation location, Vector2 tileLocation)
        {
            if (treeOverhaulConfig.FruitTreeGrowth == 1)
            {
                GrowFruitTree(fruittree, location, tileLocation, "minus");
            }

            if (treeOverhaulConfig.FruitTreeGrowth == 2)
            {
                if (Game1.dayOfMonth % 2 == 1) //odd day
                {
                    GrowFruitTree(fruittree, location, tileLocation, "plus");
                }
            }
        }

        public void UpdateGrowthStage(FruitTree tree)
        {
            if (tree.daysUntilMature.Value > 28)
            {
                tree.daysUntilMature.Set(28);
            }
            if (tree.daysUntilMature.Value <= 0)
            {
                tree.growthStage.Set(4);
            }
            else if (tree.daysUntilMature.Value <= 7)
            {
                tree.growthStage.Set(3);
            }
            else if (tree.daysUntilMature.Value <= 14)
            {
                tree.growthStage.Set(2);
            }
            else if (tree.daysUntilMature.Value <= 21)
            {
                tree.growthStage.Set(1);
            }
            else
            {
                tree.growthStage.Set(0);
            }
        }

        public void GrowFruitTree(FruitTree tree, GameLocation environment, Vector2 tileLocation, string change)
        {
            bool foundSomething = false;
            foreach (Vector2 v in Utility.getSurroundingTileLocationsArray(tileLocation))
            {
                bool isClearHoeDirt = environment.terrainFeatures.ContainsKey(v) && environment.terrainFeatures[v] is HoeDirt && (environment.terrainFeatures[v] as HoeDirt).crop == null;
                if (environment.isTileOccupied(v, "", true) && !isClearHoeDirt)
                {
                    Object o = environment.getObjectAt((int)v.X, (int)v.Y);
                    //TODO this might be the wrong way around in the real 1.4 code
                    if (o == null || o.isPassable())
                    {
                        foundSomething = true;
                        break;
                    }
                }
            }
            if (!foundSomething)
            {
                if (change == "minus")
                {
                    tree.daysUntilMature.Set(tree.daysUntilMature.Value - 1);
                }
                if (change == "plus")
                {
                    tree.daysUntilMature.Set(tree.daysUntilMature.Value + 1);
                }
                UpdateGrowthStage(tree);
            }
        }
    }

    public class TreeOverhaulConfig
    {
        public bool StopShadeSaplingGrowth { get; set; } = true;
        public bool NormalTreesGrowInWinter { get; set; } = true;
        public bool MushroomTreesGrowInWinter { get; set; } = false;
        public bool FruitTreesDontGrowInWinter { get; set; } = false;
        public bool FasterNormalTreeGrowth { get; set; } = false;
        public int FruitTreeGrowth { get; set; } = 0;
    }
}