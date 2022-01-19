namespace RingOverhaul
{
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using System;
    using System.Collections.Generic;

    public class RingOverhaul : Mod, IAssetEditor
    {
        public Texture2D ExplorerRingTexture { get; set; }

        public Texture2D BerserkerRingTexture { get; set; }

        public Texture2D PaladinRingTexture { get; set; }

        internal RingConfig Config;

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<RingConfig>();

            Helper.Events.GameLoop.GameLaunched += delegate { RingConfig.SetUpModConfigMenu(Config, this); };

            ExplorerRingTexture = Helper.Content.Load<Texture2D>($"assets/explorer_ring.png");
            BerserkerRingTexture = Helper.Content.Load<Texture2D>($"assets/berserker_ring.png");
            PaladinRingTexture = Helper.Content.Load<Texture2D>($"assets/paladin_ring.png");

            Patcher.PatchAll(this);
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
            return asset.AssetNameEquals("Data/ObjectInformation") || asset.AssetNameEquals("Data/CraftingRecipes");
        }

        public void Edit<T>(IAssetData asset)
        {
            if (asset.AssetNameEquals("Data/ObjectInformation"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;

                // Iridium Band
                var entry = data[527];
                var fields = entry.Split('/');
                fields[^1] = Helper.Translation.Get("IridiumBandTooltip");
                data[527] = string.Join("/", fields);
            }

            if (asset.AssetNameEquals("Data/CraftingRecipes"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                var recipeChanges = new Dictionary<string, Tuple<string, string>>();

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
                    data.Add("Amethyst Ring", "66 1 334 1/Home/529/false/Combat 2");
                    data.Add("Topaz Ring", "68 1 334 1/Home/530/false/Combat 3");
                    data.Add("Aquamarine Ring", "62 1 334 1/Home/531/false/Combat 4");
                    data.Add("Jade Ring", "70 1 334 1/Home/532/false/Combat 6");
                    data.Add("Emerald Ring", "60 1 334 1/Home/533/false/Combat 7");
                    data.Add("Ruby Ring", "64 1 334 1/Home/534/false/Combat 8");
                }
            }
        }
    }
}