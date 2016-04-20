using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot.Logic.Bots.OldGrindBot;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;
using Loki.Game.Objects.Items;

namespace CommunityLib
{
    public static class Inventory
    {
        /// <summary>
        /// Generic FastMove using new Inv Wrapper
        /// The inventory you refer is theinventory that will be used for moving the item from
        /// </summary>
        /// <param name="inv">This is the location where the item is picked up (can be stash or whatever you want)</param>
        /// <param name="id">This is the item localid</param>
        /// <param name="retries">Number of max fastmove attempts</param>
        /// <returns>FastMoveResult enum entry</returns>
        public static async Task<bool> FastMove(InventoryControlWrapper inv, int id, int retries = 3)
        {
            // If the inventory is null for reasons, throw ana application-level error
            if (inv == null)
                throw new ArgumentNullException(nameof(inv));

            // Here the idea is to make a first fastmove attempt to get an error
            // If the error is different of None, return the error
            var err = inv.FastMove(id);
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
                    inv.FastMove(id);
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
        /// This functions is meant to use an item on another one, for identification, chancing, anything you can think about
        /// It also supports the +X% quality using stones/scraps
        /// </summary>
        /// <param name="sourceWrapper">The source (inventory) that's holding the item meant to be used</param>
        /// <param name="sourceItem">The item meant to be used</param>
        /// <param name="destinationWrapper">The source (inventory) holding the item meant to be altered</param>
        /// <param name="destinationItem">The item menant to be altered</param>
        /// <param name="delegateToStop">
        /// The delegate is a lambda function it can be null, or used to provide a criteria to stop using.
        /// The function itself has a maximum count handling (if there's no errors during the process) you can use as the 2nd parameter of the delegate
        /// </param>
        /// <returns>ApplyCursorResult enum entry</returns>
        public static async Task<ApplyCursorResult> UseItemOnItem(InventoryControlWrapper sourceWrapper, Item sourceItem, InventoryControlWrapper destinationWrapper, Item destinationItem, Func<Item, int, bool> delegateToStop = null)
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
                onCursor = sourceWrapper.UseItem();

            // If something else than None is returned, the item can't be put on cursor properly
            if (onCursor != UseItemResult.None)
                return ApplyCursorResult.ItemNotFound;

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
                if (delegateToStop == null)
                    break;

                // We increment usecount to make it usable in delegate
                useCount++;

                // Refresh item to test the delegate (or condition)
                destinationItem = destinationWrapper.Inventory.GetItemAtLocation(itemLocation.X, itemLocation.Y);
                if (delegateToStop.Invoke(destinationItem, useCount))
                    break;
            }

            // End up the item application
            var err2 = InventoryControlWrapper.EndApplyCursor();
            // IF an error is returned, let caller know
            if (err2 != ApplyCursorResult.None)
                return ApplyCursorResult.ProcessHookManagerNotEnabled;

            return err;
        }

        public static async Task<bool> SplitAndPlaceItemInMainInventory(InventoryControlWrapper wrapper, Item item, int pickupAmount)
        {
            CommunityLib.Log.DebugFormat("[SplitAndPlaceItemInMainInventory] Spliting up stacks. Getting {0} items.", pickupAmount);
            var error = wrapper.SplitStack(item.LocalId, pickupAmount);
            //We assume it's currency stash tab, do not use LocalId with it
            if (error == SplitStackResult.Unsupported)
                error = wrapper.SplitStack(pickupAmount);

            if (error != SplitStackResult.None)
            {
                CommunityLib.Log.ErrorFormat("[SplitAndPlaceItemInMainInventory] Failed to split failed. Split Error: {0}", error);
                return false;
            }

            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();

            await Inputs.ClearCursorTask();

            return true;
        }
    }
}
