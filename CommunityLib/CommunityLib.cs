using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Buddy.Coroutines;
using Exilebuddy;
using Loki.Bot;
using Loki.Common;
using log4net;
using Loki;
using Loki.Game.Objects;
using Application = System.Windows.Application;
using UserControl = System.Windows.Controls.UserControl;

namespace CommunityLib
{
    public class CommunityLib : IPlugin
    {
        private CommunityLibGUI _gui;

        #region Delegates

        /// <summary>
        /// Delegate used in FindItem(s) functions
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns>true if the condition is met</returns>
        public delegate bool FindItemDelegate(Item item);

        #endregion

        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        #region Implementation of IRunnable

        public void Start()
        {
            if (_needRestart)
            {
                MessageBox.Show(
                    $"{Name} self-install ran successfully and requires a bot restart, bot won't start until it's done",
                    $"{Name} installed successfully!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                BotManager.Stop();
            }

            AreaStateCache.Start();

            Log.DebugFormat("[{0}] Starting", Name);
        }

        public void Tick()
        {
            //AreaStateCache.Tick();
        }

        public void Stop()
        {
            Log.DebugFormat("[{0}] Stopped", Name);
            Data.ItemsInStashAlreadyCached = false;
            AreaStateCache.Stop();
        }

        #endregion

        #region Implementation of IAuthored

        public string Name => "CommunityLib";
        public string Author => "Community ! ofc";
        public string Description => "Acts as a \"Database\" of generic functions you can use in any plugins";
        public string Version => "0.0.0.6";

        #endregion

        #region Implementation of IBase

        //Install's and enables itself
        private bool _needRestart;
        private void Install()
        {
            //Adding ourselves at the ContentLoader, wee need to be loaded before normal plugins
            if (!GuiSettings.Instance.ContentOrder.Any(ent => ent.Name.Equals(Name)))
            {
                //Adding us on top (invoke req)
                Application.Current.Dispatcher.Invoke(delegate { GuiSettings.Instance.ContentOrder.Add(new StringEntry {Name = Name}); });
                _needRestart = true;
            }

            //Checking if we are in enabled
            if (!GuiSettings.Instance.EnabledPlugins.Any(ent => ent.Equals(Name)))
            {
                Application.Current.Dispatcher.Invoke(delegate { GuiSettings.Instance.EnabledPlugins.Add(Name); });
                _needRestart = true;
            }
        }

        public void Initialize()
        {
            //Install();
            Log.DebugFormat("[{0}] Initialized", Name);
        }

        public void Deinitialize()
        {
            Log.DebugFormat("[{0}] Deinitialized", Name);
        }

        #endregion

        #region Implementation of IConfigurable

        public UserControl Control => _gui ?? new CommunityLibGUI();
        public JsonSettings Settings => CommunityLibSettings.Instance;

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
            if (name == "communitylib_get_current_area_state_cache")
                return AreaStateCache.Current;

            return null;
        }

        #endregion

    }
}
