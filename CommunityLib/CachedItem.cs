using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loki.Common;
using Loki.Game;
using Loki.Game.Objects;

namespace CommunityLib
{
    /// <summary>
    /// When creating a class of an item in the Stash, make sure you'll set the tabName property!!!
    /// </summary>
    public class CachedItem
    {
        public InventoryControlWrapper Wrapper;
        public int ItemId;

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
        public bool IsDivinationCardType;

        /// <summary>
        /// If the item is in stash, make sure the correct stash tab is loaded or use GetItem method!
        /// </summary>
        public Item Item
        {
            get
            {
                if (Wrapper == null)
                    return null;

                //Premium stash tabs can contain only one item
                //It's "safe" to return it like that.
                if (Wrapper.HasCurrencyTabOverride)
                    return Wrapper.CurrencyTabItem;

                //Find the item by it's location first
                //Maybe it has changed, as a failproof find by Location aswell.
                var ret = Wrapper.Inventory.GetItemAtLocation(LocationInTab.X, LocationInTab.Y) ??
                          Wrapper.Inventory.GetItemById(ItemId);

                if (ret == null)
                    return null;

                //Make sure the item's name is equal to the one we should have
                //We assume it'll be the same as cached one.
                return ret.FullName.Equals(FullName) ? ret : null;
            }
        }

        public CachedItem(InventoryControlWrapper wrp, int it, string tabName = "")
        {
            Wrapper = wrp;
            ItemId = it;
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

            IsDivinationCardType = item.IsDivinationCardType;
        }

        /// <summary>
        /// Opens the stash and the stash tab of this item. 
        /// <para>If the item is in inventory then it opens the inventory</para>
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoTo()
        {
            if (!string.IsNullOrEmpty(TabName)) return await Stash.OpenStashTabTask(TabName);

            //TabName is empty, so it's inventory
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                return await LibCoroutines.OpenInventoryPanel();

            return true;
        }

        /// <summary>
        /// If the item is in stash, it's getting opened first and then returning the value.
        /// <para>if TabName is empty, it assumes the item is in inventory.</para>
        /// </summary>
        /// <returns></returns>
        public async Task<Item> GetItem()
        {
            if (!await GoTo())
                return null;

            return Item;
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
}
