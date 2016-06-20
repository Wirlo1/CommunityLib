using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Loki.Bot;
using Loki.Bot.Pathfinding;
using Loki.Common;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;

namespace CommunityLib
{
    /// <summary>A class that holds useful area state information for bots.</summary>
	public class AreaStateCache
    {
        /// <summary>
        /// When set to true, AreaStateCache will not automatically create a GridExplorer instance for the current area.
        /// </summary>
        //public static bool DisableDefaultExplorer;

        /// <summary>Should items be looted based on being visible?</summary>
        public static bool LootVisibleItemsOverride
        {
            get
            {
                return _lootVisibleItemsOverride;
            }
            set
            {
                _lootVisibleItemsOverride = value;
                Log.InfoFormat("[AreaStateCache] LootVisibleItemsOverride = {0}.", _lootVisibleItemsOverride);
            }
        }

        public static bool _lootVisibleItemsOverride = false;

        /// <summary> </summary>
        public class OnLocationAddedEventArgs : EventArgs
        {
            /// <summary>The location being added.</summary>
            public Location Location { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="location"></param>
            internal OnLocationAddedEventArgs(Location location)
            {
                Location = location;
            }
        }

        /// <summary> </summary>
        public class OnChestLocationAddedEventArgs : EventArgs
        {
            /// <summary>The location being added.</summary>
            public ChestLocation ChestLocation { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="location"></param>
            internal OnChestLocationAddedEventArgs(ChestLocation location)
            {
                ChestLocation = location;
            }
        }

        private static readonly Dictionary<uint, AreaStateCache> AreaStates = new Dictionary<uint, AreaStateCache>();

        /// <summary>
        /// The current AreaStateCache for the instance we are in.
        /// </summary>
        public static AreaStateCache Current
        {
            get
            {
                var hash = LokiPoe.LocalData.AreaHash;

                AreaStateCache state;
                if (!AreaStates.TryGetValue(hash, out state))
                {
                    state = new AreaStateCache(hash);

                    AreaStates.Add(hash, state);

                    if (BotManager.IsRunning)
                        state.OnStart();
                }

                return state;
            }
        }

        /// <summary>
        /// A wrapper class to hold an item location entry
        /// </summary>
        public class ItemLocation
        {
            /// <summary> </summary>
            public int Id;

            /// <summary> </summary>
            public string Name;

            /// <summary>The position of this location.</summary>
            public Vector2i Position;

            /// <summary>The cached rarity of the item stored by this location.</summary>
            public Rarity Rarity;

            /// <summary>The cached metadata type of the item stored by this location.</summary>
            public string Metadata;
        }

        /// <summary>
        /// A wrapper class to hold a location entry
        /// </summary>
        public class Location
        {
            /// <summary> </summary>
            public int Id;

            /// <summary> </summary>
            public string Name;

            /// <summary> </summary>
            public Vector2i Position;
        }

        /// <summary>
        /// A wrapper class to hold a cached chest location entry
        /// </summary>
        public class ChestLocation
        {
            /// <summary>Has this chest been blacklisted?</summary>
            public bool IsBlacklisted => Blacklist.Contains(Id);

            /// <summary>The chest's id.</summary>
            public int Id;

            /// <summary>The chest's name.</summary>
            public string Name;

            /// <summary>The chest's type.</summary>
            public string Metadata;

            /// <summary>The chest's position.</summary>
            public Vector2i Position;

            /// <summary>Is the chest corrupted.</summary>
            public bool IsCorrupted;

            /// <summary>Is the chest able to be targeted.</summary>
            public bool IsTargetable;

            /// <summary>Is the chest opened.</summary>
            public bool IsOpened;

            /// <summary>Is the chest locked.</summary>
            public bool IsLocked;

            /// <summary>Is the chest a vaal vessel.</summary>
            public bool IsVaalVessel;

            /// <summary>Is the chest a strongbox.</summary>
            public bool IsStrongBox;

            /// <summary>Is this chest breakable.</summary>
            public bool IsBreakable;

            /// <summary>Does the chest open on damage.</summary>
            public bool OpensOnDamage;

            /// <summary>The chest's rarity.</summary>
            public Rarity Rarity;

