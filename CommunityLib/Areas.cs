using System.Diagnostics;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Game;

namespace CommunityLib
{
    public static class Areas
    {
        /// <summary>
        /// This coroutines waits for the character to change areas.
        /// </summary>
        /// <param name="original">The starting area's hash.</param>
        /// <param name="timeout">How long to wait before the coroutine fails.</param>
        /// <returns>true on success, and false on failure.</returns>
        public static async Task<bool> WaitForAreaChange(uint original, int timeout = 30000)
        {
            CommunityLib.Log.DebugFormat("[WaitForAreaChange]");

            var sw = Stopwatch.StartNew();

            while (LokiPoe.LocalData.AreaHash == original)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[WaitForAreaChange] Timeout.");
                    return false;
                }

                CommunityLib.Log.DebugFormat("[WaitForAreaChange] We have been waiting {0} for an area change.", sw.Elapsed);
                await Coroutines.LatencyWait();
                await Coroutine.Sleep(1000);
            }

            return true;
        }
    }
}
