using System.Threading.Tasks;
using System.Windows.Controls;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Common;
using log4net;

namespace CommunityLib
{
    public class CommunityLib : IPlugin
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        #region Implementation of IRunnable

        public void Start()
        {
            Log.DebugFormat("[{0}] Starting", Name);
        }

        public void Tick()
        {
            
        }

        public void Stop()
        {
            Log.DebugFormat("[{0}] Stopped", Name);
        }

        #endregion

        #region Implementation of IAuthored

        public string Name => "CommunityLib";
        public string Author => "Community ! ofc";
        public string Description => "Acts as a \"Database\" of generic functions you can use in any plugins";
        public string Version => "0.0.0.1";

        #endregion

        #region Implementation of IBase

        public void Initialize()
        {
            Log.DebugFormat("[{0}] Initialized", Name);
        }

        public void Deinitialize()
        {
            Log.DebugFormat("[{0}] Deinitialized", Name);
        }

        #endregion

        #region Implementation of IConfigurable

        public UserControl Control => null;
        public JsonSettings Settings => null;

        #endregion

        #region Implementation of IEnableable

        public void Enable()
        {
            Log.DebugFormat("[{0}] Enabled", Name);
        }

        public void Disable()
        {
            Log.DebugFormat("[{0}] Disabled", Name);
        }

        #endregion

        #region Implementation of ILogic

        public async Task<bool> Logic(string type, params dynamic[] param)
        {
            await Coroutine.Sleep(0);

            if (type == "core_area_changed_event")
                Reset();

            return false;
        }

        private void Reset()
        {
            Data.ItemsInStashAlreadyCached = false;
        }

        public object Execute(string name, params dynamic[] param)
        {
            return null;
        }

    #endregion
}
}