            /// <summary>Is the chest identified.</summary>
            public bool IsIdentified;

            /// <summary>A list of chest stats.</summary>
            public List<KeyValuePair<StatTypeGGG, int>> Stats;

            /// <summary>
            /// Returns the chest object associated with this location. If it is not in view, it will return null.
            /// </summary>
            public Chest Chest
            {
                get
                {
                    return LokiPoe.ObjectManager.GetObjectById(Id) as Chest;
                }
            }

            /// <summary>User data for how many tries we have opened the chest.</summary>
            public int OpenAttempts;
        }

        static AreaStateCache()
        {
            ThrottleMs = 500;
            ChestThrottleMs = 250;
            QuestThrottleMs = 500;
            ItemThrottleMs = 25;
            CorruptedAreaParentId = "";
        }

        /// <summary>The rate at which to Tick. Defaults to 500 ms.</summary>
        public static int ThrottleMs { get; set; }

        /// <summary>The rate at which to Tick quest logic. Defaults to 500 ms.</summary>
        public static int QuestThrottleMs { get; set; }

        /// <summary>The rate at which to Tick chest logic. Defaults to 500 ms.</summary>
        public static int ChestThrottleMs { get; set; }

        /// <summary>The rate at which to Tick item logic. Defaults to 100 ms.</summary>
        public static int ItemThrottleMs { get; set; }

        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        /// <summary>A list of locations that are deemed useful. </summary>
        private readonly List<Location> _locations = new List<Location>();

        /// <summary>A list of cached chest locations. </summary>
        private readonly Dictionary<int, ChestLocation> _chestLocations = new Dictionary<int, ChestLocation>();

        /// <summary>A list of cached item locations. </summary>
        private readonly Dictionary<int, ItemLocation> _itemLocations = new Dictionary<int, ItemLocation>();

        /// <summary>Items we have processed for filtering, and don't need to again.</summary>
        private readonly Dictionary<int, Vector2i> _ignoreItems = new Dictionary<int, Vector2i>();

        /// <summary>Do we need to run waypoint logic.</summary>
        private bool _updateCheckForWaypoint = true;

        /// <summary>Do we need to run area transition logic.</summary>
        // ReSharper disable once ConvertToConstant.Local
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private bool _updateAreaTransition = true;

        /// <summary>Should we check for area transitions.</summary>
        public bool ShouldCheckForAreaTransition { get; private set; }

        /// <summary>Should we check for waypoints.</summary>
        public bool ShouldCheckForWaypoint { get; private set; }

        /// <summary>Should we check for stash (town).</summary>
        public bool ShouldCheckForStash { get; private set; }

        /// <summary>Do we have the stash location for this area.</summary>
        public bool HasStashLocation { get; private set; }

        /// <summary>Do we have the waypoint location for this area.</summary>
        public bool HasWaypointLocation { get; private set; }

        /// <summary>Do we have the waypoint entry for this area.</summary>
        public bool HasWaypointEntry { get; private set; }

        /// <summary>Do we have this quest object's location for this area.</summary>
        public bool HasKaruiSpiritLocation { get; private set; }

        /// <summary>A list of seen area transition names for this area.</summary>
        public List<string> SeenAreaTransitions { get; private set; }

        /// <summary>The starting area transition;s name found when we entered the area.</summary>
        public string StartingAreaTransitionName { get; private set; }

        /// <summary>The starting area transition's location found when we entered the area.</summary>
        public Vector2i StartingAreaTransitionLocation { get; private set; }

        /// <summary>The starting portal's location found when we entered the area.</summary>
        public Vector2i StartingPortalLocation { get; private set; }

        /// <summary>The parent area id of the corrupted area.</summary>
        public static string CorruptedAreaParentId { get; set; }

        /// <summary>
        /// The explorer for the current area.
        /// </summary>
        //public IExplorer Explorer { get; set; }

        /// <summary> </summary>
        public uint Hash { get; private set; }

        private readonly Stopwatch _throttle = new Stopwatch();
        private readonly Stopwatch _chestThrottle = new Stopwatch();
        private readonly Stopwatch _questThrottle = new Stopwatch();
        private readonly Stopwatch _itemThrottle = new Stopwatch();

