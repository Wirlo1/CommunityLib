using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Loki.Bot;
using Loki.Common;
using Loki.Game;

namespace CommunityLib
{
    public static class Data
    {
        public static readonly Dictionary<string, Vector2i> StashesLocations = 
            new Dictionary<string, Vector2i>
            {
                {"1_town", new Vector2i(246, 266)},
                {"2_town", new Vector2i(178, 195)},
                {"3_town", new Vector2i(206, 306)},
                {"4_town", new Vector2i(199, 509)}
            };

        public static readonly List<Tuple<string, string, Vector2i>> TownNpcStaticLocations =
            new List<Tuple<string, string, Vector2i>>
            {
                new Tuple<string, string, Vector2i>("1_town", "Nessa", new Vector2i(268, 253)),
                new Tuple<string, string, Vector2i>("1_town", "Tarkleigh", new Vector2i(312, 189)),
                new Tuple<string, string, Vector2i>("2_town", "Greust", new Vector2i(192, 173)),
                new Tuple<string, string, Vector2i>("2_town", "Yeena", new Vector2i(162, 240)),
                new Tuple<string, string, Vector2i>("3_town", "Clarissa", new Vector2i(147, 326)),
                new Tuple<string, string, Vector2i>("3_town", "Hargan", new Vector2i(281, 357)),
                new Tuple<string, string, Vector2i>("4_town", "Kira", new Vector2i(169, 500)),
                new Tuple<string, string, Vector2i>("4_town", "Petarus and Vanja", new Vector2i(204, 546))
            };

        public static readonly Dictionary<string, Vector2i> WaypointsLocations =
        new Dictionary<string, Vector2i>
        {
                    {"1_town", new Vector2i(196, 172)},
                    {"2_town", new Vector2i(188, 135)},
                    {"3_town", new Vector2i(217, 226)},
                    {"4_town", new Vector2i(286, 491)}
        };

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
        
        public static List<Stash.ExtendedStashItem> CachedItemsInStash = new List<Stash.ExtendedStashItem>();
        /// <summary>
        /// If set to true UpdateItemsInStash will have no effect. It's automatically setting to false on every area change. Use it only if you know what you are doing!
        /// </summary>
        public static bool ItemsInStashAlreadyCached;

        /// <summary>
        /// Goes to stash, parse every file and save's it in the CachedItemsInStash. It can be runned only once per area change. Other tries will not work (to save time)
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> UpdateItemsInStash()
        {
            //return Communication.GenericExecute<bool>("CommunityLib", "CacheItems", null);

            //No need to do it again
            if (ItemsInStashAlreadyCached)
                return true;

            // If stash isn't opened, abort this and return
            if (!await Stash.OpenStashTabTask())
                return false;

            //Delete current stuff
            CachedItemsInStash.Clear();

            while (true)
            {
                //Making sure we can count
                if (!LokiPoe.IsInGame)
                {
                    CommunityLib.Log.ErrorFormat("[CommunityLib][UpdateItemsInStash] Disconnected?");
                    return false;
                }

                //Stash not opened
                if (!LokiPoe.InGameState.StashUi.IsOpened)
                {
                    CommunityLib.Log.InfoFormat("[CommunityLib][UpdateItemsInStash] Stash not opened? Trying again.");
                    return await UpdateItemsInStash();
                }

                //Different handling for currency tabs
                if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumCurrency)
                {
                    foreach (var wrapper in LokiPoe.InGameState.StashUi.CurrencyTabInventoryControls
                        .Where(wrp => wrp.CurrencyTabItem != null))
                    {
                        CachedItemsInStash.Add( 
                            new Stash.ExtendedStashItem(wrapper, wrapper.CurrencyTabItem.LocalId, 
                                LokiPoe.InGameState.StashUi.TabControl.CurrentTabName)
                            );
                    }
                }
                else
                {
                    foreach (var item in LokiPoe.InGameState.StashUi.InventoryControl.Inventory.Items)
                        CachedItemsInStash.Add(
                            new Stash.ExtendedStashItem(LokiPoe.InGameState.StashUi.InventoryControl, item.LocalId,
                                LokiPoe.InGameState.StashUi.TabControl.CurrentTabName)
                            );
                }

                if (LokiPoe.InGameState.StashUi.TabControl.CurrentTabName == LokiPoe.InGameState.StashUi.TabControl.TabNames.Last())
                {
                    CommunityLib.Log.DebugFormat("[CommunityLib][UpdateItemsInStash] We're on the last tab: \"{0}\". Finishing.", 
                        LokiPoe.InGameState.StashUi.TabControl.CurrentTabName);
                    break;
                }

                CommunityLib.Log.DebugFormat("[CommunityLib][UpdateItemsInStash] Switching tabs. Current tab: \"{0}\"", 
                    LokiPoe.InGameState.StashUi.TabControl.CurrentTabName);
                var lastId = LokiPoe.InGameState.StashUi.StashTabInfo.InventoryId;
                if (LokiPoe.InGameState.StashUi.TabControl.NextTabKeyboard() != SwitchToTabResult.None)
                {
                    await Coroutines.ReactionWait();
                    CommunityLib.Log.ErrorFormat("[CommunityLib][UpdateItemsInStash] Failed to switch tabs.");
                    return false;
                }

                //Sleep to not look too bottish
                await Stash.WaitForStashTabChange(lastId);
                await Coroutines.LatencyWait(2);
            }

            ItemsInStashAlreadyCached = true;
            return true;
        }
    }
}
