using System;
using System.Linq;
using System.Threading.Tasks;
using Loki.Bot.Logic.Bots.OldGrindBot;
using Loki.Game;
using Loki.Game.Objects;
using StashUI = Loki.Game.LokiPoe.InGameState.StashUi;

namespace CommunityLib
{
    //Testingzs
    public class Stash
    {
        public delegate bool FindItemDelegate(Item item);
        public class StashItem
        {
            public InventoryControlWrapper Wrapper;
            public int ItemId;
            public Item Item => Wrapper?.Inventory.GetItemById(ItemId);

            public StashItem(InventoryControlWrapper wrp, int it)
            {
                Wrapper = wrp;
                ItemId = it;
            }
        }

        /// <summary>
        /// Return the InventoryControlWrapper for an item and its class
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns>StashItem</returns>
        public static StashItem FindItemInStashTab(string itemName)
        {
            return FindItemInStashTab(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// Return the InventoryControlWrapper for an item and its class
        /// </summary>
        /// <param name="condition"></param>
        /// <returns>StashItem</returns>
        public static StashItem FindItemInStashTab(FindItemDelegate condition)
        {
            //If it's regular tab then it's rather simple
            if (!StashUI.StashTabInfo.IsPremiumCurrency)
            {
                // Gather the first item matching the condition
                var item = StashUI.InventoryControl.Inventory.Items.FirstOrDefault(d => condition(d));
                // Return it if this one is not null
                if (item != null)
                    return new StashItem(StashUI.InventoryControl, item.LocalId);
            }

            //Premium stash tab
            else
            {
                var wrapper = StashUI.CurrencyTabInventoryControls.FirstOrDefault(d => d.CurrencyTabItem != null && condition(d.CurrencyTabItem));
                var item = wrapper?.CurrencyTabItem;
                if (item != null)
                    return new StashItem(wrapper, item.LocalId);
            }

            return null;
        }

        /// <summary>
        /// This function iterates through the stash to find an item by name
        /// If a tab is reached and the item is found, GUI will be stopped on this tab so you can directly interact with it.
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, StashItem>> FindTabContainingItem(string itemName)
        {
            // If stash isn't opened, abort this and return
            if (!StashUI.IsOpened)
                return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.GuiNotOpened, null);

            // If we fail to go to first tab, return
            if (GoToFirstTab() != SwitchToTabResult.None)
                return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.GoToFirstTabFailed, null);

            foreach (var tabName in StashUI.TabControl.TabNames)
            {             
                // If the item has no occurences in this tab, switch to the next one
                var it = FindItemInStashTab(itemName);
                if (it.Item == null)
                {
                    // On last tab? break execution
                    if (StashUI.TabControl.IsOnLastTab)
                        break;

                    int switchAttemptsPerTab = 0;
                    while (true)
                    {
                        // If we tried 3 times to switch and failed, return
                        if (switchAttemptsPerTab > 2)
                            return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.SwitchToTabFailed, null);

                        var switchTab = StashUI.TabControl.SwitchToTabMouse(tabName);

                        // If the switch went fine, keep searching
                        if (switchTab == SwitchToTabResult.None)
                            break;

                        switchAttemptsPerTab++;
                        await Coroutines.LatencyWait();
                        await Coroutines.ReactionWait();
                    }

                    // Keep searching...
                    await Coroutines.LatencyWait();
                    await Coroutines.ReactionWait();
                    continue;
                }

                // We Found a tab, return informations
                return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.None, it);
            }

            return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.ItemNotFoundInTab, null);
        }

        /// <summary>
        /// Heads to the first tab in stash (stash must be opened)
        /// </summary>
        /// <returns>SwitchToTabResult enum entry</returns>
        public static SwitchToTabResult GoToFirstTab()
        {
            return StashUI.TabControl.SwitchToTabMouse(0);
        }

        /// <summary>
        /// Heads to the last tab in stash (stash must be opened)
        /// </summary>
        /// <returns>SwitchToTabResult enum entry</returns>
        public static SwitchToTabResult GoToLastTab()
        {
            return StashUI.TabControl.SwitchToTabMouse(StashUI.TabControl.TabNames.Count - 1);
        }
    }
}
