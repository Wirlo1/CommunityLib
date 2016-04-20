using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot.Logic.Bots.OldGrindBot;
using Loki.Common;
using Loki.Game;
using Loki.Game.Objects;
using StashUI = Loki.Game.LokiPoe.InGameState.StashUi;

namespace CommunityLib
{
    public class Stash
    {
        public delegate bool FindItemDelegate(Item item);
        public class StashItem
        {
            public InventoryControlWrapper Wrapper;
            public int ItemId;

            public Item Item
            {
                get
                {
                    if (Wrapper == null)
                        return null;

                    return Wrapper.HasCurrencyTabOverride ? Wrapper.CurrencyTabItem : Wrapper.Inventory.GetItemById(ItemId);
                }
            }

            public StashItem(InventoryControlWrapper wrp, int it)
            {
                Wrapper = wrp;
                ItemId = it;
            }
        }

        public class ExtendedStashItem : StashItem
        {
            public string TabName;
            public string Name;
            public string FullName;
            public string League;

            public Vector2i LocationInTab;
            public int StackCount;
            public int MaxStackCount;
            public int MaxCurrencyTabStackCount;
            public int SocketCount;
            public int Links;
            public int Quality;

            public bool IsInCurrencyTab => MaxCurrencyTabStackCount > 0;


            public ExtendedStashItem(InventoryControlWrapper wrp, int it, string tabName) : base(wrp, it)
            {
                TabName = tabName;
                var item = Item;
                if (item == null) return;

                Name = item.Name;
                FullName = item.FullName;
                League = LokiPoe.Me.League;

                StackCount = item.StackCount;
                MaxStackCount = item.MaxStackCount;
                LocationInTab = item.LocationTopLeft;
                SocketCount = item.SocketCount;
                Links = item.MaxLinkCount;

                if (wrp.HasCurrencyTabOverride)
                    MaxCurrencyTabStackCount = item.MaxCurrencyTabStackCount;
            }

            /// <summary>
            /// Opens the stash and the stash tab of this item.
            /// </summary>
            /// <returns></returns>
            public async Task<bool> GoTo()
            {
                if (string.IsNullOrEmpty(TabName))
                    return false;

                return await OpenStashTabTask(TabName);
            }


            public async Task<bool> FastMove(int retries = 3)
            {
                if (!await GoTo())
                    return false;
                return await Inventory.FastMove(Wrapper, Item.LocalId, retries);
            }

            public async Task<ApplyCursorResult> UseOnItem(InventoryControlWrapper destinationWrapper, Item destinationItem,
                Func<Item, int, bool> delegateToStop = null)
            {
                if (!await GoTo())
                    return ApplyCursorResult.ItemNotFound;

                return await Inventory.UseItemOnItem(Wrapper, Item, destinationWrapper, destinationItem);
            }

            public async Task<bool> SplitAndPlaceInMainInventory(int pickupAmount)
            {
                if (!await GoTo())
                    return false;
                return await Inventory.SplitAndPlaceItemInMainInventory(Wrapper, Item, pickupAmount);
            }

            public override string ToString()
            {
                return $"{FullName} [Tab : {TabName} | Location : {LocationInTab} | League : {League} | Sockets : {SocketCount} | Links : {Links} | Quality : {Quality}]";
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
            if (!await OpenStashTabTask())
                return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.GuiNotOpened, null);

            // If we fail to go to first tab, return
            // if (GoToFirstTab() != SwitchToTabResult.None)
            //     return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.GoToFirstTabFailed, null);

            foreach (var tabName in StashUI.TabControl.TabNames)
            {             
                // If the item has no occurences in this tab, switch to the next one
                var it = FindItemInStashTab(itemName);
                if (it == null)
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
            return StashUI.TabControl.SwitchToTabMouse(StashUI.TabControl.LastTabIndex);
        }

        /// <summary>
        /// Opens the stash at typed tab name
        /// </summary>
        /// <param name="stashTabName">If set to null or empty, first tab of the stash will be opened</param>
        /// <returns></returns>
        public static async Task<bool> OpenStashTabTask(string stashTabName = "")
        {
            //open stash
            if (!StashUI.IsOpened)
            {
                var isOpenedErr = await Coroutines.OpenStash();
                await Coroutines.WaitForStashPanel();
                if (isOpenedErr != Coroutines.OpenStashError.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to open the stash. Error: {0}", isOpenedErr);
                    return false;
                }
            }

            if (string.IsNullOrEmpty(stashTabName))
            {
                var isSwitchedFtErr = GoToFirstTab(); //Stash.GoToFirstTab()
                if (isSwitchedFtErr != SwitchToTabResult.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to switch to the first tab");
                    return false;
                }

                return true;
            }

            if (StashUI.TabControl.CurrentTabName != stashTabName)
            {
                var isSwitchedErr = StashUI.TabControl.SwitchToTabKeyboard(stashTabName);
                if (isSwitchedErr != SwitchToTabResult.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to switch to the tab: {0}", isSwitchedErr);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Waits for a stash tab to change. Pass -1 to lastId to wait for the initial tab.
        /// </summary>
        /// <param name="lastId">The last InventoryId before changing tabs.</param>
        /// <param name="timeout">The timeout of the function.</param>
        /// <returns>true if the tab was changed and false otherwise.</returns>
        public static async Task<bool> WaitForStashTabChange(int lastId = -1, int timeout = 5000)
        {
            var sw = Stopwatch.StartNew();
            var invTab = StashUI.StashTabInfo;
            while (invTab == null || invTab.InventoryId == lastId)
            {
                await Coroutine.Sleep(1);
                invTab = StashUI.StashTabInfo;
                if (sw.ElapsedMilliseconds > timeout)
                    return false;
            }
            return true;
        }
    }
}
