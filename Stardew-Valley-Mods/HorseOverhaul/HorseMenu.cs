namespace HorseOverhaul
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using StardewValley;
    using StardewValley.Menus;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HorseMenu : IClickableMenu
    {
        private static new int width = Game1.tileSize * 6;
        private static new int height = Game1.tileSize * 8;

        private string hoverText = string.Empty;

        //// private const int region_okButton = 101;
        //// private const int region_love = 102;
        //// private const int region_sellButton = 103;
        //// private const int region_fullnessHover = 107;
        //// private const int region_happinessHover = 108;
        //// private const int region_loveHover = 109;
        //// private const int region_textBoxCC = 110;

        private TextBox textBox;
        private ClickableTextureComponent okayButton;

        private ClickableTextureComponent love;
        private ClickableComponent loveHover;
        private ClickableComponent textBoxCC;

        private HorseWrapper horse;

        private HorseOverhaul mod;

        public HorseMenu(HorseOverhaul mod, HorseWrapper horse)
          : base((Game1.viewport.Width / 2) - (HorseMenu.width / 2), (Game1.viewport.Height / 2) - (HorseMenu.height / 2), HorseMenu.width, HorseMenu.height, false)
        {
            this.mod = mod;
            Game1.player.Halt();
            HorseMenu.width = Game1.tileSize * 6;
            HorseMenu.height = Game1.tileSize * 8;

            this.textBox = new TextBox((Texture2D)null, (Texture2D)null, Game1.dialogueFont, Game1.textColor);
            this.textBox.X = (Game1.viewport.Width / 2) - (Game1.tileSize * 2) - 12;
            this.textBox.Y = this.yPositionOnScreen - 4 + (Game1.tileSize * 2);
            this.textBox.Width = Game1.tileSize * 4;
            this.textBox.Height = Game1.tileSize * 3;

            this.textBoxCC = new ClickableComponent(new Rectangle(this.textBox.X, this.textBox.Y, this.textBox.Width, Game1.tileSize), string.Empty)
            {
                myID = 110,
                downNeighborID = 104
            };
            this.textBox.Text = Game1.player.horseName;

            this.textBox.Selected = false;

            ClickableTextureComponent textureComponent1 = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + HorseMenu.width + 4, this.yPositionOnScreen + HorseMenu.height - Game1.tileSize - IClickableMenu.borderWidth, Game1.tileSize, Game1.tileSize), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            int num1 = 101;
            textureComponent1.myID = num1;
            int num2 = 103;
            textureComponent1.upNeighborID = num2;
            this.okayButton = textureComponent1;

            ClickableTextureComponent textureComponent5 = new ClickableTextureComponent(
                (horse.Friendship / 10.0).ToString() + "<",
                new Rectangle(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (Game1.tileSize / 2) + 16, this.yPositionOnScreen - (Game1.tileSize / 2) + IClickableMenu.spaceToClearTopBorder + (Game1.tileSize * 4) - (Game1.tileSize / 2), HorseMenu.width - (Game1.tileSize * 2), Game1.tileSize),
                (string)null, mod.Helper.Translation.Get("Friendship"), Game1.mouseCursors, new Rectangle(172, 512, 16, 16), 4f, false);

            int num10 = 102;
            textureComponent5.myID = num10;
            this.love = textureComponent5;
            this.loveHover = new ClickableComponent(new Rectangle(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder, this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (Game1.tileSize * 3) - (Game1.tileSize / 2), HorseMenu.width, Game1.tileSize), "Friendship")
            {
                myID = 109
            };

            this.horse = horse;

            if (!Game1.options.SnappyMenus)
            {
                return;
            }

            this.populateClickableComponentList();
            this.snapToDefaultClickableComponent();
        }

        public override void snapToDefaultClickableComponent()
        {
            this.currentlySnappedComponent = this.getComponentWithID(101);
            this.snapCursorToCurrentSnappedComponent();
        }

        public void textBoxEnter(TextBox sender)
        {
        }

        public override void receiveKeyPress(Keys key)
        {
            if (Game1.globalFade)
            {
                return;
            }

            if (((IEnumerable<InputButton>)Game1.options.menuButton).Contains<InputButton>(new InputButton(key)) && (this.textBox == null || !this.textBox.Selected))
            {
                Game1.playSound("smallSelect");
                if (this.readyToClose())
                {
                    Game1.exitActiveMenu();
                    if (this.textBox.Text.Length <= 0)
                    {
                        return;
                    }
                }
            }
            else
            {
                if (!Game1.options.SnappyMenus || (((IEnumerable<InputButton>)Game1.options.menuButton).Contains<InputButton>(new InputButton(key)) && this.textBox != null && this.textBox.Selected))
                {
                    return;
                }

                base.receiveKeyPress(key);
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            int num1 = Game1.getOldMouseX() + Game1.viewport.X;
            int num2 = Game1.getOldMouseY() + Game1.viewport.Y;

            if (num1 - Game1.viewport.X < Game1.tileSize)
            {
                Game1.panScreen(-8, 0);
            }
            else if (num1 - (Game1.viewport.X + Game1.viewport.Width) >= -Game1.tileSize)
            {
                Game1.panScreen(8, 0);
            }

            if (num2 - Game1.viewport.Y < Game1.tileSize)
            {
                Game1.panScreen(0, -8);
            }
            else if (num2 - (Game1.viewport.Y + Game1.viewport.Height) >= -Game1.tileSize)
            {
                Game1.panScreen(0, 8);
            }
        }

        public void finishedPlacinghorse()
        {
            Game1.exitActiveMenu();
            Game1.currentLocation = Game1.player.currentLocation;
            Game1.currentLocation.resetForPlayerEntry();
            Game1.globalFadeToClear((Game1.afterFadeFunction)null, 0.02f);
            Game1.displayHUD = true;
            Game1.viewportFreeze = false;
            Game1.displayFarmer = true;
            Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\UI:horseQuery_Moving_HomeChanged"), Color.LimeGreen, 3500f));
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (Game1.globalFade)
            {
                return;
            }

            if (this.okayButton != null)
            {
                if (this.okayButton.containsPoint(x, y))
                {
                    Game1.exitActiveMenu();
                }
            }
        }

        public override bool readyToClose()
        {
            this.textBox.Selected = false;
            if (base.readyToClose())
            {
                return !Game1.globalFade;
            }

            return false;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (Game1.globalFade)
            {
                return;
            }

            if (this.okayButton != null)
            {
                if (this.okayButton.containsPoint(x, y))
                {
                    Game1.exitActiveMenu();
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverText = string.Empty;
            if (this.okayButton != null)
            {
                if (this.okayButton.containsPoint(x, y))
                {
                    this.okayButton.scale = Math.Min(1.1f, this.okayButton.scale + 0.05f);
                }
                else
                {
                    this.okayButton.scale = Math.Max(1f, this.okayButton.scale - 0.05f);
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            if (!Game1.globalFade)
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
                this.textBox.Draw(b);
                Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen + (Game1.tileSize * 2), HorseMenu.width, HorseMenu.height - (Game1.tileSize * 2), false, true, (string)null, false);

                string status = this.getStatusMessage();
                Utility.drawTextWithShadow(b, status, Game1.smallFont, new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (Game1.tileSize / 2)), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (Game1.tileSize / 4) + (Game1.tileSize * 2))), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);

                double friendshipLevel = horse.Friendship;

                int num3 = (int)((friendshipLevel % 200.0 >= 100.0) ? (friendshipLevel / 200.0) : -100.0);

                for (int index = 0; index < 5; ++index)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)(this.xPositionOnScreen + (Game1.tileSize * 3 / 2) + (8 * Game1.pixelZoom * index)), (float)(20 + this.yPositionOnScreen - (Game1.tileSize / 2) + (Game1.tileSize * 6))), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(211 + (friendshipLevel <= (double)((index + 1) * 195) ? 7 : 0), 428, 7, 6)), Color.White, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.89f);
                    if (num3 == index)
                    {
                        b.Draw(Game1.mouseCursors, new Vector2((float)(this.xPositionOnScreen + (Game1.tileSize * 3 / 2) + (8 * Game1.pixelZoom * index)), (float)(20 + this.yPositionOnScreen - (Game1.tileSize / 2) + (Game1.tileSize * 6))), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(211, 428, 4, 6)), Color.White, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.891f);
                    }
                }

                this.okayButton.draw(b);

                if (this.hoverText != null && this.hoverText.Length > 0)
                {
                    IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont, 0, 0, -1, (string)null, -1, (string[])null, (Item)null, 0, -1, -1, -1, -1, 1f, (CraftingRecipe)null);
                }
            }

            base.drawMouse(b);
        }

        private string getStatusMessage()
        {
            string yes = mod.Helper.Translation.Get("Yes");
            string no = mod.Helper.Translation.Get("No");

            string petAnswer = horse.WasPet ? yes : no;
            string waterAnswer = horse.GotWater ? yes : no;
            string foodAnswer = horse.GotFed ? yes : no;

            string pet = mod.Config.Petting ? mod.Helper.Translation.Get("GotPetted", new { value = petAnswer }) + "\n" : string.Empty;
            string water = mod.Config.Water ? mod.Helper.Translation.Get("GotWater", new { value = waterAnswer }) + "\n" : string.Empty;
            string food = mod.Config.Food ? mod.Helper.Translation.Get("GotFood", new { value = foodAnswer }) + "\n" : string.Empty;
            string friendship = mod.Helper.Translation.Get("Friendship2", new { value = horse.Friendship });

            return $"{pet}{water}{food}{friendship}";
        }
    }
}