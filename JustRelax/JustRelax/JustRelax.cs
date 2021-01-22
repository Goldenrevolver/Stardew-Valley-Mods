namespace JustRelax
{
    using StardewModdingAPI;
    using StardewModdingAPI.Events;
    using StardewValley;

    public class JustRelax : Mod
    {
        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.UpdateTicked += RelaxHeal;
        }

        private void RelaxHeal(object sender, UpdateTickedEventArgs args)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            // if we are in bed and in singleplayer (because multiplayer already has healing) or we are sitting
            if ((Game1.player.isInBed && !Game1.IsMultiplayer) || Game1.player.isSitting)
            {
                // if the game is not paused in singleplayer and half a second passed (same regen rate as bed in multiplayer)
                if (!(Game1.player.hasMenuOpen && !Game1.IsMultiplayer) && !Game1.paused && args.Ticks % 30 == 0)
                {
                    // upper case stamina property has an inbuilt check that it can't go over maxStamina so we can simply add one
                    if (Game1.player.Stamina < Game1.player.maxStamina)
                    {
                        Game1.player.Stamina++;
                    }

                    // health is an int so we can simply add one
                    if (Game1.player.health < Game1.player.maxHealth)
                    {
                        Game1.player.health++;
                    }
                }
            }
        }
    }
}