using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CommunityLib
{
    public static class Data
    {
        private static readonly string[] Currency =
        {
            "Scroll of Wisdom", "Portal Scroll", "Orb of Transmutation",
            "Orb of Augmentation", "Orb of Alteration", "Jeweller's Orb",
            "Armourer's Scrap", "Blacksmith's Whetstone", "Glassblower's Bauble",
            "Cartographer's Chisel", "Gemcutter's Prism", "Chromatic Orb",
            "Orb of Fusing", "Orb of Chance", "Orb of Alchemy", "Regal Orb",
            "Exalted Orb", "Chaos Orb", "Blessed Orb", "Divine Orb",
            "Orb of Scouring", "Orb of Regret", "Vaal Orb", "Mirror of Kalandra"
        };

        public static readonly ReadOnlyCollection<string> CurrencyList = new ReadOnlyCollection<string>(Currency);

    }
}
