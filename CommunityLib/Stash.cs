using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Game;
using Loki.Game.Objects;
using StashUI = Loki.Game.LokiPoe.InGameState.StashUi;

namespace CommunityLib
{
    public class Stash
    {
        /// <summary>
        /// Return the InventoryControlWrapper for an item and its class
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns>StashItem</returns>
        public static CachedItem FindItemInStashTab(string itemName)
        {
            return FindItemInStashTab(d => d.FullName.Equals(itemName));
        }

        public static CachedItem FindItemInStashTab(CommunityLib.FindItemDelegate condition)
        {
            //If it's regular tab then it's rather simple
            if (!StashUI.StashTabInfo.IsPremiumCurrency)
            {
                // Gather the first item matching the condition
                var item = StashUI.InventoryControl.Inventory.Items.FirstOrDefault(d => condition(d));
                // Return it if this one is not null
                if (item != null)
                    return new CachedItem(StashUI.InventoryControl, item.LocalId, StashUI.TabControl.CurrentTabName);
            }

            //Premium stash tab
            else
            {
                var wrapper = StashUI.CurrencyTabInventoryControls.FirstOrDefault(d => d.CurrencyTabItem != null && condition(d.CurrencyTabItem));
                var item = wrapper?.CurrencyTabItem;
                if (item != null)
                    return new CachedItem(wrapper, item.LocalId, StashUI.TabControl.CurrentTabName);
            }

            return null;
        }

        /// <summary>
        /// Overload for FindTabContainingItem to an item by its name
        /// </summary>
        /// <param name="itemName">The item name</param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, CachedItem>> FindTabContainingItem(string itemName)
        {
            return await FindTabContainingItem(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// This function iterates through the stash to find an item by name
        /// If a tab is reached and the item is found, GUI will be stopped on this tab so you can directly interact with it.
        /// </summary>
        /// <param name="condition">Condition to pass item through</param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, CachedItem>> FindTabContainingItem(CommunityLib.FindItemDelegate condition)
        {
            // If stash isn't opened, abort this and return
            if (!await OpenStashTabTask())
                return new Tuple<Results.FindItemInTabResult, CachedItem>(Results.FindItemInTabResult.GuiNotOpened, null);

            // If we fail to go to first tab, return
            // if (GoToFirstTab() != SwitchToTabResult.None)
            //     return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.GoToFirstTabFailed, null);

            foreach (var tabName in StashUI.TabControl.TabNames)
            {             
                // If the item has no occurences in this tab, switch to the next one
                var it = FindItemInStashTab(condition);
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
                            return new Tuple<Results.FindItemInTabResult, CachedItem>(Results.FindItemInTabResult.SwitchToTabFailed, null);

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
                return new Tuple<Results.FindItemInTabResult, CachedItem>(Results.FindItemInTabResult.None, it);
            }

            return new Tuple<Results.FindItemInTabResult, CachedItem>(Results.FindItemInTabResult.ItemNotFoundInTab, null);
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
                var isOpenedErr = await LibCoroutines.OpenStash();
                await Dialog.WaitForPanel(Dialog.PanelType.Stash);
                if (isOpenedErr != Results.OpenStashError.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to open the stash. Error: {0}", isOpenedErr);
                    return false;
                }
            }

            if (string.IsNullOrEmpty(stashTabName))
            {
                var isSwitchedFtErr = GoToFirstTab();
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
        /// <param name="guild">Whether it's the guild stash or not</param>
        /// <returns>true if the tab was changed and false otherwise.</returns>
        public static async Task<bool> WaitForStashTabChange(int lastId = -1, int timeout = 10000, bool guild = false)
        {
            var sw = Stopwatch.StartNew();
            var invTab = guild ? LokiPoe.InGameState.GuildStashUi.StashTabInfo : StashUI.StashTabInfo;
            while (invTab == null || invTab.InventoryId == lastId)
            {
                await Coroutine.Sleep(1);
                invTab = guild ? LokiPoe.InGameState.GuildStashUi.StashTabInfo : StashUI.StashTabInfo;
                if (sw.ElapsedMilliseconds > timeout)
                    return false;
            }

            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();
            return true;
        }

        /// <summary>
        /// Returns the corresponding stash, depending on the parameter passed
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static NetworkObject DetermineStash(bool guild = false)
        {
            var stash = LokiPoe.ObjectManager.Stash;
            if (guild)
                stash = LokiPoe.ObjectManager.GuildStash;

            return stash;
        }
    }
}
