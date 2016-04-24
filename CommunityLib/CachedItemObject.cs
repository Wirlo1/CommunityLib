using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;

namespace CommunityLib
{
    /// <summary>
    /// CachedItem can be used for Inventory or Stash items ONLY!
    /// <para>When creating a class of an item in the Stash, make sure you'll set the tabName property!!!</para>
    /// </summary>
    public class CachedItemObject
    {
        public int ItemId { get; set; }
        public string TabName { get; set; }
        public string League { get; set; }

        public int MaxStackCount { get; set; }
        public int MaxCurrencyTabStackCount { get; set; }
        public bool IsInCurrencyTab => MaxCurrencyTabStackCount > 0;
        public List<CachedModAffix> Affixes { get; set; }
        public int ArmorValue { get; set; }
        public double AttacksPerSecond { get; set; }
        public int BaseArmor { get; set; }
        public double BaseAttacksPerSecond { get; set; }
        public double BaseCritialStrikeChance { get; set; }
        public int BaseEnergyShield { get; set; }
        public int BaseEvasion { get; set; }
        public int BaseMaxPhysicalDamage { get; set; }
        public int BaseMinPhysicalDamage { get; set; }
        public int BaseRequiredLevel { get; set; }
        public int BaseWeaponType { get; set; }
        public double CritialStrikeChance { get; set; }
        public int EnergyShieldValue { get; set; }
        public int EvasionValue { get; set; }
        public List<CachedModAffix> ExplicitAffixes { get; set; }
        public Dictionary<string, int> ExplicitStats { get; set; }
        public bool FitsEquipRequirements { get; set; }
        public string FullName { get; set; }
        public bool HasFullStack { get; set; }
        public bool HasInventoryLocation { get; set; }
        public bool HasMicrotransitionAttachment { get; set; }
        public List<CachedModAffix> ImplicitAffixes { get; set; }
        public Dictionary<string, int> ImplicitStats { get; set; }
        public InventoryType InventoryType { get; set; }
        public bool IsAmuletType { get; set; }
        public bool IsArmorType { get; set; }
        public bool IsBeltType { get; set; }
        public bool IsBodyArmorType { get; set; }
        public bool IsBootType { get; set; }
        public bool IsBowType { get; set; }
        public bool IsChromatic { get; set; }
        public bool IsClawType { get; set; }
        public bool IsCorrupted { get; set; }
        public bool IsCurrencyType { get; set; }
        public bool IsDaggerType { get; set; }
        public bool IsDivinationCardType { get; set; }
        public bool IsFishingRodType { get; set; }
        public bool IsFlaskType { get; set; }
        public bool IsGloveType { get; set; }
        public bool IsHelmetType { get; set; }
        public bool IsIdentified { get; set; }
        public bool IsJewelType { get; set; }
        public bool IsMapFragmentType { get; set; }
        public bool IsMapType { get; set; }
        public bool IsMirrored { get; set; }
        public bool IsOneHandAxeType { get; set; }
        public bool IsOneHandMaceType { get; set; }
        public bool IsOneHandSwordType { get; set; }
        public bool IsOneHandThrustingSwordType { get; set; }
        public bool IsOneHandWeaponType { get; set; }
        public bool IsQuestType { get; set; }
        public bool IsQuiverType { get; set; }
        public bool IsRingType { get; set; }
        public bool IsShieldType { get; set; }
        public bool IsStackable { get; set; }
        public bool IsStaffType { get; set; }
        public bool IsTwoHandAxeType { get; set; }
        public bool IsTwoHandMaceType { get; set; }
        public bool IsTwoHandSwordType { get; set; }
        public bool IsTwoHandWeaponType { get; set; }
        public bool IsUsable { get; set; }
        public bool IsWandType { get; set; }
        public bool IsWeaponType { get; set; }
        public int ItemLevel { get; set; }
        public InventoryType ItemType { get; set; }
        public int LocalId { get; set; }
        public Dictionary<string, int> LocalStats { get; set; }
        public Vector LocationBottomRight { get; set; }
        public Vector LocationTopLeft { get; set; }
        public int MapLevel { get; set; }
        public int MaxChaosDamage { get; set; }
        public int MaxColdDamage { get; set; }
        public int MaxDamage { get; set; }
        public int MaxDps { get; set; }
        public int MaxElementalDamage { get; set; }
        public int MaxFireDamage { get; set; }
        public int MaxLightningDamage { get; set; }
        public int MaxLinkCount { get; set; }
        public int MaxPhysicalDamage { get; set; }
        public int MaxQuality { get; set; }
        public int MinChaosDamage { get; set; }
        public int MinColdDamage { get; set; }
        public int MinDamage { get; set; }
        public int MinDps { get; set; }
        public int MinElementalDamage { get; set; }
        public int MinFireDamage { get; set; }
        public int MinLightningDamage { get; set; }
        public int MinPhysicalDamage { get; set; }
        public string Name { get; set; }
        public int Quality { get; set; }
        public Rarity Rarity { get; set; }
        public int RequiredDex { get; set; }
        public int RequiredInt { get; set; }
        public int RequiredLevel { get; set; }
        public int RequiredStr { get; set; }
        public Vector Size { get; set; }
        public int SocketCount { get; set; }
        public int StackCount { get; set; }
        public Dictionary<string, int> Stats { get; set; }
        public List<string> Tags { get; set; }
        public string Type { get; set; }

