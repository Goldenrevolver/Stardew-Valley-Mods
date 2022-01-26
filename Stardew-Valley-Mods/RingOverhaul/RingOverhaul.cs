namespace RingOverhaul
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.Objects;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RingOverhaul : Mod, IAssetEditor
    {
        public Texture2D ExplorerRingTexture { get; set; }

        public Texture2D BerserkerRingTexture { get; set; }

        public Texture2D PaladinRingTexture { get; set; }

        public const int CoalID = 382;
        public const int IridiumBandID = 527;
        public const int JukeBoxRingID = 528;

        internal RingConfig Config;

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<RingConfig>();

            Helper.Events.GameLoop.GameLaunched += delegate { RingConfig.SetUpModConfigMenu(Config, this); };
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;

            ExplorerRingTexture = Helper.Content.Load<Texture2D>("assets/explorer_ring.png");
            BerserkerRingTexture = Helper.Content.Load<Texture2D>("assets/berserker_ring.png");
            PaladinRingTexture = Helper.Content.Load<Texture2D>("assets/paladin_ring.png");

            Patcher.PatchAll(this);
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            if (Config.JukeboxRingEnabled)
            {
                if (Game1.player.craftingRecipes.ContainsKey("Mini-Jukebox"))
                {
                    Game1.player.craftingRecipes.Add("Jukebox Ring", 0);
                }

                Utility.iterateAllItems(delegate (Item item)
                {
                    if (item is Ring ring && ring.ParentSheetIndex == JukeBoxRingID)
                    {
                        // Mini-Jukebox
                        ring.description = new StardewValley.Object(Vector2.Zero, 209).getDescription();
                    }
                });
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

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Data/ObjectInformation")
                || asset.AssetNameEquals("Data/CraftingRecipes")
                || (asset.AssetNameEquals("Maps/springobjects") && Config.CraftableGemRings && Config.CraftableGemRingsCustomSprites && Config.CraftableGemRingsMetalBar != 1);
        }

        public void Edit<T>(IAssetData asset)
        {
            if (asset.AssetNameEquals("Data/ObjectInformation"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;

                var entry = data[IridiumBandID];
                var fields = entry.Split('/');
                fields[^1] = Helper.Translation.Get("IridiumBandTooltip");
                data[IridiumBandID] = string.Join("/", fields);
            }

            if (asset.AssetNameEquals("Maps/springobjects") && Config.CraftableGemRings && Config.CraftableGemRingsCustomSprites && Config.CraftableGemRingsMetalBar != 1)
            {
                var editor = asset.AsImage();

                string path = Config.CraftableGemRingsMetalBar switch
                {
                    2 => "assets/gem_rings_iron.png",
                    3 => "assets/gem_rings_gold.png",
                    _ => "assets/gem_rings_progressive.png",
                };

                Texture2D sourceImage = Helper.Content.Load<Texture2D>(path, ContentSource.ModFolder);
                editor.PatchImage(sourceImage, targetArea: new Rectangle(16, 352, 96, 16));
            }

            if (asset.AssetNameEquals("Data/CraftingRecipes"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                var recipeChanges = new Dictionary<string, Tuple<string, string>>();

                if (Config.JukeboxRingEnabled)
                {
                    data.Add("Jukebox Ring", "336 1 787 1 464 1/Home/528/false/null");
                }

                if (!Config.OldGlowStoneRingRecipe)
                {
                    recipeChanges["Iridium Band"] = new Tuple<string, string>("337 2 529 1 530 1 531 1 532 1 533 1 534 1", "Combat 9");
                }

                if (!Config.OldGlowStoneRingRecipe)
                {
                    recipeChanges["Glowstone Ring"] = new Tuple<string, string>("517 1 519 1", "Mining 4");
                }

                if (Config.MinorRingCraftingChanges)
                {
                    recipeChanges["Sturdy Ring"] = new Tuple<string, string>("334 2 86 5 338 5", "Combat 1");
                    recipeChanges["Warrior Ring"] = new Tuple<string, string>("335 5 382 25 84 10", "Combat 4");
                }

                foreach (var item in recipeChanges)
                {
                    var entry = data[item.Key];
                    var fields = entry.Split('/');
                    fields[0] = item.Value.Item1;
                    fields[^1] = item.Value.Item2;
                    data[item.Key] = string.Join("/", fields);
                }

                if (!Config.OldGlowStoneRingRecipe)
                {
                    data.Add("Glow Ring", "516 1 768 5/Home/517/false/Mining 4");
                    data.Add("Magnet Ring", "518 1 769 5/Home/519/false/Mining 4");
                }

                if (Config.CraftableGemRings)
                {
                    var dict = new Dictionary<string, int> { { "Amethyst Ring", 66 }, { "Topaz Ring", 68 }, { "Aquamarine Ring", 62 }, { "Jade Ring", 70 }, { "Emerald Ring", 60 }, { "Ruby Ring", 64 } }.ToList();

                    int itemId = 529;
                    int combatLevel = 2;
                    int oreBar = Config.CraftableGemRingsMetalBar == 2 ? 335 : Config.CraftableGemRingsMetalBar == 3 ? 336 : 334;

                    for (int i = 0; i < dict.Count; i++)
                    {
                        data.Add(dict[i].Key, $"{dict[i].Value} 1 {oreBar} 1/Home/{itemId}/ false/Combat {combatLevel}");

                        if (Config.CraftableGemRingsMetalBar == 0 && i is 1 or 3)
                        {
                            oreBar++;
                        }

                        if (Config.CraftableGemRingsUnlockLevels != 0)
                        {
                            combatLevel += i == 2 ? 2 : 1;
                        }
                        else if (i is 1 or 3)
                        {
                            combatLevel += 2;
                        }

                        itemId++;
                    }
                }
            }
        }
    }
}