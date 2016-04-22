using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Common;
using Loki.Game;

namespace CommunityLib
{
    public static class Movement
    {
        /// <summary>
        /// This coroutine moves towards a position until it is within the specified stop distance.
        /// </summary>
        /// <param name="position">The position to move ot.</param>
        /// <param name="stopDistance">How close to the location should we get.</param>
        /// <param name="timeout">How long should the coroutine execute for before stopping due to timeout.</param>
        /// <param name="stopCondition">delegate to stop moving</param>
        /// <returns></returns>
        public static async Task<bool> MoveToLocation(Vector2i position, int stopDistance, int timeout, Func<bool> stopCondition)
        {
            var sw = Stopwatch.StartNew();
            var dsw = Stopwatch.StartNew();

            var da = (bool)PlayerMover.Instance.Execute("GetDoAdjustments");
            PlayerMover.Instance.Execute("SetDoAdjustments", false);

            while (LokiPoe.MyPosition.Distance(position) > stopDistance)
            {
                if (LokiPoe.Me.IsDead)
                {
                    CommunityLib.Log.ErrorFormat("[MoveToLocation] The player is dead.");
                    PlayerMover.Instance.Execute("SetDoAdjustments", da);
                    return false;
                }

                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[MoveToLocation] Timeout.");
                    PlayerMover.Instance.Execute("SetDoAdjustments", da);
                    return false;
                }

                if (stopCondition())
                    break;

                if (dsw.ElapsedMilliseconds > 100)
                {
                    CommunityLib.Log.DebugFormat(
                        "[MoveToLocation] Now moving towards {0}. We have been performing this task for {1}.",
                        position,
                        sw.Elapsed);
                    dsw.Restart();
                }

                if (!PlayerMover.MoveTowards(position))
                    CommunityLib.Log.ErrorFormat("[MoveToLocation] MoveTowards failed for {0}.", position);

                await Coroutine.Yield();
            }

            PlayerMover.Instance.Execute("SetDoAdjustments", da);
            await Coroutines.FinishCurrentAction();

            return true;
        }
    }
}