        private readonly Stopwatch _timeInInstance = new Stopwatch();
        private readonly Stopwatch _timeInArea = new Stopwatch();

        private bool _hasAnchorPoint;
        private Vector2i _anchorPoint;
        private Vector2i _currentAnchorPoint;
        private uint _anchorPointSeed;

        private bool? _hasBurningGround;
        private bool? _hasLightningGround;
        private bool? _hasIceGround;

        /// <summary>
        /// The world area entry for this area.
        /// </summary>
        public DatWorldAreaWrapper WorldArea { get; private set; }

        /// <summary></summary>
        private static readonly Dictionary<string, bool> NewInstanceOverrides = new Dictionary<string, bool>();

        /// <summary>
        /// Does this area have the burning ground effect?
        /// </summary>
        public bool HasBurningGround
        {
            get
            {
                if (_hasBurningGround == null)
                {
                    _hasBurningGround = false;

                    int val;
                    if (LokiPoe.LocalData.MapMods.TryGetValue(StatTypeGGG.MapBaseGroundFireDamageToDealPerMinute, out val))
                    {
                        if (val != 0)
                        {
                            _hasBurningGround = true;
                        }
                    }
                }
                return _hasBurningGround.Value;
            }
        }

        /// <summary>
        /// Does this area have the lightning ground effect?
        /// </summary>
        public bool HasLightningGround
        {
            get
            {
                if (_hasLightningGround == null)
                {
                    _hasLightningGround = false;

                    int val;
                    if (LokiPoe.LocalData.MapMods.TryGetValue(StatTypeGGG.MapGroundLightning, out val))
                    {
                        if (val != 0)
                        {
                            _hasLightningGround = true;
                        }
                    }
                }
                return _hasLightningGround.Value;
            }
        }

        /// <summary>
        /// Does this area have the ice ground effect?
        /// </summary>
        public bool HasIceGround
        {
            get
            {
                if (_hasIceGround == null)
                {
                    _hasIceGround = false;

                    int val;
                    if (LokiPoe.LocalData.MapMods.TryGetValue(StatTypeGGG.MapGroundIce, out val))
                    {
                        if (val != 0)
                        {
                            _hasIceGround = true;
                        }
                    }
                }
                return _hasIceGround.Value;
            }
        }

        /// <summary>
        /// The initial starting position for this area.
        /// </summary>
        public Vector2i AnchorPoint
        {
            get
            {
                return _anchorPoint;
            }
        }

        /// <summary>
        /// The current starting position for this area (can be changed).
        /// </summary>
        public Vector2i CurrentAnchorPoint
        {
            get
            {
                return _currentAnchorPoint;
            }
        }

        /// <summary>
        /// Updates the anchor point as the current player's position. 
        /// This is required for local area transitions.
        /// </summary>
        public void ResetCurrentAnchorPoint()
        {
            _currentAnchorPoint = LokiPoe.MyPosition;
            Log.DebugFormat("[ResetAnchorPoint] Setting CurrentAnchorPoint to {0} for {1}.", _currentAnchorPoint,
                _anchorPointSeed);
        }

        private void ResetAnchorPoint()
        {
            _anchorPoint = LokiPoe.MyPosition;
            _hasAnchorPoint = true;
            _anchorPointSeed = LokiPoe.LocalData.AreaHash;
            Log.DebugFormat("[ResetAnchorPoint] Setting AnchorPoint to {0} for {1}.", _anchorPoint, _anchorPointSeed);
        }

        /// <summary>Returns how long the character has been in this instance.</summary>
        public TimeSpan TimeInInstance
        {
            get
            {
                return _timeInInstance.Elapsed;
            }
        }

        /// <summary>Returns how long the character has been in the current area.</summary>
        public TimeSpan TimeInArea
        {
            get
            {
                return _timeInArea.Elapsed;
            }
        }

        /// <summary>
        /// Resets the time in the area.
        /// </summary>
        public void ResetTimeInArea()
        {
            if (_timeInArea.IsRunning)
            {
                _timeInArea.Restart();
            }
            else
            {
                _timeInArea.Reset();
            }
        }

