using System.Collections.ObjectModel;
using System.ComponentModel;
using Loki;
using Loki.Common;

namespace CommunityLib
{
    public class CommunityLibSettings : JsonSettings
    {
        private static CommunityLibSettings _instance;

        /// <summary>The current instance for this class. </summary>
        public static CommunityLibSettings Instance => _instance ?? (_instance = new CommunityLibSettings());

        public CommunityLibSettings() : base(GetSettingsFilePath(Configuration.Instance.Name, "CommunityLib.json"))
        {
            if (CacheTabsCollection == null) 
                CacheTabsCollection = new ObservableCollection<StringEntry>();
        }

        private ObservableCollection<StringEntry> _cacheTabsCollection;

        [DefaultValue(null)]
        public ObservableCollection<StringEntry> CacheTabsCollection
        {
            get { return _cacheTabsCollection; }
            set { _cacheTabsCollection = value; NotifyPropertyChanged(() => CacheTabsCollection); }
        }

        public class StringEntry
        {
            public string Name { get; set; }
        }
    }
}
