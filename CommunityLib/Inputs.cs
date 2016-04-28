using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Game;
using Loki.Game.GameData;

namespace CommunityLib
{
    /// <summary>
    /// 
    /// </summary>
    public class Inputs
    {
        public static async Task<Results.ClearCursorResults> ClearCursorTask(int maxTries = 3)
        {
            var cursMode = LokiPoe.InGameState.CursorItemOverlay.Mode;

            if (cursMode == LokiPoe.InGameState.CursorItemModes.None)
            {
                CommunityLib.Log.DebugFormat("[CommunityLib][ClearCursorTask] Nothing is on cursor, continue execution");
                return Results.ClearCursorResults.None;
            }

            if (cursMode == LokiPoe.InGameState.CursorItemModes.VirtualMove || cursMode == LokiPoe.InGameState.CursorItemModes.VirtualUse)
            {
                CommunityLib.Log.DebugFormat("[CommunityLib][ClearCursorTask] VirtualMode detected, pressing escape to clear");
                LokiPoe.Input.SimulateKeyEvent(Keys.Escape, true, false, false);
                return Results.ClearCursorResults.None;
            }

            var cursorhasitem = LokiPoe.InGameState.CursorItemOverlay.Item;
            // there is a item on the cursor let clear it
            int attempts = 0;
            while (cursorhasitem != null && attempts < maxTries)
            {
                if (attempts > maxTries)
                    return Results.ClearCursorResults.MaxTriesReached;

                if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                {
                    await LibCoroutines.OpenInventoryPanel();
                    await Coroutines.LatencyWait();
                    await Coroutines.ReactionWait();
                    if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                        return Results.ClearCursorResults.InventoryNotOpened;
                }

                int col, row;
                if (!LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.CanFitItem(cursorhasitem.Size, out col, out row))
                {
                    CommunityLib.Log.ErrorFormat("[CommunityLib][ClearCursorTask] Now stopping the bot because it cannot continue.");
                    BotManager.Stop();
                    return Results.ClearCursorResults.NoSpaceInInventory;
                }

                var res = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.PlaceCursorInto(col, row);
                if (res == PlaceCursorIntoResult.None)
                {
                    if (!await WaitForCursorToBeEmpty())
                        CommunityLib.Log.ErrorFormat("[CommunityLib][ClearCursorTask] WaitForCursorToBeEmpty failed.");

                    await Coroutines.ReactionWait();
                    return Results.ClearCursorResults.None;
                }

                CommunityLib.Log.DebugFormat("[CommunityLib][ClearCursorTask] Placing item into inventory failed, Err : {0}", res);
                switch (res)
                {
                    case PlaceCursorIntoResult.ItemWontFit:
                        return Results.ClearCursorResults.NoSpaceInInventory;
                    case PlaceCursorIntoResult.NoItemToMove:
                        return Results.ClearCursorResults.None;
                }

                await Coroutine.Sleep(3000);
                await Coroutines.LatencyWait();
                await Coroutines.ReactionWait();
                cursorhasitem = LokiPoe.InGameState.CursorItemOverlay.Item;
                attempts++;
            }

            return Results.ClearCursorResults.None;
        }

        public static async Task<bool> WaitForCursorToBeEmpty(int timeout = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot(InventorySlot.Cursor).Any())
            {
                CommunityLib.Log.InfoFormat("[CommunityLib][WaitForCursorToBeEmpty] Waiting for the cursor to be empty.");
                await Coroutines.LatencyWait();
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.InfoFormat("[CommunityLib][WaitForCursorToBeEmpty] Timeout while waiting for the cursor to become empty.");
                    return false;
                }
            }
            return true;
        }

        public static async Task<bool> WaitForCursorToHaveItem(int timeout = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (!LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot(InventorySlot.Cursor).Any())
            {
                CommunityLib.Log.InfoFormat("[CommunityLib][WaitForCursorToHaveItem] Waiting for the cursor to have an item.");
                await Coroutines.LatencyWait();
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.InfoFormat("[CommunityLib][WaitForCursorToHaveItem] Timeout while waiting for the cursor to contain an item.");
                    return false;
                }
            }
            return true;
        }
    }
}