        private AreaStateCache(uint hash)
        {
            Hash = hash;

            WorldArea = LokiPoe.CurrentWorldArea;

            ShouldCheckForWaypoint = LokiPoe.CurrentWorldArea.HasWaypoint;

            ShouldCheckForStash = LokiPoe.CurrentWorldArea.IsTown;
            HasStashLocation = false;

            HasWaypointLocation = false;

            var areaId = WorldArea.Id;
            HasWaypointEntry = LokiPoe.InstanceInfo.AvailableWaypoints.ContainsKey(areaId);

            SeenAreaTransitions = new List<string>();

            //GenerateStaticLocations();

            ResetStartingAreaTransition();

            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>().OrderBy(a => a.Distance).FirstOrDefault();
            StartingPortalLocation = portal != null
                ? ExilePather.FastWalkablePositionFor(portal)
                : Vector2i.Zero;

            //if (!DisableDefaultExplorer)
            //{
            //    var ge = new GridExplorer
            //    {
            //        AutoResetOnAreaChange = false
            //    };
            //    Explorer = ge;
            //}
        }

        /// <summary>
        /// Used to reset the starting area transition. This is for handling local area transitions.
        /// </summary>
        public void ResetStartingAreaTransition()
        {
            var at = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(a => a.Distance).FirstOrDefault();
            StartingAreaTransitionName = at != null ? at.Name : "";
            StartingAreaTransitionLocation = at != null
                ? ExilePather.FastWalkablePositionFor(at)
                : Vector2i.Zero;
        }

        /// <summary>The bot Start event.</summary>
        public static void Start()
        {
            foreach (var kvp in AreaStates)
            {
                kvp.Value.OnStart();
            }
            ItemEvaluator.OnRefreshed += ItemEvaluatorOnOnRefresh;
        }

        private static void ItemEvaluatorOnOnRefresh(object sender,
            ItemEvaluatorRefreshedEventArgs itemEvaluatorRefreshedEventArgs)
        {
            Log.InfoFormat("[ItemEvaluatorOnOnRefresh] Now clearing ignored items.");
            Current._ignoreItems.Clear();
        }

        /// <summary>
        /// Removes an item location.
        /// </summary>
        /// <param name="location"></param>
        public void RemoveItemLocation(ItemLocation location)
        {
            Log.InfoFormat("[RemoveItemLocation] The location {0} [{1}] is being removed.", location.Id, location.Name);
            _itemLocations.Remove(location.Id);
        }

        /// <summary> </summary>
        private void OnStart()
        {
            _updateCheckForWaypoint = true;
            _throttle.Reset();
            _chestThrottle.Reset();
            _questThrottle.Reset();
            _itemThrottle.Reset();
            //if (Explorer != null)
            //{
            //    Explorer.Start();
            //}
        }

        /// <summary>The bot Tick event.</summary>
        public static void Tick()
        {
            if (!LokiPoe.IsInGame)
                return;

            var unload = new List<uint>();

            Current.OnTick();
            foreach (var kvp in AreaStates)
            {
                if (kvp.Value != Current)
                {
                    if (kvp.Value.OnInactiveTick())
                    {
                        unload.Add(kvp.Key);
                    }
                }
            }

            // Remove memory from unloaded areas.
            foreach (var id in unload)
            {
                AreaStates.Remove(id);
            }
        }

        /// <summary></summary>
        private bool OnInactiveTick()
        {
            if (_timeInInstance.IsRunning)
                _timeInInstance.Stop();

            if (_timeInArea.IsRunning)
                _timeInArea.Stop();

            // Unload other instances of the same area we're currently in.
            if (WorldArea == Current.WorldArea)
            {
                //if (Explorer != null)
                //{
                //    Explorer.Unload();
                //}
                return true;
            }

            // If we're in an area of the same type, but a different area, we can remove it to avoid memory stacking up.
            if ((WorldArea.IsOverworldArea && Current.WorldArea.IsOverworldArea) ||
                (WorldArea.IsMap && Current.WorldArea.IsMap) ||
                (WorldArea.IsTown && Current.WorldArea.IsTown) ||
                (WorldArea.IsCorruptedArea && Current.WorldArea.IsCorruptedArea) ||
                (WorldArea.IsRelicArea && Current.WorldArea.IsRelicArea) ||
                (WorldArea.IsDenArea && Current.WorldArea.IsDenArea) ||
                (WorldArea.IsDailyArea && Current.WorldArea.IsDailyArea) ||
                (WorldArea.IsMissionArea && Current.WorldArea.IsMissionArea) ||
                (WorldArea.IsHideoutArea && Current.WorldArea.IsHideoutArea)
                )
            {
                //if (Explorer != null)
                //{
                //    Explorer.Unload();
                //}
                return true;
            }

            return false;
        }

