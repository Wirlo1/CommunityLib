using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Common;
using Loki.Game;
using Loki.Game.GameData;

namespace CommunityLib
{
    public static class Navigation
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

        /// <summary>
        /// The function will use chat to move to hideout
        /// </summary>
        /// <param name="retries">Number of max fastmove attempts. One is 8-16s timeout</param>
        /// <returns></returns>
        public static async Task<Results.FastGoToHideoutResult> FastGoToHideout( int retries = 3)
        {
            // No need to proceed if we are already there.
            if (LokiPoe.Me.IsInHideout)
                return Results.FastGoToHideoutResult.None;

            //I need to be in town to go to hideout using this method
            if (!LokiPoe.Me.IsInTown)
                return Results.FastGoToHideoutResult.NotInTown;

            LokiPoe.InGameState.ChatPanel.Commands.hideout();
            //Chat.SendChatMsg("/hideout", false);
            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();

            // The idea is to have a maximum of tries, but we don't want to spam them.
            // A Timer is started to "cool-off" the tries and a random lapse is calculated between each checks
            var nextTimer = Stopwatch.StartNew();
            var nextTry = LokiPoe.Random.Next(8000, 16000);
            int nextTryTries = 0;

            while (nextTryTries < retries)
            {
                if (LokiPoe.Me.IsInHideout)
                {
                    await Coroutines.LatencyWait();
                    await Coroutines.ReactionWait();
                    return Results.FastGoToHideoutResult.None;
                }

                //Thanks pushedx for the function. I think I don't need it but I'll put a comment here so you'll feel better Kappa
                //LokiPoe.InGameState.IsEnteringAreaTextShown

                //User have no hideout
                var noHideoutMessage = Chat.GetNewChatMessages().Any(d => d.Message.Contains(Dat.LookupBackendError(BackendErrorEnum.NoHideout).Text));
                if (noHideoutMessage)
                  return Results.FastGoToHideoutResult.NoHideout;

                // If it exists, and the timer has reached the random lapse we calculated above,
                if (nextTimer.ElapsedMilliseconds > nextTry)
                {
                    CommunityLib.Log.DebugFormat("[CommunityLib][FastGoToHideout] Attempt to fastmove ({0}/{1})", nextTryTries, retries);
                    if (LokiPoe.IsInGame)
                        LokiPoe.InGameState.ChatPanel.Commands.hideout();

                    await Coroutines.LatencyWait();
                    await Coroutines.ReactionWait();
                    nextTry = LokiPoe.Random.Next(8000, 16000);
                    nextTimer.Restart();
                    nextTryTries++;
                }

                //No need to go that fast
                await Coroutine.Sleep(200);
            }

            CommunityLib.Log.ErrorFormat("[CommunityLib][FastGoToHideout] Operation failed after {0} tries", retries);
            return Results.FastGoToHideoutResult.TimeOut;
        }

        /// <summary>
        /// Goes to the specified area using waypoint.
        /// </summary>
        /// <param name="name">Name of the area eg "Highgate"</param>
        /// <param name="difficulty"></param>
        /// <param name="newInstance">Do you want to open new instance?</param>
        /// <returns></returns>
        public static async Task<LokiPoe.InGameState.TakeWaypointResult> TakeWaypoint( string name, Difficulty difficulty = Difficulty.Unknown, bool newInstance = false )
        {
            //We are already there
            if (LokiPoe.LocalData.WorldArea.Name == name && LokiPoe.LocalData.WorldArea.Difficulty == difficulty)
                return LokiPoe.InGameState.TakeWaypointResult.None;

            //await Coroutines.CloseBlockingWindows();

            // First try of fastgotohideout instead of LokiPoe.InGameState.WorldUi.GoToHideout()
            if (name.Equals("Hideout", StringComparison.OrdinalIgnoreCase))
            {
                await Coroutines.CloseBlockingWindows();
                var res = await FastGoToHideout();

                switch (res)
                {
                    case Results.FastGoToHideoutResult.None:
                        return LokiPoe.InGameState.TakeWaypointResult.None;
                    case Results.FastGoToHideoutResult.NoHideout:
                        return LokiPoe.InGameState.TakeWaypointResult.AreaNotFound;
                    case Results.FastGoToHideoutResult.NotInGame:
                        return LokiPoe.InGameState.TakeWaypointResult.UiNotOpen;
                    //if we timed out then try to use default method like below
                }
            }

            if (!LokiPoe.InGameState.WorldUi.IsOpened)
            {
                var opened = await LibCoroutines.OpenWaypoint();
                if (opened != Results.OpenWaypointError.None)
                {
                    CommunityLib.Log.ErrorFormat("[TakeWaypoint] Fail to open waypoint. Error: \"{0}\".", opened);
                    return LokiPoe.InGameState.TakeWaypointResult.WaypointControlNotVisible;
                }
            }
            if (difficulty == Difficulty.Unknown) difficulty = LokiPoe.CurrentWorldArea.Difficulty;

            //var areaId = name == "Hideout" ? "" : GetZoneId(difficulty.ToString(), name);
            CommunityLib.Log.InfoFormat($"[TakeWaypoint] Going to {name} at {difficulty}.");

            var areaHash = LokiPoe.LocalData.AreaHash;
            var taken = name.Equals("Hideout", StringComparison.OrdinalIgnoreCase) 
                ? LokiPoe.InGameState.WorldUi.GoToHideout()
                : LokiPoe.InGameState.WorldUi.TakeWaypoint(LokiPoe.GetZoneId(difficulty.ToString(), name), newInstance, Int32.MaxValue);

            if (taken != LokiPoe.InGameState.TakeWaypointResult.None)
            {
                CommunityLib.Log.ErrorFormat("[TakeWaypoint] Failed to take waypoint to \"{0}\". Error: \"{1}\".", name, taken);
                return taken;
            }

            var awaited = await Areas.WaitForAreaChange(areaHash);
            return awaited ? LokiPoe.InGameState.TakeWaypointResult.None : LokiPoe.InGameState.TakeWaypointResult.CouldNotJoinNewInstance;
        }

        //public static string GetZoneId(string difficulty, string zoneName)
        //{
        //    StringBuilder stringBuilder = new StringBuilder();
        //    string str1 = difficulty.ToLowerInvariant();
        //    if (str1 == "normal")
        //        stringBuilder.Append("1_");
        //    else if (str1 == "cruel")
        //        stringBuilder.Append("2_");
        //    else
        //        stringBuilder.Append("3_");
             

        //    if (zoneName == "Lioneye's Watch")
        //    {
        //        stringBuilder.Append("1_town");
        //        return stringBuilder.ToString();
        //    }
        //    if (zoneName == "The Forest Encampment")
        //    {
        //        stringBuilder.Append("2_town");
        //        return stringBuilder.ToString();
        //    }
        //    if (zoneName == "The Sarn Encampment")
        //    {
        //        stringBuilder.Append("3_town");
        //        return stringBuilder.ToString();
        //    }
        //    if (zoneName == "Highgate")
        //    {
        //        stringBuilder.Append("4_town");
        //        return stringBuilder.ToString();
        //    }

        //    return LokiPoe.GetZoneId(difficulty, zoneName);
        //}
    }
}
