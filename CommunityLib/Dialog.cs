using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot.Logic.Bots.OldGrindBot;
using Loki.Game;

namespace CommunityLib
{
    public class Dialog
    {
        /// <summary>
        /// This task (awaitable) handles the whole process to talk to a NPC to reach the list of choices
        /// </summary>
        /// <param name="npcName">Name of the NPC to interact with</param>
        /// <returns>boolean</returns>
        public static async Task<bool> TalkToNpc(string npcName)
        {
            await Coroutines.CloseBlockingWindows();

            var isInteracted = await Coroutines.TalkToNpc(npcName);

            // Clicking continue if NPC is blablaing (xD)
            while (LokiPoe.InGameState.NpcDialogUi.DialogDepth == 2)
            {
                await Coroutines.LatencyWait(5);
                await Coroutines.ReactionWait();
                LokiPoe.InGameState.NpcDialogUi.Continue();
            }

            // Wait for the window to appear
            await Coroutines.WaitForNpcDialogPanel();

            switch (isInteracted)
            {
                case Coroutines.TalkToNpcError.InteractFailed:
                    CommunityLib.Log.ErrorFormat("[CommunityLib][TalkToNpc] Interacting with {0} failed.", npcName);
                    return false;
                case Coroutines.TalkToNpcError.NpcDialogPanelDidNotOpen:
                    CommunityLib.Log.ErrorFormat("[CommunityLib][TalkToNpc] {0} was interacted, but dialog window is not open.", npcName);
                    return false;
                case Coroutines.TalkToNpcError.CouldNotMoveToNpc:
                    CommunityLib.Log.ErrorFormat("[CommunityLib][TalkToNpc] Cannot reach {0}.", npcName);
                    return false;
            }

            await Coroutine.Sleep(500);

            if (!LokiPoe.InGameState.NpcDialogUi.IsOpened)
            {
                CommunityLib.Log.ErrorFormat("[CommunityLib][TalkToNpc] {0} was interacted, but dialog window is not open.", npcName);
                return false;
            }

            return true;
        }
    }
}