        private void OnTick()
        {
            //if (Explorer != null)
            //{
            //    Explorer.Tick();
            //}

            var update = false;
            if (!_itemThrottle.IsRunning)
            {
                _itemThrottle.Start();
                update = true;
            }
            else
            {
                if (_itemThrottle.ElapsedMilliseconds >= ItemThrottleMs)
                {
                    update = true;
                }
            }

            //using (new PerformanceTimer("Tick::WorldItem", 1))
            {
                if (update)
                {
                    var myPos = LokiPoe.MyPosition;

                    var added = 0;
                    foreach (var worldItem in LokiPoe.ObjectManager.GetObjectsByType<WorldItem>())
                    {
                        var doAdd = false;

                        Vector2i pos;
                        if (!_ignoreItems.TryGetValue(worldItem.Id, out pos))
                        {
                            doAdd = true;
                        }
                        else
                        {
                            if (pos != worldItem.Position)
                            {
                                Log.InfoFormat("[AreaStateCache] An item collision has been detected! Item id {0}.", worldItem.Id);
                                _ignoreItems.Remove(worldItem.Id);
                                doAdd = true;
                            }
                        }

                        if (doAdd)
                        {
                            if (added > 10)
                                break;

                            ++added;

                            var item = worldItem.Item;

                            if (worldItem.IsAllocatedToOther)
                            {
                                if (DateTime.Now < worldItem.PublicTime)
                                {
                                    //Log.InfoFormat("[AreaStateCache] The item {0} is not being marked for pickup because it is allocated to another player.", item.FullName);
                                    //_ignoreItems.Add(worldItem.Id, worldItem.Position);
                                    continue;
                                }
                            }

                            var visibleOverride = false;
                            if (LootVisibleItemsOverride)
                            {
                                // We can only consider items when they are visible, otherwise we ignore stuff we might want.
                                if (!LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
                                    continue;

                                if (LokiPoe.Input.GetClickableHighlightLabelPosition(worldItem) != Vector2.Zero)
                                {
                                    visibleOverride = true;
                                }
                            }

                            IItemFilter filter = null;
                            if (visibleOverride || ItemEvaluator.Match(item, EvaluationType.PickUp, out filter))
                            {
                                var location = new ItemLocation
                                {
                                    Id = worldItem.Id,
                                    Name = worldItem.Name,
                                    Position = worldItem.Position,
                                    Rarity = worldItem.Item.Rarity,
                                    Metadata = worldItem.Item.Metadata
                                };

                                if (_itemLocations.ContainsKey(location.Id))
                                {
                                    _itemLocations[location.Id] = location;
                                }
                                else
                                {
                                    _itemLocations.Add(location.Id, location);
                                }

                                Log.InfoFormat("[AreaStateCache] The location {0} [{1}] is being added from filter [{3}].{2}", location.Id,
                                    location.Name,
                                    worldItem.HasAllocation ? " [Allocation " + worldItem.PublicTime + "]" : "",
                                    filter != null ? filter.Name : "(null)");
                            }

                            _ignoreItems.Add(worldItem.Id, worldItem.Position);
                        }
                    }

                    var toRemove = new List<int>();
                    foreach (var kvp in _itemLocations)
                    {
                        if (Blacklist.Contains(kvp.Key))
                        {
                            Log.InfoFormat("[AreaStateCache] The location {0} [{1}] is being removed because the id has been Blacklisted.",
                                kvp.Value.Id, kvp.Value.Name);
                            toRemove.Add(kvp.Value.Id);
                        }
                        else if (myPos.Distance(kvp.Value.Position) < 30)
                        {
                            if (LokiPoe.ObjectManager.GetObjectById<WorldItem>(kvp.Value.Id) == null)
                            {
                                Log.InfoFormat("[AreaStateCache] The location {0} [{1}] is being removed because the WorldItem does not exist.",
                                    kvp.Value.Id, kvp.Value.Name);
                                toRemove.Add(kvp.Value.Id);
                            }
                        }
                    }

                    foreach (var id in toRemove)
                    {
                        _itemLocations.Remove(id);
                    }

                    _itemThrottle.Restart();
                }
            }

            if (!_chestThrottle.IsRunning)
            {
                _chestThrottle.Start();
            }
            else
            {
                if (_chestThrottle.ElapsedMilliseconds >= ChestThrottleMs)
                {
                    //using (new PerformanceTimer("Tick::Chest", 1))
                    {
                        var addedChests = new List<ChestLocation>();
                        foreach (var chest in LokiPoe.ObjectManager.GetObjectsByType<Chest>().ToList())
                        {
                            ChestLocation location;
                            if (!_chestLocations.TryGetValue(chest.Id, out location))
                            {
                                location = new ChestLocation
                                {
                                    Id = chest.Id,
                                    Name = chest.Name,
                                    IsTargetable = chest.IsTargetable,
                                    IsOpened = chest.IsOpened,
                                    IsStrongBox = chest.IsStrongBox,
                                    IsVaalVessel = chest.IsVaalVessel,
                                    OpensOnDamage = chest.OpensOnDamage,
                                    Position = chest.Position,
                                    Stats = chest.Stats.ToList(),
                                    IsIdentified = chest.IsIdentified,
                                    IsBreakable = chest.OpensOnDamage,
                                    Rarity = chest.Rarity,
                                    Metadata = chest.Type
                                };

                                _chestLocations.Add(location.Id, location);

                                addedChests.Add(location);
                            }

                            if (!location.IsOpened)
                            {
                                location.IsOpened = chest.IsOpened;
                                location.IsLocked = chest.IsLocked;
                                location.IsTargetable = chest.IsTargetable;
                                // Support for chests that change locked state, without the lock state updating.
                                var tc = chest.Components.TransitionableComponent;
                                if (tc != null)
                                {
                                    if ((tc.Flag1 & 2) != 0)
                                    {
                                        location.IsLocked = false;
                                    }
                                }
                                if (chest.IsVaalVessel)
                                {
                                    location.IsLocked = false;
                                }
                                if (!location.IsCorrupted && chest.IsCorrupted)
                                {
                                    location.IsCorrupted = chest.IsCorrupted;
                                    location.Stats = chest.Stats.ToList();
                                }
                                if (!location.IsIdentified && chest.IsIdentified)
                                {
                                    location.IsIdentified = chest.IsIdentified;
                                    location.Stats = chest.Stats.ToList();
                                }
                            }

                            if (addedChests.Count > 10)
                                break;
                        }

                        foreach (var location in addedChests)
                        {
                            if (!location.IsBreakable)
                            {
                                location.Position = ExilePather.FastWalkablePositionFor(location.Position);
                            }

                            LokiPoe.InvokeEvent(OnChestLocationAdded, null, new OnChestLocationAddedEventArgs(location));
                        }

                        addedChests.Clear();

                        _chestThrottle.Restart();
                    }
                }
            }

            if (!_questThrottle.IsRunning)
            {
                _questThrottle.Start();
            }
            else
            {
                if (_questThrottle.ElapsedMilliseconds >= QuestThrottleMs)
                {
                    if (LokiPoe.CurrentWorldArea.IsMissionArea)
                    {
                        if (!HasKaruiSpiritLocation)
                        {
                            var obj = LokiPoe.ObjectManager.GetObjectByName("Karui Spirit");
                            if (obj != null)
                            {
                                AddLocation(ExilePather.FastWalkablePositionFor(obj), obj.Id, obj.Name);
                                HasKaruiSpiritLocation = true;
                            }
                        }
                    }

                    _questThrottle.Restart();
                }
            }

            if (!_throttle.IsRunning)
            {
                _throttle.Start();
            }
            else
            {
                if (_throttle.ElapsedMilliseconds >= ThrottleMs)
                {
                    if (!_timeInInstance.IsRunning)
                        _timeInInstance.Start();

                    if (!_timeInArea.IsRunning)
                        _timeInArea.Start();

                    // Do we need to update wp state flags.
                    if (_updateCheckForWaypoint)
                    {
                        // If the current area doesn't have a wp, we do not want to do any more logic processing.
                        if (!LokiPoe.CurrentWorldArea.HasWaypoint)
                        {
                            _updateCheckForWaypoint = false;
                            ShouldCheckForWaypoint = false;
                            HasWaypointLocation = false;
                            HasWaypointEntry = false;
                        }
                        else
                        {
                            ShouldCheckForWaypoint = true;
                        }
                    }

                    // Do we need to update at state flags.
                    if (_updateAreaTransition)
                    {
                        ShouldCheckForAreaTransition = true;
                    }

                    if (ShouldCheckForStash)
                    {
                        //using (new PerformanceTimer("ShouldCheckForStash", 1))
                        {
                            if (!HasStashLocation)
                            {
                                var stash = LokiPoe.ObjectManager.Stash;
                                if (stash != null)
                                {
                                    // Save the location so we know where it is when the entity isn't in view.
                                    AddLocation(ExilePather.FastWalkablePositionFor(stash), stash.Id, "Stash");

                                    // We now have the waypoint location.
                                    HasStashLocation = true;
                                }
                            }
                            else
                            {
                                ShouldCheckForStash = false;
                            }
                        }
                    }

                    // If we need to handle wps.
                    if (ShouldCheckForWaypoint)
                    {
                        //using (new PerformanceTimer("ShouldCheckForWaypoint", 1))
                        {
                            // If we don't have the wp location yet, check to see if we see one.
                            if (!HasWaypointLocation)
                            {
                                var wp = LokiPoe.ObjectManager.Waypoint;
                                if (wp != null)
                                {
                                    // Save the location so we know where it is when the entity isn't in view.
                                    AddLocation(ExilePather.FastWalkablePositionFor(wp), wp.Id, "Waypoint");

                                    // We now have the waypoint location.
                                    HasWaypointLocation = true;
                                }
                            }

                            // If we don't have the wp entry yet, poll for us having it now.
                            // But only if we've seen the waypoint, since otherwise there's no way we have it.
                            if (HasWaypointLocation && !HasWaypointEntry)
                            {
                                var areaId = WorldArea.Id;
                                HasWaypointEntry = LokiPoe.InstanceInfo.AvailableWaypoints.ContainsKey(areaId);
                            }

                            // Once we have both the location and the entry, we do not need to execute wp logic anymore.
                            if (HasWaypointLocation && HasWaypointEntry)
                            {
                                _updateCheckForWaypoint = false;
                                ShouldCheckForWaypoint = false;
                            }
                        }
                    }

                    // If we need to handle ats.
                    if (ShouldCheckForAreaTransition)
                    {
                        //using (new PerformanceTimer("ShouldCheckForAreaTransition", 1))
                        {
                            // If there are any area transitions on screen, add them if we don't already know of them.
                            foreach (var transition in LokiPoe.ObjectManager.Objects.OfType<AreaTransition>().ToList())
                            {
                                var name = transition.Name;

                                // We have to check all this in order to handle the areas that have transitions with the same name, but different
                                // entity ids.
                                if (HasLocation(name, transition.Id))
                                {
                                    continue;
                                }

                                AddLocation(ExilePather.FastWalkablePositionFor(transition), transition.Id, name);

                                if (!SeenAreaTransitions.Contains(name))
                                {
                                    SeenAreaTransitions.Add(name);
                                }
                            }
                        }
                    }

                    // Check to see if we need a new anchor point to kite back towards.
                    if (!_hasAnchorPoint || LokiPoe.LocalData.AreaHash != _anchorPointSeed)
                    {
                        ResetAnchorPoint();
                        ResetCurrentAnchorPoint();
                    }

                    _throttle.Restart();
                }
            }
        }

        /// <summary>The bot Stop event.</summary>
        public static void Stop()
        {
            foreach (var kvp in AreaStates)
            {
                kvp.Value.OnStop();
            }

            ItemEvaluator.OnRefreshed -= ItemEvaluatorOnOnRefresh;
        }

        /// <summary> </summary>
        private void OnStop()
        {
            _throttle.Reset();
            _chestThrottle.Reset();
            _timeInInstance.Stop();
            _timeInArea.Stop();
            //if (Explorer != null)
            //{
            //    Explorer.Stop();
            //}
        }

        /// <summary>An event handler for when a location is added.</summary>
        public static event EventHandler<OnLocationAddedEventArgs> OnLocationAdded;

        /// <summary>An event handler for when a chest location is added.</summary>
        public static event EventHandler<OnChestLocationAddedEventArgs> OnChestLocationAdded;

        /// <summary>
        /// This function will add a location that we can reference later. An example of a location
        /// would be area transitions or NPCs which can despawn.
        /// </summary>
        public void AddLocation(Vector2i position, int id, string name)
        {
            var location = new Location
            {
                Position = position,
                Id = id,
                Name = name
            };
            _locations.Add(location);

            Log.DebugFormat("Adding location [\"{0}\"][{1}] = {2} for area [0x{3:X}]", location.Name,
                location.Id, location.Position, Hash);

            LokiPoe.InvokeEvent(OnLocationAdded, null, new OnLocationAddedEventArgs(location));
        }

        /// <summary>
        /// Clears the list of locations.
        /// </summary>
        public void ClearLocations()
        {
            _locations.Clear();
        }

        /// <summary>
        /// Clears the list of chest locations.
        /// </summary>
        public void ClearChestLocations()
        {
            _chestLocations.Clear();
        }

        /// <summary>
        /// Clears the list of item locations.
        /// </summary>
        public void ClearItemLocations()
        {
            foreach (var kvp in _itemLocations)
            {
                Log.InfoFormat("[ClearItemLocations] The location {0} [{1}] is being removed.", kvp.Value.Id, kvp.Value.Name);
            }

            _itemLocations.Clear();
            _ignoreItems.Clear();
        }

        /// <summary>
        /// Returns an IEnumerable of chest locations.
        /// </summary>
        public IEnumerable<ChestLocation> ChestLocations
        {
            get
            {
                return _chestLocations.Select(kvp => kvp.Value);
            }
        }

        /// <summary>
        /// Returns an IEnumerable of item locations.
        /// </summary>
        public IEnumerable<ItemLocation> ItemLocations
        {
            get
            {
                return _itemLocations.Select(kvp => kvp.Value);
            }
        }

        /// <summary>
        /// Returns the list of locations in the specified area that have the name.
        /// </summary>
        public IEnumerable<Location> GetLocations(string name)
        {
            return _locations.Where(l => l.Name == name);
        }

        /// <summary>
        /// Returns true if the specified area hash has the location by name.
        /// </summary>
        public bool HasLocation(string name)
        {
            return _locations.Any(l => l.Name == name);
        }

        /// <summary>
        /// Returns true if the specified area hash has the location by name.
        /// </summary>
        public bool HasLocation(string name, int id)
        {
            return _locations.Any(l => l.Name == name && l.Id == id);
        }

        /// <summary>
        /// A function to set the new instance override for a specific area by id.
        /// </summary>
        /// <param name="id">The area's id.</param>
        /// <param name="value">The new instance override flag.</param>
        public static void SetNewInstanceOverride(string id, bool value)
        {
            bool flag;
            if (NewInstanceOverrides.TryGetValue(id, out flag))
            {
                NewInstanceOverrides[id] = value;
            }
            else
            {
                NewInstanceOverrides.Add(id, value);
            }
            Log.InfoFormat("[SetNewInstanceOverride] {0} = {1}", id, value);
        }

        /// <summary>
        /// A function to get the new instance override for a specific area by id.
        /// </summary>
        /// <param name="id">The area's id.</param>
        /// <param name="flag">The area's set value.</param>
        /// <returns>Returns true if there is a new instance override and false otherwise.</returns>
        public static bool HasNewInstanceOverride(string id, out bool flag)
        {
            if (NewInstanceOverrides.TryGetValue(id, out flag))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the new instance override for a specific area by id.
        /// </summary>
        /// <param name="id">The area's id.</param>
        public static void RemoveNewInstanceOverride(string id)
        {
            NewInstanceOverrides.Remove(id);
            Log.InfoFormat("[RemoveNewInstanceOverride] {0}", id);
        }
    }

}
