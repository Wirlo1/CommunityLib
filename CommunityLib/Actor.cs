using System;
using System.Linq;
using Loki.Bot;
using Loki.Bot.Pathfinding;
using Loki.Common;
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
                    var npcs = LokiPoe.ObjectManager.GetObjectsByType<Npc>()
                        .Where(npc => npc.IsTargetable)
                        .ToList();

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

        /// <summary>
        /// Returns a location where the stash should be if we're in a town.
        /// </summary>
        /// <returns>A location where the stash should come into view.</returns>
        public static Vector2i GuessStashLocation()
        {
            var curArea = LokiPoe.LocalData.WorldArea.Id.ToLowerInvariant();

            foreach (var stl in Data.StashesLocations)
            {
                if (!curArea.Contains(stl.Key)) continue;
                return stl.Value;
            }

            throw new Exception($"GuessStashLocation called when curArea = {curArea}");
        }

        /// <summary>
        /// Returns a location where the waypoint should be if we're in a town.
        /// </summary>
        /// <returns>A location where the waypoint should come into view.</returns>
        public static Vector2i GuessWaypointLocation()
        {
            var curArea = LokiPoe.LocalData.WorldArea.Id.ToLowerInvariant();

            foreach (var stl in Data.WaypointsLocations)
            {
                if (!curArea.Contains(stl.Key)) continue;
                return stl.Value;
            }


            throw new Exception($"GuessWaypointLocation called when curArea = {curArea}");
        }

        /// <summary>
        /// Returns hardcoded locations for npcs in a town. We need to make sure these don't change while we aren't looking!
        /// Ideally, we'd explore town to find the location if the npc object was not in view.
        /// </summary>
        public static Vector2i GuessNpcLocation(string npcName)
        {
            var curArea = LokiPoe.LocalData.WorldArea.Id.ToLowerInvariant();

            foreach (var npc in Data.TownNpcStaticLocations)
            {
                if (!curArea.Contains(npc.Item1)) continue;
                if (!npcName.Equals(npc.Item2)) continue;
                return npc.Item3;
            }

            return Vector2i.Zero;
        }

        /// <summary>
        /// Returns the number of mobs near a target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <param name="dead"></param>
        /// <returns></returns>
        public static int NumberOfMobsNear(NetworkObject target, float distance, bool dead = false)
        {
            var mpos = target.Position;
            var curCount = 0;

            foreach (var mob in LokiPoe.ObjectManager.Objects.OfType<Monster>())
            {
                if (mob.Id == target.Id)
                    continue;

                // If we're only checking for dead mobs... then... yeah...
                if (dead)
                {
                    if (!mob.IsDead)
                        continue;
                }
                else if (!mob.IsActive)
                {
                    continue;
                }

                if (mob.Position.Distance(mpos) < distance)
                    curCount++;
            }

            return curCount;
        }

        /// <summary>
        /// Returns the number of mobs near a target.
        /// </summary>
        /// <param name="mpos"></param>
        /// <param name="distance"></param>
        /// <param name="dead"></param>
        /// <returns></returns>
        public static int NumberOfMobsNear(Vector2i mpos, float distance, bool dead = false)
        {
            var curCount = 0;

            foreach (var mob in LokiPoe.ObjectManager.Objects.OfType<Monster>())
            {
                // If we're only checking for dead mobs... then... yeah...
                if (dead)
                {
                    if (!mob.IsDead)
                        continue;
                }
                else if (!mob.IsActive)
                {
                    continue;
                }

                if (mob.Position.Distance(mpos) < distance)
                    curCount++;
            }

            return curCount;
        }

        /// <summary>
        /// Returns the number of mobs between 2 points
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint"></param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns></returns>
        public static int NumberOfMobsBetween(NetworkObject start, NetworkObject end, int distanceFromPoint = 5, bool dontLeaveFrame = false)
        {
            return NumberOfMobsBetween(start.Position, end.Position, distanceFromPoint, dontLeaveFrame);
        }

        /// <summary>
        /// Returns the number of mobs between 2 points
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint"></param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns></returns>
        public static int NumberOfMobsBetween(Vector2i start, Vector2i end, int distanceFromPoint = 5, bool dontLeaveFrame = false)
        {
            var mobs = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(d => d.IsActive).ToList();
            if (!mobs.Any())
                return 0;

            var path = ExilePather.GetPointsOnSegment(start, end, dontLeaveFrame);

            var count = 0;
            for (var i = 0; i < path.Count; i += 10)
            {
                count += mobs.Count(mob => mob.Position.Distance(path[i]) <= distanceFromPoint);
            }

            return count;
        }
    }
}
