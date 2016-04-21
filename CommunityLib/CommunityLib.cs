using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Buddy.Coroutines;
using Exilebuddy;
using IronPython.Modules;
using Loki.Bot;
using Loki.Common;
using log4net;
using Loki;
using Loki.Game;
using Loki.Game.Objects;

namespace CommunityLib
{
    public class CommunityLib : IPlugin
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public delegate bool FindItemDelegate(Item item);


        #region Implementation of IRunnable

        private void RestartBot()
        {
            //var args = Environment.GetCommandLineArgs();
            var exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var workingDir = Path.GetDirectoryName(exe);

            var proc1 = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = exe,
                Arguments = LokiPoe.Memory.Process.StartInfo.Arguments,
                WindowStyle = ProcessWindowStyle.Normal
            };
            if (workingDir != null)
                proc1.WorkingDirectory = workingDir;

            Process.Start(proc1);
            Application.Current.Shutdown((int)ApplicationExitCodes.Restarting);
        }

        public void Start()
        {
            if (_needRestart)
            {
                Log.InfoFormat("[{0}] ------------------------------------------------------------------------------", Name);
                Log.InfoFormat("[{0}] ------------------------------------------------------------------------------", Name);
                Log.InfoFormat("[{0}] ------------------------------------------------------------------------------", Name);
                //Log.InfoFormat("[{0}] {1} has been successfuly installed. Please restart the bot completly.", Name, Name);
                Log.InfoFormat("[{0}] {1} has been successfuly installed. The bot will now restart itself.", Name, Name);
                Log.InfoFormat("[{0}] ------------------------------------------------------------------------------", Name);
                Log.InfoFormat("[{0}] ------------------------------------------------------------------------------", Name);
                Log.InfoFormat("[{0}] ------------------------------------------------------------------------------", Name);
                //BotManager.Stop(true);
                RestartBot();
            }
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

        //Install's and enables itself
        private bool _needRestart;
        private void Install()
        {
            //Adding ourselves at the ContentLoader, wee need to be loaded before normal plugins
            if (!GuiSettings.Instance.ContentOrder.Any(ent => ent.Name.Equals(Name)))
            {
                //Adding us on top
                GuiSettings.Instance.ContentOrder.Add( new StringEntry{Name = Name });
                _needRestart = true;
            }

            //Checking if we are in enabled
            if (!GuiSettings.Instance.EnabledPlugins.Any(ent => ent.Equals(Name)))
            {
                GuiSettings.Instance.EnabledPlugins.Add(Name);
                _needRestart = true;
            }
        }

        public void Initialize()
        {
            Install();
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
