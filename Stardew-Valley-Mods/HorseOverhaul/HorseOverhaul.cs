namespace HorseOverhaul
{
    using Harmony;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewModdingAPI;
    using StardewModdingAPI.Events;
    using StardewModdingAPI.Utilities;
    using StardewValley;
    using StardewValley.Buildings;
    using StardewValley.Characters;
    using StardewValley.Objects;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public enum SeasonalVersion
    {
        None,
        Sonr,
        Gwen
    }

    public class HorseOverhaul : Mod, IAssetEditor
    {
        //// how stable cords look

        //// (tile.x, tile.y), (tile.x+1, tile.y), (tile.x+2, tile.y), (tile.x+3, tile.y)
        //// (tile.x, tile.y+1), (tile.x+1, tile.y+1), (tile.x+2, tile.y+1), (tile.x+3, tile.y+1)

        public readonly List<HorseWrapper> Horses = new List<HorseWrapper>();

        private string gwenOption = "1";

        private bool usingMyTextures = false;

        private bool dayJustStarted = false;

        private SeasonalVersion seasonalVersion = SeasonalVersion.None;

        public Texture2D HorseSpriteWithHead { get; set; }

        public HorseConfig Config { get; set; }

        public Lazy<Texture2D> CurrentStableTexture => new Lazy<Texture2D>(() => usingMyTextures ? Helper.Content.Load<Texture2D>("assets/stable.png") : Helper.Content.Load<Texture2D>("Buildings/Stable", ContentSource.GameContent));

        public Lazy<Texture2D> FilledTroughTexture => FilledTroughOverlay == null ? CurrentStableTexture : MergeTextures(FilledTroughOverlay, CurrentStableTexture);

        public Lazy<Texture2D> EmptyTroughTexture => EmptyTroughOverlay == null ? CurrentStableTexture : MergeTextures(EmptyTroughOverlay, CurrentStableTexture);

        private Lazy<Texture2D> FilledTroughOverlay { get; set; }

        private Lazy<Texture2D> EmptyTroughOverlay { get; set; }

        //// TODO add food preferences
        //// TODO fix menu for zoom

        public static bool IsTractor(Horse horse)
        {
            return horse?.modData.TryGetValue("Pathoschild.TractorMod", out _) == true || horse?.Name.StartsWith("tractor/") == true;
        }

        public static bool IsGarage(Stable stable)
        {
            return stable != null && (stable.maxOccupants.Value == -794739 || stable.buildingType.Value == "TractorGarage");
        }

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<HorseConfig>();

            HorseConfig.VerifyConfigValues(Config, this);

            Helper.Events.GameLoop.GameLaunched += delegate { HorseConfig.SetUpModConfigMenu(Config, this); };

            Helper.Events.GameLoop.SaveLoaded += delegate { SetStableOverlays(); };

            Helper.Events.GameLoop.Saving += delegate { SaveChest(); };
            Helper.Events.GameLoop.DayStarted += delegate { OnDayStarted(); };
            helper.Events.GameLoop.UpdateTicked += delegate { LateDayStarted(); };

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            Patcher.PatchAll(this);
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Animals/horse") && Config.ThinHorse;
        }

        public void Edit<T>(IAssetData asset)
        {
            if (Config.ThinHorse)
            {
                var editor = asset.AsImage();

                HorseSpriteWithHead = new Texture2D(editor.Data.GraphicsDevice, editor.Data.Width, editor.Data.Height);

                int count = editor.Data.Width * editor.Data.Height;
                var data = new Color[count];
                editor.Data.GetData(data);
                HorseSpriteWithHead.SetData(data);

                Texture2D sourceImage = Helper.Content.Load<Texture2D>("assets/empty.png", ContentSource.ModFolder);
                editor.PatchImage(sourceImage, targetArea: new Microsoft.Xna.Framework.Rectangle?(new Rectangle(160, 96, 9, 15)));
            }
        }

        public void DebugLog(object o)
        {
            Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        public void ErrorLog(object o, Exception e = null)
        {
            string baseMessage = o == null ? "null" : o.ToString();

            string errorMessage = e == null ? string.Empty : $"\n{e.Message}\n{e.StackTrace}";

            Monitor.Log(baseMessage + errorMessage, LogLevel.Error);
        }

        private void SetStableOverlays()
        {
            if (!Config.Water)
            {
                return;
            }

            seasonalVersion = SeasonalVersion.None;
            usingMyTextures = false;
            FilledTroughOverlay = null;

            if (Helper.ModRegistry.IsLoaded("sonreirblah.JBuildings"))
            {
                // seasonal overlays are assigned in LateDayStarted
                EmptyTroughOverlay = null;

                seasonalVersion = SeasonalVersion.Sonr;
                return;
            }

            if (Helper.ModRegistry.IsLoaded("Oklinq.CleanStable"))
            {
                EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/overlay_empty.png", ContentSource.ModFolder));

                return;
            }

            if (Helper.ModRegistry.IsLoaded("Elle.SeasonalBuildings"))
            {
                var data = Helper.ModRegistry.Get("Elle.SeasonalBuildings");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var list = ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "color palette", "stable" }, data.Manifest.Name);

                    if (list["stable"] != "false")
                    {
                        EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/elle/overlay_empty_{list["color palette"]}.png", ContentSource.ModFolder));

                        return;
                    }
                }
            }

            if (Helper.ModRegistry.IsLoaded("Elle.SeasonalVanillaBuildings"))
            {
                var data = Helper.ModRegistry.Get("Elle.SeasonalVanillaBuildings");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var list = ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "stable" }, data.Manifest.Name);

                    if (list["stable"] == "true")
                    {
                        FilledTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/overlay_filled_tone.png", ContentSource.ModFolder));
                        EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/overlay_empty_tone.png", ContentSource.ModFolder));

                        return;
                    }
                }
            }

            if (Helper.ModRegistry.IsLoaded("Gweniaczek.Medieval_stables"))
            {
                IModInfo data = Helper.ModRegistry.Get("Gweniaczek.Medieval_stables");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var dict = ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "stableOption" }, data.Manifest.Name);

                    SetupGwenTextures(dict);

                    return;
                }
            }

            if (Helper.ModRegistry.IsLoaded("Gweniaczek.Medieval_buildings"))
            {
                var data = Helper.ModRegistry.Get("Gweniaczek.Medieval_buildings");

                var path = data.GetType().GetProperty("DirectoryPath");

                if (path != null && path.GetValue(data) != null)
                {
                    var dict = ReadConfigFile("config.json", path.GetValue(data) as string, new[] { "buildingsReplaced", "stableOption" }, data.Manifest.Name);

                    if (dict["buildingsReplaced"].Contains("stable"))
                    {
                        SetupGwenTextures(dict);

                        return;
                    }
                }
            }

            if (Helper.ModRegistry.IsLoaded("magimatica.SeasonalVanillaBuildings"))
            {
                EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/overlay_empty_no_bucket.png", ContentSource.ModFolder));

                return;
            }

            // no compatible texture mod found so we will use mine
            usingMyTextures = true;

            EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/overlay_empty.png", ContentSource.ModFolder));
        }

        private void SetupGwenTextures(Dictionary<string, string> dict)
        {
            if (dict["stableOption"] == "4")
            {
                FilledTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/gwen/overlay_{dict["stableOption"]}_full.png", ContentSource.ModFolder));
            }

            EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/gwen/overlay_{dict["stableOption"]}.png", ContentSource.ModFolder));

            seasonalVersion = SeasonalVersion.Gwen;
            gwenOption = dict["stableOption"];
        }

        private Dictionary<string, string> ReadConfigFile(string path, string modFolderPath, string[] options, string modName)
        {
            string fullPath = Path.Combine(modFolderPath, PathUtilities.NormalizePath(path));

            var result = new Dictionary<string, string>();

            try
            {
                string fullText = File.ReadAllText(fullPath).ToLower();
                var split = fullText.Split('\"');

                for (int i = 0; i < split.Length; i++)
                {
                    foreach (var option in options)
                    {
                        if (option.ToLower() == split[i].Trim() && i + 2 < split.Length)
                        {
                            string optionText = split[i + 2].Trim();

                            result.Add(option, optionText);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog($"There was an exception while {ModManifest.Name} was reading the config for {modName}:", e);
            }

            return result;
        }

        private Lazy<Texture2D> MergeTextures(Lazy<Texture2D> overlay, Lazy<Texture2D> oldTexture)
        {
            int count = overlay.Value.Width * overlay.Value.Height;
            var newData = new Color[count];
            overlay.Value.GetData(newData);
            var origData = new Color[count];
            oldTexture.Value.GetData(origData);

            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = newData[i].A != 0 ? newData[i] : origData[i];
            }

            oldTexture.Value.SetData(newData);
            return oldTexture;
        }

        private void LateDayStarted()
        {
            if (!Context.IsWorldReady || !dayJustStarted || !Config.Water || Config.DisableStableSpriteChanges)
            {
                return;
            }

            dayJustStarted = false;

            if (seasonalVersion == SeasonalVersion.Sonr)
            {
                EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/sonr/overlay_empty_{Game1.currentSeason}.png", ContentSource.ModFolder));
            }
            else if (seasonalVersion == SeasonalVersion.Gwen)
            {
                if (Game1.currentSeason == "winter" && Game1.isSnowing)
                {
                    EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/gwen/overlay_1_snow_peta.png", ContentSource.ModFolder));
                }
                else
                {
                    EmptyTroughOverlay = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>($"assets/gwen/overlay_{gwenOption}.png", ContentSource.ModFolder));
                }
            }

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building is Stable stable && !IsGarage(stable))
                {
                    stable.texture = EmptyTroughTexture;
                }
            }
        }

        private void OnDayStarted()
        {
            for (int i = 0; i < Horses.Count; i++)
            {
                Horses[i] = null;
            }

            Horses.Clear();

            // for LateDayStarted, so we can change the textures after content patcher did
            dayJustStarted = true;

            if (Game1.player.hasPet())
            {
                Pet pet = Game1.player.getPet();

                if (pet?.modData?.TryGetValue($"{ModManifest.UniqueID}/gotFed", out _) == true)
                {
                    pet.modData.Remove($"{ModManifest.UniqueID}/gotFed");
                }
            }

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building is Stable stable && !IsGarage(stable))
                {
                    Chest saddleBag = null;

                    stable.modData.TryGetValue($"{ModManifest.UniqueID}/stableID", out string modData);

                    int stableID;
                    if (string.IsNullOrEmpty(modData))
                    {
                        if (Config.SaddleBag)
                        {
                            // find position for the new position (will get overridden at the end of the day)
                            stableID = -1;
                            int i = 0;

                            while (i < 10)
                            {
                                if (!Game1.getFarm().Objects.ContainsKey(new Vector2(i, 0)))
                                {
                                    stableID = i;
                                    break;
                                }

                                i++;
                            }

                            if (stableID == -1)
                            {
                                ErrorLog("Couldn't find a spot to place the saddle bag chest");
                                return;
                            }

                            saddleBag = new Chest(true, new Vector2(stableID, 0));
                        }
                    }
                    else
                    {
                        stableID = int.Parse(modData);

                        StardewValley.Object value;
                        Game1.getFarm().Objects.TryGetValue(new Vector2(stableID, 0), out value);

                        if (value != null && value is Chest chest)
                        {
                            Game1.getFarm().Objects.Remove(new Vector2(stableID, 0));

                            if (Config.SaddleBag)
                            {
                                saddleBag = chest;
                            }
                            else
                            {
                                if (chest.items.Count > 0)
                                {
                                    foreach (var item in chest.items)
                                    {
                                        Game1.player.team.returnedDonations.Add(item);
                                        Game1.player.team.newLostAndFoundItems.Value = true;
                                    }

                                    chest.items.Clear();
                                }
                            }
                        }
                        else
                        {
                            ErrorLog("Stable says there is a saddle bag chest, but I couldn't find it!");
                            saddleBag = new Chest(true, new Vector2(stableID, 0));
                        }
                    }

                    if (Config.ThinHorse)
                    {
                        stable.getStableHorse().forceOneTileWide.Value = true;
                    }

                    Horses.Add(new HorseWrapper(stable.getStableHorse(), this, saddleBag));
                }
            }
        }

        private void SaveChest()
        {
            if (!Config.SaddleBag)
            {
                return;
            }

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building is Stable stable && !IsGarage(stable))
                {
                    stable.getStableHorse().forceOneTileWide.Value = false;

                    HorseWrapper horse = null;

                    Horses.Where(x => x.Horse == stable.getStableHorse()).Do(x => horse = x);

                    if (horse != null && horse.SaddleBag != null)
                    {
                        // find the first free position
                        int stableID = -1;
                        int i = 0;

                        while (i < 10)
                        {
                            if (!Game1.getFarm().Objects.ContainsKey(new Vector2(i, 0)))
                            {
                                stableID = i;
                                break;
                            }

                            i++;
                        }

                        if (stableID == -1)
                        {
                            ErrorLog("Couldn't find a spot to save the saddle bag chest");

                            if (horse.SaddleBag.items.Count > 0)
                            {
                                foreach (var item in horse.SaddleBag.items)
                                {
                                    Game1.player.team.returnedDonations.Add(item);
                                    Game1.player.team.newLostAndFoundItems.Value = true;
                                }

                                horse.SaddleBag.items.Clear();
                            }

                            horse.SaddleBag = null;
                            return;
                        }

                        if (stable.modData.ContainsKey($"{ModManifest.UniqueID}/stableID"))
                        {
                            stable.modData[$"{ModManifest.UniqueID}/stableID"] = stableID.ToString();
                        }
                        else
                        {
                            stable.modData.Add($"{ModManifest.UniqueID}/stableID", stableID.ToString());
                        }

                        if (horse.SaddleBag != null)
                        {
                            horse.SaddleBag.TileLocation = new Vector2(stableID, 0);
                        }

                        Game1.getFarm().Objects.Add(new Vector2(stableID, 0), horse.SaddleBag);
                    }
                }
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
            {
                return;
            }

            if (e.Button.IsUseToolButton())
            {
                bool wasController = e.Button.TryGetController(out _);
                Point cursorPosition = Game1.getMousePosition();

                CheckHorseInteraction(Game1.currentLocation, cursorPosition.X + Game1.viewport.X, cursorPosition.Y + Game1.viewport.Y, wasController);
            }
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
            {
                return;
            }

            // this is done in buttonsChanged instead of buttonPressed as recommend
            // in the documentation: https://stardewcommunitywiki.com/Modding:Modder_Guide/APIs/Input#KeybindList
            if (Config.HorseMenuKey.JustPressed())
            {
                OpenHorseMenu();
                return;
            }

            if (Config.PetMenuKey.JustPressed())
            {
                OpenPetMenu();
                return;
            }
        }

        private void CheckHorseInteraction(GameLocation currentLocation, int x, int y, bool wasController)
        {
            // Find if click was on Horse
            foreach (Horse horse in currentLocation.characters.OfType<Horse>())
            {
                // Can only feed your own horse
                if (horse.getOwner() != Game1.player || IsTractor(horse))
                {
                    continue;
                }

                HorseWrapper horseW = null;

                Horses.Where(h => h.Horse == horse).Do(h => horseW = h);

                if (horseW == null)
                {
                    continue;
                }

                if (IsInRange(horse, x, y, wasController))
                {
                    if (Game1.player.CurrentItem != null && Config.Feeding)
                    {
                        // Holding food
                        Item currentItem = Game1.player.CurrentItem;
                        if (IsEdible(currentItem))
                        {
                            Item food = Game1.player.CurrentItem;

                            if (horseW.GotFed)
                            {
                                Game1.drawObjectDialogue(Helper.Translation.Get("AteEnough", new { horseName = horse.displayName }));
                            }
                            else
                            {
                                Game1.drawObjectDialogue(Helper.Translation.Get("AteFood", new { horseName = horse.displayName, foodName = food.DisplayName }));

                                if (Config.ThinHorse)
                                {
                                    horse.doEmote(Character.happyEmote);
                                }

                                Game1.player.reduceActiveItemByOne();

                                horseW.JustGotFood(CalculateExpGain(currentItem, horseW.Friendship));
                            }

                            return;
                        }
                    }

                    if (Context.IsWorldReady && Context.CanPlayerMove && Context.IsPlayerFree && Config.SaddleBag)
                    {
                        if (horseW.SaddleBag != null)
                        {
                            horseW.SaddleBag.ShowMenu();
                            return;
                        }
                    }
                }
            }

            if (Config.PetFeeding && Game1.player.hasPet())
            {
                Pet pet = Game1.player.getPet();

                if (pet != null)
                {
                    if (IsInRange(pet, x, y, wasController))
                    {
                        if (Game1.player.CurrentItem != null)
                        {
                            // Holding food
                            Item currentItem = Game1.player.CurrentItem;
                            if (IsEdible(currentItem))
                            {
                                Item food = Game1.player.CurrentItem;

                                if (pet?.modData?.TryGetValue($"{ModManifest.UniqueID}/gotFed", out _) == true)
                                {
                                    Game1.drawObjectDialogue(Helper.Translation.Get("AteEnough", new { name = pet.displayName }));
                                }
                                else
                                {
                                    pet.modData.Add($"{ModManifest.UniqueID}/gotFed", "fed");

                                    Game1.drawObjectDialogue(Helper.Translation.Get("AteFood", new { name = pet.displayName, foodName = food.DisplayName }));

                                    pet.doEmote(Character.happyEmote);

                                    Game1.player.reduceActiveItemByOne();

                                    pet.friendshipTowardFarmer.Set(Math.Min(1000, pet.friendshipTowardFarmer.Value + CalculateExpGain(currentItem, pet.friendshipTowardFarmer.Value)));
                                }

                                return;
                            }
                        }
                    }
                }
            }
        }

        private bool IsInRange(Character chara, int x, int y, bool wasController)
        {
            return Utility.withinRadiusOfPlayer((int)chara.Position.X, (int)chara.Position.Y, 1, Game1.player) && (Utility.distance(x, chara.Position.X, y, chara.Position.Y) <= 110 || wasController);
        }

        private void OpenHorseMenu(int? x = null, int? y = null)
        {
            if (x == null && y == null)
            {
                HorseWrapper horse = null;

                Horses.Where(h => h.Horse.getOwner() == Game1.player && h.Horse.getName() == Game1.player.horseName).Do(h => horse = h);

                if (horse != null)
                {
                    Game1.activeClickableMenu = new HorseMenu(this, horse);
                }
            }
        }

        private void OpenPetMenu(int? x = null, int? y = null)
        {
            if (x == null && y == null)
            {
                if (Game1.player.hasPet())
                {
                    Pet pet = Game1.player.getPet();

                    if (pet != null)
                    {
                        Game1.activeClickableMenu = new PetMenu(this, pet);
                    }
                }
            }
        }

        private bool IsEdible(Item item)
        {
            if (item.getCategoryName() == "Cooking")
            {
                return true;
            }

            if (item.healthRecoveredOnConsumption() > 0)
            {
                return true;
            }

            return false;
        }

        private int CalculateExpGain(Item item, int currentFriendship)
        {
            if (item.getCategoryName() == "Cooking")
            {
                return (int)Math.Floor((10 + (item.healthRecoveredOnConsumption() / 10)) * Math.Pow(1.2, -currentFriendship / 200));
            }
            else
            {
                return (int)Math.Floor((5 + (item.healthRecoveredOnConsumption() / 10)) * Math.Pow(1.2, -currentFriendship / 200));
            }
        }
    }
}