        private InventoryControlWrapper Wrapper
        {
            get
            {
                //If there is no tab name then we can assume it's inventory
                if (string.IsNullOrEmpty(TabName))
                    return LokiPoe.InGameState.InventoryUi.InventoryControl_Main;

                if (IsInCurrencyTab)
                {
                    //BUG: It's going to return on first wrapper.
                    //BUG: If you'll have the currencies in misc, they are going to be missed.
                    return LokiPoe.InGameState.StashUi.CurrencyTabInventoryControls
                    .FirstOrDefault(d => d.CurrencyTabItem != null && d.CurrencyTabItem.FullName.Equals(FullName));
                }

                return LokiPoe.InGameState.StashUi.InventoryControl;
            }
        }

        /// <summary>
        /// If the item is in stash, make sure the correct stash tab is loaded or use GetItem method!
        /// </summary>
        private Item Item
        {
            get
            {
                //If TabName is null or empty then we need to look at the main inventory
                if (Wrapper == null)
                {
                    CommunityLib.Log.DebugFormat("[CachedItem] returned null on Wrapper == null");
                    CommunityLib.Log.DebugFormat($"[CachedItem] FullName was {FullName}, name was {Name}");
                    CommunityLib.Log.DebugFormat($"[CachedItem] Wrapper was {Wrapper}");
                    return null;
                }

                //Premium stash tabs can contain only one item
                //It's "safe" to return it like that.
                if (Wrapper.HasCurrencyTabOverride)
                    return Wrapper.CurrencyTabItem;

                //Find the item by it's location first
                //Maybe it has changed, as a failproof find by Location aswell.
                var ret = Wrapper.Inventory.GetItemAtLocation(Convert.ToInt32(LocationTopLeft.X), Convert.ToInt32(LocationTopLeft.Y)) ??
                          Wrapper.Inventory.GetItemById(ItemId);

                if (ret == null)
                {
                    CommunityLib.Log.DebugFormat("[CachedItem] returned null on Wrapper.Inventory.GetIte(...)");
                    CommunityLib.Log.DebugFormat($"[CachedItem] FullName was {FullName}, name was {Name}");
                    CommunityLib.Log.DebugFormat($"[CachedItem] Wrapper was {Wrapper}");
                    return null;
                }

                //Make sure the item's name is equal to the one we should have
                //We assume it'll be the same as cached one.
                if (ret.FullName.Equals(FullName))
                    return ret;

                CommunityLib.Log.DebugFormat("[CachedItem] returned null on ret.FullName.Equals");
                CommunityLib.Log.DebugFormat($"[CachedItem] FullName was {FullName}, name was {Name}");
                CommunityLib.Log.DebugFormat($"[CachedItem] Wrapper was {Wrapper}");
                return null;
            }
        }

