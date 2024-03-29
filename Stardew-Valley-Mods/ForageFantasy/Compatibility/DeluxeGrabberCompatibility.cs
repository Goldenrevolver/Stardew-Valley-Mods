﻿namespace ForageFantasy
{
    using Microsoft.Xna.Framework;
    using StardewValley;
    using System;
    using System.Collections.Generic;
    using StardewObject = StardewValley.Object;

    public interface IDeluxeGrabberReduxApi
    {
        Func<StardewObject, Vector2, GameLocation, KeyValuePair<StardewObject, int>> GetMushroomHarvest { get; set; }

        Func<StardewObject, Vector2, GameLocation, KeyValuePair<StardewObject, int>> GetBerryBushHarvest { get; set; }
    }

    public class DeluxeGrabberCompatibility
    {
        private static ForageFantasy mod;

        public static void Setup(ForageFantasy forageFantasy)
        {
            mod = forageFantasy;

            IDeluxeGrabberReduxApi api = mod.Helper.ModRegistry.GetApi<IDeluxeGrabberReduxApi>("ferdaber.DeluxeGrabberRedux");

            if (api == null)
            {
                return;
            }

            // if neither is on, we can skip adding our overwrite for better compatibility with other mods
            if (mod.Config.BerryBushQuality)
            {
                api.GetBerryBushHarvest += ChangeBerryBushHarvest;
            }

            // if neither is on, we can skip adding our overwrite for better compatibility with other mods
            if (mod.Config.MushroomBoxQuality)
            {
                api.GetMushroomHarvest += ChangeMushroomHarvest;
            }
        }

        private static KeyValuePair<StardewObject, int> ChangeBerryBushHarvest(StardewObject item, Vector2 tile, GameLocation location)
        {
            if (item == null || !(item.QualifiedItemId is BerryBushLogic.springBerries or BerryBushLogic.fallBerries))
            {
                return new KeyValuePair<StardewObject, int>(item, 0);
            }

            int expAmount = 0;

            if (mod.Config.BerryBushQuality)
            {
                item.Quality = ForageFantasy.DetermineForageQuality(Game1.player);
            }
            else
            {
                item.Quality = Game1.MasterPlayer.professions.Contains(Farmer.botanist) ? StardewObject.bestQuality : StardewObject.lowQuality;
            }

            return new KeyValuePair<StardewObject, int>(item, expAmount);
        }

        private static KeyValuePair<StardewObject, int> ChangeMushroomHarvest(StardewObject item, Vector2 tile, GameLocation location)
        {
            if (item == null)
            {
                return new KeyValuePair<StardewObject, int>(item, 0);
            }

            int expAmount = 0;

            item.Quality = mod.Config.MushroomBoxQuality ? ForageFantasy.DetermineForageQuality(Game1.MasterPlayer) : StardewObject.lowQuality;

            return new KeyValuePair<StardewObject, int>(item, expAmount);
        }
    }
}