namespace HorseOverhaul
{
    using Microsoft.Xna.Framework.Graphics;
    using StardewValley;
    using System;

    public interface IBetterRanchingApi
    {
        void DrawHeartBubble(SpriteBatch spriteBatch, Character character, Func<bool> displayHeart);

        void DrawHeartBubble(SpriteBatch spriteBatch, float xPosition, float yPosition, int spriteWidth, Func<bool> displayHeart);

        void DrawItemBubble(SpriteBatch spriteBatch, FarmAnimal animal, bool ranchingInProgress);

        void DrawItemBubble(SpriteBatch spriteBatch, float xPosition, float yPosition, int spriteWidth, bool isShortTarget, int produceIcon, Func<bool> displayItem, Func<bool> displayHeart);
    }
}