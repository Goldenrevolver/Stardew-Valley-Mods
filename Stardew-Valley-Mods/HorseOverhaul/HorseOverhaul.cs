using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HorseOverhaul
{
    public class HorseOverhaul : Mod, IAssetEditor
    {
        // how stable cords look

        // (tile.x, tile.y), (tile.x+1, tile.y), (tile.x+2, tile.y), (tile.x+3, tile.y)
        // (tile.x, tile.y+1), (tile.x+1, tile.y+1), (tile.x+2, tile.y+1), (tile.x+3, tile.y+1)

        //public static int openMenuX;

        //public static int openMenuY;

        //public const int INVENTORY_TAB = 0;

        public readonly List<HorseWrapper> Horses = new List<HorseWrapper>();

        public Texture2D HorseSpriteWithHead { get; set; }

        public HorseConfig Config { get; set; }

        // TODO do the same interaction menu for the cat/dog, including feeding and also especially liked food
        // TODO fix menu for zoom

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<HorseConfig>();

            HorseConfig.VerifyConfigValues(Config, this);

            Helper.Events.GameLoop.GameLaunched += delegate { HorseConfig.SetUpModConfigMenu(Config, this); };

            Helper.Events.GameLoop.Saving += delegate { SaveChest(); };
            Helper.Events.GameLoop.DayStarted += delegate { OnDayStarted(); };

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            //helper.Events.Display.MenuChanged += OnMenuChanged;

            Patcher.PatchAll(this);
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetName == "Animals\\horse" && Config.ThinHorse;
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
                editor.PatchImage(sourceImage, targetArea: new Rectangle?(new Rectangle(160, 96, 9, 15)));
            }
        }

        private void OnDayStarted()
        {
            for (int i = 0; i < Horses.Count; i++)
            {
                Horses[i] = null;
            }

            Horses.Clear();

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building is Stable stable)
                {
                    if (Config.Water)
                    {
                        stable.texture = new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>("assets/stable_empty.png"));
                    }

                    Chest saddleBag = null;

                    int stableID = 0;

                    string modData;

                    stable.modData.TryGetValue($"{ModManifest.UniqueID}/stableID", out modData);

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
                if (building is Stable stable)
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
                Point cursorPosition = Game1.getMousePosition();

                CheckHorseInteraction(Game1.currentLocation, cursorPosition.X + Game1.viewport.X, cursorPosition.Y + Game1.viewport.Y);

                //OpenHorseMenu(cursorPosition.X, cursorPosition.Y);
            }
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
            {
                return;
            }

            // this is done in buttonsChanged instead of buttonPressed as recommend in the documentation: https://stardewcommunitywiki.com/Modding:Modder_Guide/APIs/Input#KeybindList
            if (Config.MenuKey.JustPressed())
            {
                OpenHorseMenu();
            }
        }

        private void CheckHorseInteraction(GameLocation currentLocation, int x, int y)
        {
            // Find if click was on Horse
            foreach (Horse horse in currentLocation.characters.OfType<Horse>())
            {
                // Can only feed your own horse
                if (horse.getOwner() != Game1.player && horse.getName() == Game1.player.horseName)
                {
                    return;
                }

                HorseWrapper horseW = null;

                Horses.Where(h => h.Horse == horse).Do(h => horseW = h);

                if (horseW == null)
                {
                    return;
                }

                if (Utility.withinRadiusOfPlayer((int)(horse.Position.X), (int)(horse.Position.Y), 1, Game1.player)
                && (Utility.distance((float)x, horse.Position.X, (float)y, horse.Position.Y) <= 110))
                {
                    if (Game1.player.CurrentItem != null && Config.Food)
                    {
                        // Holding food
                        Item currentItem = Game1.player.CurrentItem;
                        if (IsEdible(currentItem))
                        {
                            Item food = Game1.player.CurrentItem;

                            if (horseW.GotFed)
                            {
                                Game1.drawObjectDialogue(Helper.Translation.Get("AteEnough", new { horseName = horse.name }));
                            }
                            else
                            {
                                Game1.drawObjectDialogue(Helper.Translation.Get("AteFood", new { horseName = horse.name, foodName = food.Name }));

                                if (Config.ThinHorse)
                                {
                                    horse.doEmote(20);
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
                        }
                    }
                }
            }
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

                return;
            }

            // TODO fix this including for zoom
            //if (Game1.activeClickableMenu is GameMenu menu)
            //{
            //    if (menu.currentTab == INVENTORY_TAB)
            //    {
            //        Vector2 rectangle = new Vector2((float)((double)(openMenuX + Game1.tileSize * 5 + Game1.pixelZoom * 2) + (double)Math.Max((float)Game1.tileSize, Game1.dialogueFont.MeasureString(Game1.player.name).X / 2f) + (Game1.player.getPetDisplayName() != null ? (double)Math.Max((float)Game1.tileSize, Game1.dialogueFont.MeasureString(Game1.player.getPetDisplayName()).X) : 0.0)), (float)(openMenuY + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 7 * Game1.tileSize - Game1.pixelZoom));
            //        if (Utility.distance((float)x, rectangle.X, (float)y, rectangle.Y) <= 100)
            //        {
            //            HorseWrapper horse = null;

            //            Horses.Where(h => h.Horse.getOwner() == Game1.player && h.Horse.getName() == Game1.player.horseName).Do(h => horse = h);

            //            if (horse != null)
            //            {
            //                Game1.activeClickableMenu = (IClickableMenu)new HorseMenu(this, horse);
            //            }

            //            return;
            //        }
            //    }
            //}
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
                return (int)Math.Floor((10 + item.healthRecoveredOnConsumption() / 10) * Math.Pow(1.2, -currentFriendship / 200));
            }
            else
            {
                return (int)Math.Floor((5 + item.healthRecoveredOnConsumption() / 10) * Math.Pow(1.2, -currentFriendship / 200));
            }
        }

        //private void OnMenuChanged(object sender, MenuChangedEventArgs args)
        //{
        //    if (!Context.IsMainPlayer)
        //    {
        //        return;
        //    }
        //    if (args.NewMenu is GameMenu gm)
        //    {
        //        openMenuX = gm.xPositionOnScreen;
        //        openMenuY = gm.yPositionOnScreen;
        //    }
        //    else if (args.OldMenu is GameMenu ogm)
        //    {
        //        openMenuX = ogm.xPositionOnScreen;
        //        openMenuY = ogm.yPositionOnScreen;
        //    }
        //}
    }
}