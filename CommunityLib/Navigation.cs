using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot.Logic.Bots.OldGrindBot;
using Loki.Game;
using Loki.Game.GameData;

namespace CommunityLib
{
    public static class Navigation
    {
        //?
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

            Chat.SendChatMsg("/hideout", false);
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
                var noHideoutMessage = Chat.GetNewChatMessages().Any( d => d.Message.Contains(Dat.LookupClientString(ClientStringsEnum.NoHideout).Value) );
                if (noHideoutMessage)
                    return Results.FastGoToHideoutResult.NoHideout;

                // If it exists, and the timer has reached the random lapse we calculated above,
                if (nextTimer.ElapsedMilliseconds > nextTry)
                {
                    CommunityLib.Log.DebugFormat("[CommunityLib][FastGoToHideout] Attempt to fastmove ({0}/{1})", nextTryTries, retries);
                    if (LokiPoe.IsInGame)
                        Chat.SendChatMsg("/hideout", false);

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
    }
}
