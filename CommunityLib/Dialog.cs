using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Game;

namespace CommunityLib
{
    public class Dialog
    {
        public enum PanelType
        {
            Purchase,
            Sell,
            NpcDialog,
            Stash,
            GuildStash
        }

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
            await WaitForPanel(PanelType.Purchase);

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
            await WaitForPanel(PanelType.Sell);

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
            await LibCoroutines.TalkToNpc(npcName);

            // Clicking continue if NPC is blablaing (xD)
            while (LokiPoe.InGameState.NpcDialogUi.DialogDepth == 2)
            {
                await Coroutines.LatencyWait(5);
                await Coroutines.ReactionWait();
                LokiPoe.InGameState.NpcDialogUi.Continue();
            }

            // Wait for the window to appear
            var ret = await WaitForPanel(PanelType.NpcDialog);
            await Coroutine.Sleep(500);

            return ret;
        }

        /// <summary>
        /// This coroutine waits for the panel to open after buy interaction.
        /// </summary>
        /// <param name="p">Panel type, part of PanelType enum</param>
        /// <param name="timeout">How long to wait before the coroutine fails.</param>
        /// <returns>true on succes and false on failure.</returns>
        public static async Task<bool> WaitForPanel(PanelType p, int timeout = 10000)
        {
            CommunityLib.Log.DebugFormat($"[WaitFor{p}Panel]");

            var sw = Stopwatch.StartNew();
            Func<bool> condition = null;

            switch (p)
            {
                    case PanelType.Sell:
                        condition = () => LokiPoe.InGameState.SellUi.IsOpened;
                        break;
                    case PanelType.Purchase:
                        condition = () => LokiPoe.InGameState.PurchaseUi.IsOpened;
                        break;
                    case PanelType.NpcDialog:
                        condition = () => LokiPoe.InGameState.NpcDialogUi.IsOpened;
                        break;
                    case PanelType.Stash:
                        condition = () => LokiPoe.InGameState.StashUi.IsOpened;
                        break;
                    case PanelType.GuildStash:
                        condition = () => LokiPoe.InGameState.GuildStashUi.IsOpened;
                        break;
            }

            // Can't be null yo!
            // ReSharper disable once PossibleNullReferenceException
            while (!condition.Invoke())
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat($"[WaitFor{p}Panel] Timeout.");
                    return false;
                }

                CommunityLib.Log.DebugFormat($"[WaitFor{p}Panel] We have been waiting {sw.Elapsed} for the panel to open.");
                await Coroutines.ReactionWait();
            }

            return true;
        }
    }
}
