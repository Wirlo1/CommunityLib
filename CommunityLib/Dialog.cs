using System;
using System.Diagnostics;
using System.Threading.Tasks;
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
            GuildStash,
            DivinationCardsTrade
        }

        /// <summary>
        /// Opens the NPC buy Panel.
        /// </summary>
        /// <param name="npcName">If the parameter is null or empty, default NPC name will be used</param>
        /// <param name="maxTries">Number of attempts to open panel before returning false</param>
        /// <returns>boolean</returns>
        public static async Task<bool> OpenNpcBuyPanel(string npcName = "", int maxTries = 3)
        {
            if ( string.IsNullOrEmpty(npcName) )
                npcName = Actor.TownNpcName;

            if (npcName == "")
            {
                CommunityLib.Log.ErrorFormat("[CommunityLib][OpenNpcBuyPanel] TownNpcName returned an empty string.");
                return false;
            }

            int tries = 1;
            while (true)
            {
                if (tries > maxTries)
                {
                    CommunityLib.Log.ErrorFormat($"[CommunityLib][OpenNpcBuyPanel] Failed to open {npcName}'s BuyPanel after {tries} attempts, returning false");
                    return false;
                }

                if (!await TalkToNpc(npcName))
                {
                    CommunityLib.Log.ErrorFormat($"[CommunityLib][OpenNpcBuyPanel] Failed to talk to {npcName} (Attempt {tries}/{maxTries})");
                    tries++;
                    await Coroutines.LatencyWait();
                    continue;
                }

                var isBuyDialogOpen = LokiPoe.InGameState.NpcDialogUi.PurchaseItems();
                await WaitForPanel(PanelType.Purchase);

                if (isBuyDialogOpen != LokiPoe.InGameState.ConverseResult.None)
                {
                    CommunityLib.Log.ErrorFormat($"[CommunityLib][OpenNpcBuyPanel] Failed open buy dialog. Error: {isBuyDialogOpen} (Attempt {tries}/{maxTries})");
                    tries++;
                    await Coroutines.LatencyWait();
                    continue;
                }

                break;
            }

            CommunityLib.Log.DebugFormat($"[CommunityLib][OpenNpcBuyPanel] {npcName}'s Buy Panel opened successfully");
            return true;
        }

        /// <summary>
        /// Opens the NPC buy Panel.
        /// </summary>
        /// <param name="npcName"> If the parameter is null or empty, default NPC name will be used</param>
        /// <returns></returns>
        public static async Task<bool> OpenNpcSellPanel( string npcName = "", int maxTries = 3)
        {
            if (string.IsNullOrEmpty(npcName))
                npcName = Actor.TownNpcName;

            if (npcName == "")
            {
                CommunityLib.Log.ErrorFormat("[CommunityLib][OpenNpcSellPanel] TownNpcName returned an empty string.");
                return false;
            }

            int tries = 1;
            while (true)
            {
                if (tries > maxTries)
                {
                    CommunityLib.Log.ErrorFormat($"[CommunityLib][OpenNpcSellPanel] Failed to open {npcName}'s SellPanel after {tries} attempts, returning false");
                    return false;
                }

                if (!await TalkToNpc(npcName))
                {
                    CommunityLib.Log.ErrorFormat($"[CommunityLib][OpenNpcSellPanel] Failed to talk to {npcName} (Attempt {tries}/{maxTries})");
                    tries++;
                    await Coroutines.LatencyWait();
                    continue;
                }

                var isSellDialogOpen = LokiPoe.InGameState.NpcDialogUi.SellItems();
                await WaitForPanel(PanelType.Sell);

                if (isSellDialogOpen != LokiPoe.InGameState.ConverseResult.None)
                {
                    CommunityLib.Log.ErrorFormat($"[CommunityLib][OpenNpcSellPanel] Fail open sell dialog. Error: {isSellDialogOpen} (Attempt {tries}/{maxTries})");
                    tries++;
                    await Coroutines.LatencyWait();
                    continue;
                }

                break;
            }

            CommunityLib.Log.DebugFormat($"[CommunityLib][OpenNpcSellPanel] {npcName}'s Sell Panel opened successfully");
            return true;
        }

        /// <summary>
        /// This task (awaitable) handles the whole process to talk to a NPC to reach the list of choices
        /// </summary>
        /// <param name="npcName">Name of the NPC to interact with</param>
        /// <returns>boolean</returns>
        public static async Task<bool> TalkToNpc(string npcName)
        {
            //await Coroutines.CloseBlockingWindows();
            var ret = await LibCoroutines.TalkToNpc(npcName);

            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();

            return ret == Results.TalkToNpcError.None;
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
                    case PanelType.DivinationCardsTrade:
                        condition = () => LokiPoe.InGameState.CardTradeUi.IsOpened;
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
