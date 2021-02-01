using StardewValley.Characters;
using StardewValley.Objects;

namespace HorseOverhaul
{
    public class HorseWrapper
    {
        public Horse Horse { get; set; }

        public bool WasPet { get; set; }

        public bool GotFed { get; set; }

        public bool GotWater { get; set; }

        public Chest SaddleBag { get; set; }

        public int Friendship { get => GetFriendship(Horse); set => SetFriendship(Horse, value); }

        private HorseOverhaul mod;

        public HorseWrapper(Horse horse, HorseOverhaul mod, Chest saddleBag)
        {
            Horse = horse;
            this.mod = mod;
            SaddleBag = saddleBag;
        }

        public void JustGotWater()
        {
            if (!GotWater)
            {
                GotWater = true;
                Friendship += 6;
            }
        }

        public void JustGotFood(int xpAmount)
        {
            if (!GotFed)
            {
                GotFed = true;
                Friendship += xpAmount;
            }
        }

        public void JustGotPetted()
        {
            if (!WasPet)
            {
                WasPet = true;
                Friendship += 12;
            }
        }

        public float GetMovementSpeedBonus()
        {
            int f = Friendship;

            // this is intentionally integer division
            int halfHearts = f / 100;

            float maxSpeed = mod.Config.MaxMovementSpeedBonus;

            return maxSpeed / 10 * halfHearts;
        }

        private int GetFriendship(Horse horse)
        {
            string moddata;
            horse.modData.TryGetValue($"{mod.ModManifest.UniqueID}/friendship", out moddata);

            int friendship = 0;

            if (!string.IsNullOrEmpty(moddata))
            {
                friendship = int.Parse(moddata);

                if (friendship > 1000)
                {
                    friendship = 1000;
                }

                horse.modData[$"{mod.ModManifest.UniqueID}/friendship"] = friendship.ToString();
            }
            else
            {
                horse.modData.Add($"{mod.ModManifest.UniqueID}/friendship", friendship.ToString());
            }

            return friendship;
        }

        private int SetFriendship(Horse horse, int friendship)
        {
            if (friendship > 1000)
            {
                friendship = 1000;
            }

            if (horse.modData.ContainsKey($"{mod.ModManifest.UniqueID}/friendship"))
            {
                horse.modData[$"{mod.ModManifest.UniqueID}/friendship"] = friendship.ToString();
            }
            else
            {
                horse.modData.Add($"{mod.ModManifest.UniqueID}/friendship", friendship.ToString());
            }

            return friendship;
        }
    }
}