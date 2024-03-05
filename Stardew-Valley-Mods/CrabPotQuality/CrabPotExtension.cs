using StardewValley;
using StardewValley.Objects;

namespace CrabPotQuality
{
    public static class CrabPotExtension
    {
        public static bool UsesMagnetBait(this CrabPot pot)
        {
            return pot?.bait.Value is not null && pot.bait.Value.QualifiedItemId == "(O)703";
        }

        public static bool UsesWildBait(this CrabPot pot)
        {
            return pot?.bait.Value is not null && pot.bait.Value.QualifiedItemId == "(O)774";
        }

        public static bool UsesMagicBait(this CrabPot pot)
        {
            return pot?.bait.Value is not null && pot.bait.Value.QualifiedItemId == "(O)908";
        }

        public static bool IsMariner(this Farmer farmer)
        {
            return farmer.professions.Contains(10);
        }

        public static bool IsLuremaster(this Farmer farmer)
        {
            return farmer.professions.Contains(11);
        }
    }
}