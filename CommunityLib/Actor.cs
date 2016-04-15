using System.Linq;
using Loki.Bot;
using Loki.Game;
using Loki.Game.Objects;

namespace CommunityLib
{
    public class Actor
    {
        /// <summary>
        /// This property returns an Npc's name depending on the area you're in.
        /// If you're in Hideout and no NPCs are found, bot will stop (after logging something)
        /// </summary>
        public static string TownNpcName
        {
            get
            {
                // If we're in town, just check for worldarea
                if (LokiPoe.Me.IsInTown)
                {
                    switch (LokiPoe.LocalData.WorldArea.Name)
                    {
                        case "Lioneye's Watch":
                            return "Nessa";
                        case "The Forest Encampment":
                            return "Yeena";
                        case "The Sarn Encampment":
                            return "Clarissa";
                        case "Highgate":
                            return "Petarus and Vanja";
                    }
                }

                // If in hideout, process NPCs
                if (LokiPoe.Me.IsInHideout)
                {
                    var npcs = LokiPoe.ObjectManager.GetObjectsByType<Npc>().ToList();

                    // If no NPCs are available in hideout, bot will stop for security
                    if (npcs.Count == 0)
                    {
                        CommunityLib.Log.ErrorFormat("[CommunityLib][TownNpcName] Error, no NPC Found in hideout, stopping bot");
                        BotManager.Stop();
                        return "";
                    }

                    // Return the closest NPC's name
                    return npcs.OrderBy(m => m.Distance).First().Name;
                }

                return "";
            }
        }
    }
}