        public CachedItemObject(InventoryControlWrapper wrp, Item item, string tabName = "")
        {
            //Wrapper = wrp;
            TabName = tabName;
            League = string.Copy(LokiPoe.Me.League);

            if (wrp.HasCurrencyTabOverride)
                MaxCurrencyTabStackCount = item.MaxCurrencyTabStackCount;

            Update(item);
        }

        /// <summary>
        /// Opens the stash and the stash tab of this item. 
        /// <para>If the item is in inventory then it opens the inventory</para>
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoTo()
        {
            if (!string.IsNullOrEmpty(TabName))
                return await Stash.OpenStashTabTask(TabName);

            //TabName is empty, so it's inventory
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                return await LibCoroutines.OpenInventoryPanel();

            return true;
        }

        private async Task<InventoryControlWrapper> GetWrapper()
        {
            if (!await GoTo())
                return null;

            return Wrapper;
        }

        /// <summary>
        /// Find the item. Remember to check if it's not null, because something with it might have changed in the meantime.
        /// <para>If the item is in stash, it's getting opened first and then returning the value.</para>
        /// <para>if TabName is empty, it assumes the item is in inventory.</para>
        /// </summary>
        /// <returns>Opened current stash and the item itself</returns>
        private async Task<Item> GetItem()
        {
            if (!await GoTo())
                return null;

            return Item;
        }

        public async Task<bool> FastMove(int retries = 3)
        {
            var item = await GetItem();
            var wrapper = await GetWrapper();
            if (wrapper == null || item == null)
            {
                CommunityLib.Log.ErrorFormat("[{0}] Failed to get item or wrapper, item == null: {1}, wrapper == null == {2}", Name, item == null, wrapper == null);
                return false;
            }

            return await Inventory.FastMove(wrapper, item.LocalId, retries);
        }

        public async Task<ApplyCursorResult> UseOnItem(InventoryControlWrapper destinationWrapper, Item destinationItem,
            Inventory.StopUsingDelegate delegateToStop = null)
        {
            var item = await GetItem();
            var wrapper = await GetWrapper();
            if (wrapper == null || item == null)
            {
                CommunityLib.Log.ErrorFormat("[{0}] Failed to get item or wrapper, item == null: {1}, wrapper == null == {2}", Name, item == null, wrapper == null);
                return ApplyCursorResult.ItemNotFound;
            }

            var res = await Inventory.UseItemOnItem(Wrapper, item, destinationWrapper, destinationItem, delegateToStop);

            //Updating the StackCount now and removing the item if needed
            await Update();

            return res;
        }

        public async Task<bool> SplitAndPlaceInMainInventory(int pickupAmount)
        {
            var item = await GetItem();
            var wrapper = await GetWrapper();
            if (wrapper == null || item == null)
            {
                CommunityLib.Log.ErrorFormat("[{0}] Failed to get item or wrapper, item == null: {1}, wrapper == null == {2}", Name, item == null, wrapper == null);
                return false;
            }

            var res = await Inventory.SplitAndPlaceItemInMainInventory(wrapper, item, pickupAmount);

            await Update();

            return res;
        }

        /// <summary>
        /// Updating the StackCount now and removing the item from cache if needed
        /// </summary>
        /// <returns></returns>
        private async Task Update()
        {
            var item = await GetItem();
            var delete = false;
            if (item != null)
            {
                Update(item);
                if (StackCount == 0)
                    delete = true;
            }

            //Item dissapeared
            if (item == null || delete)
            {
                var isHere = Data.CachedItemsInStash.Contains(this);
                if (isHere)
                {
                    CommunityLib.Log.Debug("[CommunityLib][StashCache] An item is being removed from cache");
                    Data.CachedItemsInStash.Remove(this);
                } 
            }
        }

