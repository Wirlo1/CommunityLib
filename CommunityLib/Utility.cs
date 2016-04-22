using System.Linq;
using Loki.Bot.Pathfinding;
using Loki.Common;
using Loki.Game;
using Loki.Game.Objects;

namespace CommunityLib
{
    /// <summary>
    /// Shared utility functions.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(
            NetworkObject start,
            NetworkObject end,
            int distanceFromPoint = 10,
            int stride = 10,
            bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(
            NetworkObject start,
            Vector2i end,
            int distanceFromPoint = 10,
            int stride = 10,
            bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(
            Vector2i start,
            NetworkObject end,
            int distanceFromPoint = 10,
            int stride = 10,
            bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(
            Vector2i start,
            Vector2i end,
            int distanceFromPoint = 10,
            int stride = 10,
            bool dontLeaveFrame = false)
        {
            var doors = LokiPoe.ObjectManager.Doors.Where(d => !d.IsOpened).ToList();

            if (!doors.Any())
                return false;

            var path = ExilePather.GetPointsOnSegment(start, end, dontLeaveFrame);

            for (var i = 0; i < path.Count; i += stride)
            {
                if (doors.Any(door => door.Position.Distance(path[i]) <= distanceFromPoint))
                    return true;
            }

            return false;
        }
    }
}
