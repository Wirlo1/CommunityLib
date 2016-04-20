using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot.Logic.Bots.OldGrindBot;
using Loki.Game;

namespace CommunityLib
{
    public class Dialog
    {

        /// <summary>
        /// Opens the NPC buy Panel.
        /// </summary>
        /// <param name="npcName"> If the parameter is null or empty, default NPC name will be used</param>
        /// <returns></returns>
        public static async Task<bool> OpenNpcBuyPanel( string npcName = "" )
        {
            if ( string.IsNullOrEmpty(npcName) )
                npcName = Actor.TownNpcName;

            if (npcName == "")
            {
                CommunityLib.Log.ErrorFormat("[OpenNpcBuyPanel] TownNpcName returned an empty string.");
                return false;
            }

            if (!await TalkToNpc(npcName))
                return false;

            var isBuyDialogOpen = LokiPoe.InGameState.NpcDialogUi.PurchaseItems();
            await Coroutines.WaitForPurchasePanel();

            if (isBuyDialogOpen != LokiPoe.InGameState.ConverseResult.None)
            {
                CommunityLib.Log.ErrorFormat("[OpenNpcBuyPanel] Fail open buy dialog. Error: {0}", isBuyDialogOpen);
                return false;
            }

            CommunityLib.Log.DebugFormat("[OpenNpcBuyPanel] {0}'s Buy Panel opened successfully", npcName);

            return true;
        }

        /// <summary>
        /// Opens the NPC buy Panel.
        /// </summary>
        /// <param name="npcName"> If the parameter is null or empty, default NPC name will be used</param>
        /// <returns></returns>
        public static async Task<bool> OpenNpcSellPanel( string npcName = "" )
        {
            if (string.IsNullOrEmpty(npcName))
                npcName = Actor.TownNpcName;

            if (npcName == "")
            {
                CommunityLib.Log.ErrorFormat("[{0}] TownNpcName returned an empty string.", "OpenNpcSellPanel");
                return false;
            }

            if (!await TalkToNpc(npcName))
                return false;

            var isSellDialogOpen = LokiPoe.InGameState.NpcDialogUi.SellItems();
            await Coroutines.WaitForSellPanel();

            if (isSellDialogOpen != LokiPoe.InGameState.ConverseResult.None)
            {
                CommunityLib.Log.ErrorFormat("[{0}] Fail open sell dialog. Error: {1}", "OpenNpcSellPanel", isSellDialogOpen);
                return false;
            }

            CommunityLib.Log.DebugFormat("[{0}] {1}'s Sell Panel opened successfully", "OpenNpcSellPanel", npcName);

            return true;
        }

        /// <summary>
        /// This task (awaitable) handles the whole process to talk to a NPC to reach the list of choices
        /// </summary>
        /// <param name="npcName">Name of the NPC to interact with</param>
        /// <returns>boolean</returns>
        public static async Task<bool> TalkToNpc(string npcName)
        {
            await Coroutines.CloseBlockingWindows();

            await Coroutines.TalkToNpc(npcName);

            // Clicking continue if NPC is blablaing (xD)
            while (LokiPoe.InGameState.NpcDialogUi.DialogDepth == 2)
            {
                await Coroutines.LatencyWait(5);
                await Coroutines.ReactionWait();
                LokiPoe.InGameState.NpcDialogUi.Continue();
            }

            // Wait for the window to appear
            var ret = await Coroutines.WaitForNpcDialogPanel();
            await Coroutine.Sleep(500);

            return ret;
        }


    }
}
