using System.Collections.Generic;
using System.Linq;
using Loki.Common;
using Loki.Game;

namespace CommunityLib
{
    public static class Chat
    {
        private static List<LokiPoe.InGameState.ChatPanel.ChatEntry> _oldMessages = new IndexedList<LokiPoe.InGameState.ChatPanel.ChatEntry>();

        /// <summary>
        /// Type a message in the chat ui.
        /// </summary>
        /// <param name="msg">the message to be typed</param>
        /// <param name="closeChatUi">Close chat after sending (default : true)</param>
        /// <returns>ChatResult</returns>
        public static LokiPoe.InGameState.ChatResult SendChatMsg(string msg, bool closeChatUi = true)
        {
            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
                LokiPoe.InGameState.ChatPanel.ToggleChat();

            if (!LokiPoe.InGameState.ChatPanel.IsOpened) return LokiPoe.InGameState.ChatResult.UiNotOpen;

            var result = LokiPoe.InGameState.ChatPanel.Chat(msg);

            if (closeChatUi)
            {
                if (LokiPoe.InGameState.ChatPanel.IsOpened)
                    LokiPoe.InGameState.ChatPanel.ToggleChat();
            }

            return result;
        }

        /// <summary>
        /// Return a list of message recieved since the last time it was caled.
        /// </summary>
        /// <returns> List of ChatEntry </returns>
        public static List<LokiPoe.InGameState.ChatPanel.ChatEntry> GetNewChatMessages()
        {
            var tempMsg = LokiPoe.InGameState.ChatPanel.Messages;
            var result = new IndexedList<LokiPoe.InGameState.ChatPanel.ChatEntry>();

            result.AddRange(tempMsg.Where(msg => !_oldMessages.Contains(msg)));

            _oldMessages = tempMsg;

            return result;
        }
    }
}