        /// <summary>
        /// Updates current object with given item
        /// </summary>
        /// <param name="item"></param>
        private void Update(Item item)
        {
            ItemId = item.LocalId;
            Affixes = new List<CachedModAffix>();
            foreach (var affixes in item.Affixes)
                Affixes.Add(new CachedModAffix(affixes));

            ArmorValue = item.ArmorValue;
            AttacksPerSecond = item.AttacksPerSecond;
            BaseArmor = item.BaseArmor;
            BaseAttacksPerSecond = item.BaseAttacksPerSecond;
            BaseCritialStrikeChance = item.BaseCritialStrikeChance;
            BaseEnergyShield = item.BaseEnergyShield;
            BaseEvasion = item.BaseEvasion;
            BaseMaxPhysicalDamage = item.BaseMaxPhysicalDamage;
            BaseMinPhysicalDamage = item.BaseMinPhysicalDamage;
            //BaseObject = item.BaseObject;
            BaseRequiredLevel = item.BaseRequiredLevel;
            BaseWeaponType = item.BaseWeaponType;
            //Components = item.Components;
            CritialStrikeChance = item.CritialStrikeChance;
            EnergyShieldValue = item.EnergyShieldValue;
            EvasionValue = item.EvasionValue;
            ExplicitAffixes = new List<CachedModAffix>();
            foreach (var explicitAffixes in item.ExplicitAffixes)
                ExplicitAffixes.Add(new CachedModAffix(explicitAffixes));

            ExplicitStats = new Dictionary<string, int>();
            foreach (var explicitStat in item.ExplicitStats)
                ExplicitStats.Add(explicitStat.Key.ToString(), explicitStat.Value);

            FitsEquipRequirements = item.FitsEquipRequirements;
            FullName = item.FullName;
            HasFullStack = item.HasFullStack;
            HasInventoryLocation = item.HasInventoryLocation;
            HasMicrotransitionAttachment = item.HasMicrotransitionAttachment;
            ImplicitAffixes = new List<CachedModAffix>();
            foreach (var implicitAffixes in item.ImplicitAffixes)
                ImplicitAffixes.Add(new CachedModAffix(implicitAffixes));

            ImplicitStats = new Dictionary<string, int>();
            foreach (var implicitStat in item.ImplicitStats)
                ImplicitStats.Add(implicitStat.Key.ToString(), implicitStat.Value);

            IsAmuletType = item.IsAmuletType;
            IsArmorType = item.IsArmorType;
            IsBeltType = item.IsBeltType;
            IsBodyArmorType = item.IsBodyArmorType;
            IsBootType = item.IsBootType;
            IsBowType = item.IsBowType;
            IsChromatic = item.IsChromatic;
            IsClawType = item.IsClawType;
            IsCorrupted = item.IsCorrupted;
            IsCurrencyType = item.IsCurrencyType;
            IsDaggerType = item.IsDaggerType;
            IsDivinationCardType = item.IsDivinationCardType;
            IsFishingRodType = item.IsFishingRodType;
            IsFlaskType = item.IsFlaskType;
            IsGloveType = item.IsGloveType;
            IsHelmetType = item.IsHelmetType;
            IsIdentified = item.IsIdentified;
            IsJewelType = item.IsJewelType;
            IsMapFragmentType = item.IsMapFragmentType;
            IsMapType = item.IsMapType;
            IsMirrored = item.IsMirrored;
            IsOneHandAxeType = item.IsOneHandAxeType;
            IsOneHandMaceType = item.IsOneHandMaceType;
            IsOneHandSwordType = item.IsOneHandSwordType;
            IsOneHandThrustingSwordType = item.IsOneHandThrustingSwordType;
            IsOneHandWeaponType = item.IsOneHandWeaponType;
            IsQuestType = item.IsQuestType;
            IsQuiverType = item.IsQuiverType;
            IsRingType = item.IsRingType;
            IsShieldType = item.IsShieldType;
            IsStackable = item.IsStackable;
            IsStaffType = item.IsStaffType;
            IsTwoHandAxeType = item.IsTwoHandAxeType;
            IsTwoHandMaceType = item.IsTwoHandMaceType;
            IsTwoHandSwordType = item.IsTwoHandSwordType;
            IsTwoHandWeaponType = item.IsTwoHandWeaponType;
            IsUsable = item.IsUsable;
            IsWandType = item.IsWandType;
            IsWeaponType = item.IsWeaponType;
            ItemLevel = item.ItemLevel;
            ItemType = item.ItemType;
            LocalId = item.LocalId;
            LocalStats = new Dictionary<string, int>();
            foreach (var localStat in item.LocalStats)
                LocalStats.Add(localStat.Key.ToString(), localStat.Value);

            LocationBottomRight = new Vector(item.LocationBottomRight.X, item.LocationBottomRight.Y);
            LocationTopLeft = new Vector(item.LocationTopLeft.X, item.LocationTopLeft.Y);
            MapLevel = item.ItemLevel;
            MaxChaosDamage = item.MaxChaosDamage;
            MaxColdDamage = item.MaxColdDamage;
            MaxDamage = item.MaxDamage;
            MaxDps = item.MaxDps;
            MaxElementalDamage = item.MaxElementalDamage;
            MaxFireDamage = item.MaxFireDamage;
            MaxLightningDamage = item.MaxLightningDamage;
            MaxLinkCount = item.MaxLinkCount;
            MaxPhysicalDamage = item.MaxPhysicalDamage;
            MaxQuality = item.MaxQuality;
            MaxStackCount = item.MaxStackCount;
            //MicrotransactionAttachments = new List<string>(item.MicrotransactionAttachments);
            MinChaosDamage = item.MinChaosDamage;
            MinColdDamage = item.MinColdDamage;
            MinDamage = item.MinDamage;
            MinDps = item.MinDps;
            MinElementalDamage = item.MinElementalDamage;
            MinFireDamage = item.MinFireDamage;
            MinLightningDamage = item.MinLightningDamage;
            MinPhysicalDamage = item.MinPhysicalDamage;
            Name = item.Name;
            Quality = item.Quality;
            Rarity = item.Rarity;
            RequiredDex = item.RequiredDex;
            RequiredInt = item.RequiredInt;
            RequiredLevel = item.RequiredLevel;
            RequiredStr = item.RequiredStr;
            Size = new Vector(item.Size.X, item.Size.Y);
            SocketCount = item.SocketCount;
            StackCount = item.StackCount;
            Stats = new Dictionary<string, int>();
            foreach (var stat in item.Stats)
                Stats.Add(stat.Key.ToString(), stat.Value);

            Tags = new List<string>(item.Tags);
            Type = item.ItemType.ToString().Replace('/', ' ');
            IsDivinationCardType = item.IsDivinationCardType;
        }
    }

