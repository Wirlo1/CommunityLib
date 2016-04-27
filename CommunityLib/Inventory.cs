using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;
using Loki.Game.Objects.Items;

namespace CommunityLib
{
    public static class Inventory
    {
        /// <summary>
        /// Finds the item in inventory or in Stash
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, CachedItemObject>> SearchForItem(string itemName)
        {
            //Open Inventory panel
            return await SearchForItem(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// Finds the item in inventory or in Stash
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, CachedItemObject>> SearchForItem(CommunityLib.FindItemDelegate condition)
        {
            //Open Inventory panel
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                await LibCoroutines.OpenInventoryPanel();
                await Coroutines.ReactionWait();
            }

            var item = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.Items.FirstOrDefault(d => condition(d));
            if (item != null)
                return new Tuple<Results.FindItemInTabResult, CachedItemObject>
                    (
                    Results.FindItemInTabResult.None,
                    new CachedItemObject(LokiPoe.InGameState.InventoryUi.InventoryControl_Main, item)
                    );

            //Now let's look in Stash
            return await Stash.FindTabContainingItem(condition);
        }

        /// <summary>
        /// Overload for FindItem to find a single item by name (Main inventory only !)
        /// </summary>
        /// <param name="itemName">Name of the item to find</param>
        /// <returns>Item</returns>
        public static async Task<Item> FindItem(string itemName)
        {
            //Open Inventory panel
            return await FindItem(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// Function that returns a specific item matching condition (Main inventory only !)
        /// </summary>
        /// <param name="condition">condition to pass item through</param>
        /// <returns>Item</returns>
        public static async Task<Item> FindItem(CommunityLib.FindItemDelegate condition)
        {
            //Open Inventory panel
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                await LibCoroutines.OpenInventoryPanel();
                await Coroutines.ReactionWait();
            }

            return LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.Items.FirstOrDefault(d => condition(d));
        }

        /// <summary>
        /// Overload of FindItems to find all the items matching this name (Main inventory only !)
        /// </summary>
        /// <param name="itemName">Name of the items to find</param>
        /// <returns>A list of item(s)</returns>
        public static async Task<List<Item>> FindItems(string itemName)
        {
            //Open Inventory panel
            return await FindItems(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// Functions that returns a list of items matching condition (Main inventory only !)
        /// </summary>
        /// <param name="condition">condition to pass item through</param>
        /// <returns>A list of item(s)</returns>
        public static async Task<List<Item>> FindItems(CommunityLib.FindItemDelegate condition)
        {
            //Open Inventory panel
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                await LibCoroutines.OpenInventoryPanel();
                await Coroutines.ReactionWait();
            }

            return LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.Items.Where(d => condition(d)).ToList();
        }

        /// <summary>
        /// Generic FastMove using new Inv Wrapper
        /// The inventory you refer is theinventory that will be used for moving the item from
        /// </summary>
        /// <param name="inv">This is the location where the item is picked up (can be stash or whatever you want)</param>
        /// <param name="id">This is the item localid</param>
        /// <param name="retries">Number of max fastmove attempts</param>
        /// <param name="breakFunc">If specified condition return true, FastMove will canceled and false will be returned</param>
        /// <returns>FastMoveResult enum entry</returns>
        public static async Task<bool> FastMove(InventoryControlWrapper inv, int id, int retries = 3, Func<bool> breakFunc = null )
        {
            // If the inventory is null for reasons, throw ana application-level error
            if (inv == null)
                throw new ArgumentNullException(nameof(inv));

            // Here the idea is to make a first fastmove attempt to get an error
            // If the error is different of None, return the error
            var err = inv.FastMove(id);
            //We assume it's currency stash tab, do not use LocalId with it
            if (err == FastMoveResult.Unsupported)
                err = inv.FastMove();

            if (err != FastMoveResult.None)
            {
                CommunityLib.Log.ErrorFormat("[CommunityLib][FastMove] FastMove has returned an error : {0}", err);
                return false;
            }

            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();

            // The idea is to have a maximum of tries, but we don't want to spam them.
            // A Timer is started to "cool-off" the tries and a random lapse is calculated between each checks
            var nextfastmovetimer = Stopwatch.StartNew();
            var nextFastMove = LokiPoe.Random.Next(2500, 4000);
            int nextFastMoveTries = 0;
            while (nextFastMoveTries < retries)
            {
                if (breakFunc != null)
                    if (breakFunc())
                        return false;

                // Verifying if the item exists in the source inventory
                // If not, the item has been moved return true
                var itemExists = inv.Inventory.GetItemById(id);
                if (itemExists == null)
                {
                    await Coroutines.ReactionWait();
                    return true;
                }

                // If it exists, and the timer has reached the random lapse we calculated above,
                // Attempt to make a new move
                if (nextfastmovetimer.ElapsedMilliseconds > nextFastMove)
                {
                    CommunityLib.Log.DebugFormat("[CommunityLib][FastMove] Attempt to fastmove ({0}/{1})", nextFastMoveTries, retries);
                    var error = inv.FastMove(id);
                    if (error == FastMoveResult.Unsupported)
                        inv.FastMove();

                    await Coroutines.LatencyWait();
                    await Coroutines.ReactionWait();
                    nextFastMove = LokiPoe.Random.Next(2500, 4000);
                    nextfastmovetimer.Restart();
                    nextFastMoveTries++;
                }

                await Coroutine.Sleep(20);
            }

            // It failed after the number of tries referenced, just return false.
            CommunityLib.Log.ErrorFormat("[CommunityLib][FastMove] Operation failed after {0} tries", retries);
            return false;
        }

        /// <summary>
        /// Returns an inventory wrapper based on the inventory slot referenced
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static InventoryControlWrapper GetWrapperBySlot(InventorySlot type)
        {
            return LokiPoe.InGameState.InventoryUi.AllInventoryControls.FirstOrDefault(ic => ic.Inventory.PageSlot == type);
        }

        /// <summary>
        /// This function returns an item depending on the slot referenced
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Item GetItem(InventorySlot type)
        {
            return LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(type)?.Items.FirstOrDefault();
        }

        /// <summary>
        /// Returning item info on the flask in corresponding slot
        /// </summary>
        /// <param name="slot">effective slot (1-5)</param>
        /// <returns></returns>
        public static Flask GetFlask(int slot)
        {
            return GetFlask<Flask>(slot);
        }

        /// <summary>
        /// Returning item info on the flask in corresponding slot
        /// </summary>
        /// <param name="slot">effective slot (1-5)</param>
        /// <returns></returns>
        public static T GetFlask<T>(int slot) where T : Item
        {
            return LokiPoe.InGameState.QuickFlaskHud.InventoryControl.Inventory.Items.FirstOrDefault(i => i.LocationTopLeft.X == slot - 1) as T;
        }

        /// <summary>
        /// This delegate is used in UseItemOnItem to stop execution if condition is met
        /// </summary>
        /// <param name="i">Item to use in delegate</param>
        /// <param name="useCount">The number of times an item is used</param>
        /// <returns>true if the condition is met</returns>
        public delegate bool StopUsingDelegate(Item i, int useCount);

        /// <summary>
        /// This functions is meant to use an item on another one, for identification, chancing, anything you can think about
        /// It also supports the +X% quality using stones/scraps
        /// </summary>
        /// <param name="sourceWrapper">The source (inventory) that's holding the item meant to be used</param>
        /// <param name="sourceItem">The item meant to be used</param>
        /// <param name="destinationWrapper">The source (inventory) holding the item meant to be altered</param>
        /// <param name="destinationItem">The item menant to be altered</param>
        /// <param name="d">Delegate/Condition to stop using item</param>
        /// <returns>ApplyCursorResult enum entry</returns>
        public static async Task<ApplyCursorResult> UseItemOnItem(InventoryControlWrapper sourceWrapper, Item sourceItem, InventoryControlWrapper destinationWrapper, Item destinationItem, StopUsingDelegate d = null)
        {
            // If Any of these args are null, throw an application-level exception
            if (sourceWrapper == null)
                throw new ArgumentNullException(nameof(sourceWrapper));
            if (sourceItem == null)
                throw new ArgumentNullException(nameof(sourceItem));
            if (destinationWrapper == null)
                throw new ArgumentNullException(nameof(destinationWrapper));
            if (destinationItem == null)
                throw new ArgumentNullException(nameof(destinationItem));

            var onCursor = sourceWrapper.UseItem(sourceItem.LocalId);

            // We assume it's currency stash tab, do not use LocalId with it
            if (onCursor == UseItemResult.Unsupported)
            {
                CommunityLib.Log.DebugFormat($"[CommunityLib] Failed to use item on item. Unsupported");
                onCursor = sourceWrapper.UseItem();

            }

            // If something else than None is returned, the item can't be put on cursor properly
            if (onCursor != UseItemResult.None)
            {
                CommunityLib.Log.ErrorFormat($"[CommunityLib] Failed to use item on item. OnCursor: {onCursor}. Returning item not found");
                return ApplyCursorResult.ItemNotFound;
            }

            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();

            // First, we put the item on cursor to start applying
            var err = InventoryControlWrapper.BeginApplyCursor(true);
            if (err != ApplyCursorResult.None)
                return ApplyCursorResult.ProcessHookManagerNotEnabled;

            // We store the destination item's location to make sure it has been applied or the delegate (lower in the code) is valid
            var itemLocation = destinationItem.LocationTopLeft;
            int useCount = 0;

            while (true)
            {
                // Apply item on cursor to the destination item
                err = destinationWrapper.ApplyCursorTo(destinationItem.LocalId);
                // If the error is different of None, break the execution and return the error
                if (err != ApplyCursorResult.None)
                    break;

                await Coroutines.LatencyWait();
                await Coroutines.ReactionWait();

                // If the delegate is null, that means our processing is done, break the loop to return None
                if (d == null)
                    break;

                // We increment usecount to make it usable in delegate
                useCount++;

                // Refresh item to test the delegate (or condition)
                destinationItem = destinationWrapper.Inventory.GetItemAtLocation(itemLocation.X, itemLocation.Y);
                if (d.Invoke(destinationItem, useCount))
                    break;
            }

            // End up the item application
            var err2 = InventoryControlWrapper.EndApplyCursor();
            await Coroutines.FinishCurrentAction();
            await Coroutines.ReactionWait();

            // IF an error is returned, let caller know
            if (err2 != ApplyCursorResult.None)
                return ApplyCursorResult.ProcessHookManagerNotEnabled;

            if (err != ApplyCursorResult.None)
                CommunityLib.Log.ErrorFormat($"[CommunityLib] Failed to use item on item. Error: {err}");
            return err;
        }

        public static async Task<bool> SplitAndPlaceItemInMainInventory(InventoryControlWrapper wrapper, Item item, int pickupAmount)
        {
            CommunityLib.Log.DebugFormat("[SplitAndPlaceItemInMainInventory] Spliting up stacks. Getting {0} {1}. Count in stack: {2}", pickupAmount, item.FullName, item.StackCount);

            if (pickupAmount >= item.StackCount)
                return await FastMove(wrapper, item.LocalId);

            var error = wrapper.SplitStack(item.LocalId, pickupAmount);
            //We assume it's currency stash tab, do not use LocalId with it
            if (error == SplitStackResult.Unsupported)
                error = wrapper.SplitStack(pickupAmount);

            if (error != SplitStackResult.None)
            {
                CommunityLib.Log.ErrorFormat("[SplitAndPlaceItemInMainInventory] Failed to split failed. Split Error: {0}", error);
                return false;
            }

            await Inputs.WaitForCursorToHaveItem();
            await Coroutines.ReactionWait();

            await Inputs.ClearCursorTask();

            return true;
        }
    }
}
