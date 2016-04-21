using System.Threading.Tasks;
using System.Windows.Forms;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Game;

namespace CommunityLib
{
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

                var res = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.PlaceCursorInto();
                if (res == PlaceCursorIntoResult.None)
                {
                    await Coroutines.LatencyWait();
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
    }
}