    public class CachedModAffix
    {
        public string Category { get; set; }
        public string DisplayName { get; set; }
        public string InternalName { get; set; }
        public bool IsHidden { get; set; }
        public bool IsPrefix { get; set; }
        public bool IsSuffix { get; set; }
        public int Level { get; set; }
        public List<StatContainer> Stats { get; set; }
        public List<int> Values { get; set; }

        public CachedModAffix(ModAffix modAffix)
        {
            Category = modAffix.Category;
            DisplayName = modAffix.DisplayName;
            InternalName = modAffix.InternalName;
            IsHidden = modAffix.IsHidden;
            IsPrefix = modAffix.IsPrefix;
            IsSuffix = modAffix.IsSuffix;
            Level = modAffix.Level;
            Stats = new List<StatContainer>();
            for (int i = 0; i < modAffix.Stats.Length; i++)
            {
                Stats.Add(new StatContainer(modAffix.Stats[i]));
            }
            Values = modAffix.Values;
        }
        public class StatContainer
        {
            public int Max { get; set; }
            public int Min { get; set; }
            public string Stat { get; set; }

            public StatContainer(ModAffix.StatContainer statContainer)
            {
                Max = statContainer.Max;
                Min = statContainer.Min;
                Stat = statContainer.Stat.ToString();
            }
        }
    }
}
