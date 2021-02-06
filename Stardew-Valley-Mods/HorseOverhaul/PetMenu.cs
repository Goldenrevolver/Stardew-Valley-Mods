namespace HorseOverhaul
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using StardewValley;
    using StardewValley.Characters;
    using StardewValley.Menus;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PetMenu : IClickableMenu
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

        private Pet pet;

        private HorseOverhaul mod;

        public PetMenu(HorseOverhaul mod, Pet pet)
          : base((Game1.viewport.Width / 2) - (PetMenu.width / 2), (Game1.viewport.Height / 2) - (PetMenu.height / 2), PetMenu.width, PetMenu.height, false)
        {
            this.mod = mod;
            Game1.player.Halt();
            PetMenu.width = Game1.tileSize * 6;
            PetMenu.height = Game1.tileSize * 8;

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

            this.textBox.Text = pet.displayName;

            this.textBox.Selected = false;

            var yPos1 = this.yPositionOnScreen + PetMenu.height - Game1.tileSize - IClickableMenu.borderWidth;
            var yPos2 = this.yPositionOnScreen - (Game1.tileSize / 2) + IClickableMenu.spaceToClearTopBorder + (Game1.tileSize * 4) - (Game1.tileSize / 2);

            ClickableTextureComponent textureComponent1 = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + PetMenu.width + 4, yPos1, Game1.tileSize, Game1.tileSize), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false)
            {
                myID = 101,
                upNeighborID = 103
            };

            this.okayButton = textureComponent1;

            ClickableTextureComponent textureComponent5 = new ClickableTextureComponent(
                (pet.friendshipTowardFarmer.Value / 10.0).ToString() + "<",
                new Rectangle(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (Game1.tileSize / 2) + 16, yPos2, PetMenu.width - (Game1.tileSize * 2), Game1.tileSize),
                (string)null, mod.Helper.Translation.Get("Friendship"), Game1.mouseCursors, new Rectangle(172, 512, 16, 16), 4f, false)
            {
                myID = 102
            };

            this.love = textureComponent5;
            this.loveHover = new ClickableComponent(new Rectangle(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder, this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (Game1.tileSize * 3) - (Game1.tileSize / 2), PetMenu.width, Game1.tileSize), "Friendship")
            {
                myID = 109
            };

            this.pet = pet;

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
                Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen + (Game1.tileSize * 2), PetMenu.width, PetMenu.height - (Game1.tileSize * 2), false, true, (string)null, false);

                var yPos1 = (float)(yPositionOnScreen + spaceToClearTopBorder + (Game1.tileSize / 4) + (Game1.tileSize * 2));
                var yPos2 = yPos1 + (Game1.tileSize / 2) + (Game1.tileSize / 4);

                string status = this.getStatusMessage();
                Utility.drawTextWithShadow(b, status, Game1.smallFont, new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (Game1.tileSize / 2)), yPos2), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);

                double friendshipLevel = pet.friendshipTowardFarmer.Value;

                int num3 = (int)((friendshipLevel % 200.0 >= 100.0) ? (friendshipLevel / 200.0) : -100.0);

                for (int index = 0; index < 5; ++index)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)(this.xPositionOnScreen + (Game1.tileSize * 3 / 2) + (8 * Game1.pixelZoom * index)), yPos1), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(211 + (friendshipLevel <= (double)((index + 1) * 195) ? 7 : 0), 428, 7, 6)), Color.White, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.89f);
                    if (num3 == index)
                    {
                        b.Draw(Game1.mouseCursors, new Vector2((float)(this.xPositionOnScreen + (Game1.tileSize * 3 / 2) + (8 * Game1.pixelZoom * index)), yPos1), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(211, 428, 4, 6)), Color.White, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.891f);
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

            string petAnswer = pet.grantedFriendshipForPet.Value ? yes : no;
            string waterAnswer = Game1.getFarm().petBowlWatered.Value ? yes : no;
            string foodAnswer = pet?.modData?.TryGetValue($"{mod.ModManifest.UniqueID}/gotFed", out _) == true ? yes : no;

            string friendship = mod.Helper.Translation.Get("Friendship2", new { value = pet.friendshipTowardFarmer.Value }) + "\n";
            string petted = mod.Config.Petting ? mod.Helper.Translation.Get("GotPetted", new { value = petAnswer }) + "\n" : string.Empty;
            string water = mod.Config.Water ? mod.Helper.Translation.Get("GotWater", new { value = waterAnswer }) + "\n" : string.Empty;
            string food = mod.Config.PetFeeding ? mod.Helper.Translation.Get("GotFood", new { value = foodAnswer }) : string.Empty;

            return $"{friendship}{petted}{water}{food}";
        }
    }
}