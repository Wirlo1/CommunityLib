using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Bot.Pathfinding;
using Loki.Common;
using Loki.Game;
using Loki.Game.GameData;
using Loki.Game.Objects;

namespace CommunityLib
{
    public static class LibCoroutines
    {
        /// <summary>
        /// Opens the inventory panel.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> OpenInventoryPanel(int timeout = 10000)
        {
            CommunityLib.Log.DebugFormat("[OpenInventoryPanel]");

            var sw = Stopwatch.StartNew();

            // Make sure we close all blocking windows so we can actually open the inventory.
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                await Coroutines.CloseBlockingWindows();

            // Open the inventory panel like a player would.
            while (!LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                CommunityLib.Log.DebugFormat("[OpenInventoryPanel] The InventoryUi is not opened. Now opening it.");

                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[OpenInventoryPanel] Timeout.");
                    return false;
                }

                if (LokiPoe.Me.IsDead)
                {
                    CommunityLib.Log.ErrorFormat("[OpenInventoryPanel] We are now dead.");
                    return false;
                }

                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.open_inventory_panel, true, false, false);
                await Coroutines.LatencyWait(2);
                await Coroutines.ReactionWait();
            }

            return true;
        }

        /// <summary>
        /// This coroutine interacts with stash and waits for the stash panel to open. When called from a hideout,
        /// the stash must be in spawn range, otherwise the coroutine will fail.
        /// </summary>
        ///<param name="guild">Should the guild stash be opened?</param>
        /// <returns>An OpenStashError that describes the result.</returns>
        public static async Task<Results.OpenStashError> OpenStash(bool guild = false)
        {
            await Coroutines.CloseBlockingWindows();
            await Coroutines.FinishCurrentAction();

            var stash = Stash.DetermineStash(guild);
            if (stash == null)
            {
                if (LokiPoe.Me.IsInHideout)
                    return Results.OpenStashError.NoStash;

                var mtl = await Navigation.MoveToLocation(
                    ExilePather.FastWalkablePositionFor(Actor.GuessStashLocation()), 25, 60000,
                    () => Stash.DetermineStash(guild) != null && Stash.DetermineStash(guild).Distance < 75);

                if (!mtl)
                    return Results.OpenStashError.CouldNotMoveToStash;

                stash = Stash.DetermineStash(guild);
                if (stash == null)
                    return Results.OpenStashError.NoStash;
            }

            if (stash.Distance > 30)
            {
                var p = stash.Position;
                if (!await Navigation.MoveToLocation(ExilePather.FastWalkablePositionFor(p), 25, 15000, () => false))
                    return Results.OpenStashError.CouldNotMoveToStash;
            }

            await Coroutines.FinishCurrentAction();

            stash = Stash.DetermineStash(guild);
            if (stash == null)
                return Results.OpenStashError.NoStash;

            if (!await InteractWith(stash))
                return Results.OpenStashError.InteractFailed;

            if (guild)
            {
                if (!await Dialog.WaitForPanel(Dialog.PanelType.GuildStash))
                    return Results.OpenStashError.StashPanelDidNotOpen;

                await Stash.WaitForStashTabChange(guild: true);
            }
            else
            {
                if (!await Dialog.WaitForPanel(Dialog.PanelType.Stash))
                    return Results.OpenStashError.StashPanelDidNotOpen;

                await Stash.WaitForStashTabChange();
            }

            return Results.OpenStashError.None;
        }

        /// <summary>
        /// This coroutine interacts with the waypoint and waits for the world panel to open. When called from a hideout,
        /// the waypoint must be in spawn range, otherwise the coroutine will fail. The movement is done without returning,
        /// so this should be carefully used when not in town.
        /// </summary>
        /// <returns>An OpenStashError that describes the result.</returns>
        public static async Task<Results.OpenWaypointError> OpenWaypoint()
        {
            await Coroutines.CloseBlockingWindows();

            await Coroutines.FinishCurrentAction();

            var waypoint = LokiPoe.ObjectManager.Waypoint;
            if (waypoint == null)
            {
                if (!LokiPoe.Me.IsInTown)
                {
                    return Results.OpenWaypointError.NoWaypoint;
                }

                if (
                    !await
                        Navigation.MoveToLocation(ExilePather.FastWalkablePositionFor(Actor.GuessWaypointLocation()), 25,
                            60000, () => LokiPoe.ObjectManager.Waypoint != null))
                {
                    return Results.OpenWaypointError.CouldNotMoveToWaypoint;
                }

                waypoint = LokiPoe.ObjectManager.Waypoint;
                if (waypoint == null)
                {
                    return Results.OpenWaypointError.NoWaypoint;
                }
            }

            if (ExilePather.PathDistance(LokiPoe.MyPosition, waypoint.Position) > 30)
            {
                if (!await Navigation.MoveToLocation(ExilePather.FastWalkablePositionFor(waypoint.Position), 25, 15000, () => false))
                {
                    return Results.OpenWaypointError.CouldNotMoveToWaypoint;
                }
            }

            await Coroutines.FinishCurrentAction();

            waypoint = LokiPoe.ObjectManager.Waypoint;
            if (waypoint == null)
            {
                return Results.OpenWaypointError.NoWaypoint;
            }

            if (!await InteractWith(waypoint))
                return Results.OpenWaypointError.InteractFailed;

            if (!await WaitForWorldPanel())
                return Results.OpenWaypointError.WorldPanelDidNotOpen;

            await Coroutine.Sleep(1000); // Adding this in to let the gui load more

            return Results.OpenWaypointError.None;
        }

        /// <summary>
        /// This coroutine waits for the world panel to open after clicking on a waypoint.
        /// </summary>
        /// <param name="timeout">How long to wait before the coroutine fails.</param>
        /// <returns>true on succes and false on failure.</returns>
        public static async Task<bool> WaitForWorldPanel(int timeout = 10000)
        {
            CommunityLib.Log.DebugFormat("[WaitForWorldPanel]");

            var sw = Stopwatch.StartNew();

            while (!LokiPoe.InGameState.WorldUi.IsOpened)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[WaitForWorldPanel] Timeout.");
                    return false;
                }

                CommunityLib.Log.DebugFormat("[WaitForWorldPanel] We have been waiting {0} for the world panel to open.", sw.Elapsed);

                await Coroutines.ReactionWait();
            }

            return true;
        }

        /// <summary>
        /// This coroutine interacts with a npc and waits for the npc dialog panel to open. When called for a non-main town npc 
        /// the npc must be in spawn range, otherwise the coroutine will fail. The movement is done without returning,
        /// so this should be carefully used when not in town.
        /// </summary>
        /// <returns>An OpenStashError that describes the result.</returns>
        public static async Task<Results.TalkToNpcError> TalkToNpc(string name)
        {
            await Coroutines.CloseBlockingWindows();

            await Coroutines.FinishCurrentAction();

            var npc = LokiPoe.ObjectManager.GetObjectByName(name);
            if (npc == null)
            {
                var pos = Actor.GuessNpcLocation(name);

                if (pos == Vector2i.Zero)
                    return Results.TalkToNpcError.NoNpc;

                if (!await Navigation.MoveToLocation(ExilePather.FastWalkablePositionFor(pos), 25, 60000, () => LokiPoe.ObjectManager.GetObjectByName(name) != null))
                    return Results.TalkToNpcError.CouldNotMoveToNpc;

                npc = LokiPoe.ObjectManager.GetObjectByName(name);
                if (npc == null)
                    return Results.TalkToNpcError.NoNpc;
            }

            if (ExilePather.PathDistance(LokiPoe.MyPosition, npc.Position) > 30)
            {
                if (!await Navigation.MoveToLocation(ExilePather.FastWalkablePositionFor(npc.Position), 25, 15000, () => false))
                    return Results.TalkToNpcError.CouldNotMoveToNpc;

                npc = LokiPoe.ObjectManager.GetObjectByName(name);
                if (npc == null)
                    return Results.TalkToNpcError.NoNpc;
            }

            await Coroutines.FinishCurrentAction();

            if (!await InteractWith(npc))
                return Results.TalkToNpcError.InteractFailed;

            if (!await Dialog.WaitForPanel(Dialog.PanelType.NpcDialog))
                return Results.TalkToNpcError.NpcDialogPanelDidNotOpen;

            return Results.TalkToNpcError.None;
        }

        /// <summary>
        /// This coroutine will create a portal to town and take it. If the process fails, the coroutine will logout.
        /// </summary>
        /// <returns></returns>
        public static async Task CreateAndTakePortalToTown()
        {
            if (await CreatePortalToTown())
            {
                if (await TakeClosestPortal())
                    return;
            }

            CommunityLib.Log.ErrorFormat(
                "[CreateAndTakePortalToTown] A portal to town could not be made/taken. Now logging out to get back to town.");

            await LogoutToTitleScreen();
        }

        /// <summary>
        /// This coroutine creates a portal to town from a Portal Scroll in the inventory.
        /// </summary>
        /// <returns>true if the Portal Scroll was used and false otherwise.</returns>
        public static async Task<bool> CreatePortalToTown()
        {
            if (LokiPoe.Me.IsInTown)
            {
                CommunityLib.Log.ErrorFormat("[CreatePortalToTown] Town portals are not allowed in town.");
                return false;
            }

            if (LokiPoe.Me.IsInHideout)
            {
                CommunityLib.Log.ErrorFormat("[CreatePortalToTown] Town portals are not allowed in hideouts.");
                return false;
            }

            if (LokiPoe.CurrentWorldArea.IsMissionArea || LokiPoe.CurrentWorldArea.IsDenArea ||
                LokiPoe.CurrentWorldArea.IsRelicArea || LokiPoe.CurrentWorldArea.IsDailyArea)
            {
                CommunityLib.Log.ErrorFormat("[CreatePortalToTown] Town Portals are not allowed in mission areas.");
                return false;
            }

            await Coroutines.FinishCurrentAction();
            await Coroutines.CloseBlockingWindows();

            var portalSkill = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Portal");
            if (portalSkill != null && portalSkill.CanUse(true))
            {
                CommunityLib.Log.DebugFormat("[CreatePortalToTown] We have a Portal skill on the skill bar. Now using it.");

                var err = LokiPoe.InGameState.SkillBarHud.Use(portalSkill.Slot, false);
                CommunityLib.Log.InfoFormat($"[CreatePortalToTown] SkillBarHud.Use returned {err}.");

                await Coroutines.LatencyWait();
                await Coroutines.FinishCurrentAction();

                if (err == LokiPoe.InGameState.UseResult.None)
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < 3000)
                    {
                        var portal = LokiPoe.ObjectManager.Objects.OfType<Portal>().FirstOrDefault(p => p.Distance < 50);
                        if (portal != null)
                            return true;

                        CommunityLib.Log.DebugFormat("[CreatePortalToTown] No portal was detected yet, waiting...");
                        await Coroutines.LatencyWait();
                    }
                }
            }

            CommunityLib.Log.DebugFormat("[CreatePortalToTown] Now opening the inventory panel.");

            // We need the inventory panel open.
            if (!await OpenInventoryPanel())
                return false;

            await Coroutines.ReactionWait();

            CommunityLib.Log.DebugFormat("[CreatePortalToTown] Now searching the main inventory for a Portal Scroll.");

            var item = LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot(InventorySlot.Main).FirstOrDefault(i => i.Name == "Portal Scroll");
            if (item == null)
            {
                CommunityLib.Log.ErrorFormat("[CreatePortalToTown] There are no Portal Scrolls in the inventory.");
                return false;
            }

            CommunityLib.Log.DebugFormat("[CreatePortalToTown] Now using the Portal Scroll.");

            var err2 = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.UseItem(item.LocalId);
            if (err2 != UseItemResult.None)
            {
                CommunityLib.Log.ErrorFormat($"[CreatePortalToTown] UseItem returned {err2}.");
                return false;
            }

            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();
            await Coroutines.CloseBlockingWindows();

            return true;
        }

        /// <summary>
        /// This coroutine will attempt to take a portal
        /// </summary>
        /// <returns>true if the portal was taken, and an area change occurred, and false otherwise.</returns>
        public static async Task<bool> TakeClosestPortal()
        {
            var sw = Stopwatch.StartNew();

            if (LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
            {
                CommunityLib.Log.InfoFormat("[TakeClosestPortal] Now disabling Always Highlight to avoid label issues.");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
                await Coroutine.Sleep(16);
            }

            NetworkObject portal = null;
            while (portal == null || !portal.IsTargetable)
            {
                CommunityLib.Log.DebugFormat($"[TakeClosestPortal] Now waiting for the portal to spawn. {sw.Elapsed} elapsed.");
                await Coroutines.LatencyWait();

                portal =
                    LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                        .Where(p => p.Distance < 50)
                        .OrderBy(p => p.Distance)
                        .FirstOrDefault();

                if (sw.ElapsedMilliseconds > 10000)
                    break;
            }

            if (portal == null)
            {
                CommunityLib.Log.ErrorFormat("[TakeClosestPortal] A portal was not found.");
                return false;
            }

            var pos = ExilePather.FastWalkablePositionFor(portal);
            CommunityLib.Log.DebugFormat($"[TakeClosestPortal] The portal was found at {pos}.");

            if (!await Navigation.MoveToLocation(pos, 5, 10000, () => false))
                return false;

            var hash = LokiPoe.LocalData.AreaHash;

            // Try to interact 3 times.
            for (var i = 0; i < 3; i++)
            {
                await Coroutines.FinishCurrentAction();

                CommunityLib.Log.DebugFormat($"[TakeClosestPortal] The portal to interact with is {portal.Id} at {pos}.");

                if (await InteractWith(portal))
                {
                    if (await Areas.WaitForAreaChange(hash))
                    {
                        CommunityLib.Log.DebugFormat("[TakeClosestPortal] The portal has been taken.");
                        return true;
                    }
                }

                await Coroutine.Sleep(1000);
            }

            CommunityLib.Log.ErrorFormat("[TakeClosestPortal] We have failed to take the portal 3 times.");
            return false;
        }

        /// <summary>
        /// This coroutine will logout the character to character selection.
        /// </summary>
        /// <returns></returns>
        public static async Task LogoutToTitleScreen()
        {
            CommunityLib.Log.DebugFormat("[LogoutToTitleScreen] Now logging out to Character Selection screen.");

            var err = LokiPoe.EscapeState.LogoutToTitleScreen();
            if (err != LokiPoe.EscapeState.LogoutError.None)
            {
                CommunityLib.Log.ErrorFormat($"[LogoutToTitleScreen] EscapeState.LogoutToTitleScreen returned {err}. Now stopping the bot because it cannot continue.");
                BotManager.Stop();
            }

            var sw = Stopwatch.StartNew();

            while (!LokiPoe.IsInLoginScreen)
            {
                CommunityLib.Log.DebugFormat($"[LogoutToTitleScreen] We have been waiting {sw.Elapsed} to get out of game.");
                await Coroutines.ReactionWait();
            }
        }

        /// <summary>
        /// This coroutine attempts to highlight and interact with an object.
        /// Interaction only takes place if the object is highlighted.
        /// </summary>
        /// <param name="obj">The object to interact with.</param>
        /// <param name="holdCtrl">Should control be held? For area transitions.</param>
        /// <returns>true on success and false on failure.</returns>
        public static async Task<bool> InteractWith(NetworkObject obj, bool holdCtrl = false)
        {
            return await InteractWith<NetworkObject>(obj, holdCtrl);
        }

        /// <summary>
        /// This coroutine attempts to highlight and interact with an object.
        /// Interaction only takes place if the object is highlighted or an object of type T is.
        /// </summary>
        /// <typeparam name="T">The type of object acceptable to be highlighted if the intended target is not highlighted.</typeparam>
        /// <param name="holdCtrl">Should control be held? For area transitions.</param>
        /// <param name="obj">The object to interact with.</param>
        /// <returns>true on success and false on failure.</returns>
        public static async Task<bool> InteractWith<T>(NetworkObject obj, bool holdCtrl = false)
        {
            if (obj == null)
            {
                CommunityLib.Log.ErrorFormat("[InteractWith] The object is null.");
                return false;
            }

            var id = obj.Id;

            CommunityLib.Log.DebugFormat($"[InteractWith] Now attempting to highlight {id}.");
            await Coroutines.FinishCurrentAction();

            if (!LokiPoe.Input.HighlightObject(obj))
            {
                CommunityLib.Log.ErrorFormat("[InteractWith] The target could not be highlighted.");
                return false;
            }

            var target = LokiPoe.InGameState.CurrentTarget;
            if (target != obj && !(target is T))
            {
                CommunityLib.Log.ErrorFormat("[InteractWith] The target highlight has been lost.");
                return false;
            }

            CommunityLib.Log.DebugFormat($"[InteractWith] Now attempting to interact with {id}.");

            if (holdCtrl)
                LokiPoe.ProcessHookManager.SetKeyState(Keys.ControlKey, 0x8000);

            LokiPoe.Input.ClickLMB();
            await Coroutines.LatencyWait();
            await Coroutines.FinishCurrentAction(false);
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            return true;
        }

        /// <summary>
        /// This coroutines waits for the character to change positions from a local area transition.
        /// </summary>
        /// <param name="position">The starting position.</param>
        /// <param name="delta">The change in position required.</param>
        /// <param name="timeout">How long to wait before the coroutine fails.</param>
        /// <returns>true on success, and false on failure.</returns>
        public static async Task<bool> WaitForPositionChange(Vector2i position, int delta = 30, int timeout = 5000)
        {
            CommunityLib.Log.DebugFormat("[WaitForPositionChange]");

            var sw = Stopwatch.StartNew();

            while (LokiPoe.MyPosition.Distance(position) < delta)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[WaitForLargerPositionChange] Timeout.");
                    return false;
                }

                CommunityLib.Log.DebugFormat("[WaitForLargerPositionChange] We have been waiting {0} for an area change.", sw.Elapsed);

                await Coroutines.LatencyWait();
            }

            return true;
        }

        /// <summary>
        /// This coroutine waits for the instance manager to open.
        /// </summary>
        /// <param name="timeout">How long to wait before the coroutine fails.</param>
        /// <returns>true on succes and false on failure.</returns>
        public static async Task<bool> WaitForInstanceManager(int timeout = 1000)
        {
            CommunityLib.Log.DebugFormat("[WaitForInstanceManager]");

            var sw = Stopwatch.StartNew();

            while (!LokiPoe.InGameState.InstanceManagerUi.IsOpened)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[WaitForInstanceManager] Timeout.");
                    return false;
                }

                CommunityLib.Log.DebugFormat("[WaitForInstanceManager] We have been waiting {0} for the instance manager to open.",
                    sw.Elapsed);

                await Coroutines.LatencyWait();
            }

            return true;
        }

        /// <summary>
        /// This coroutine waits for an area transition to be usable.
        /// </summary>
        /// <param name="name">The name of the area transition.</param>
        /// <param name="timeout">How long to wait before the coroutine fails.</param>
        /// <returns>true on succes and false on failure.</returns>
        public static async Task<bool> WaitForAreaTransition(string name, int timeout = 3000)
        {
            CommunityLib.Log.DebugFormat("[WaitForAreaTransition]");

            var sw = Stopwatch.StartNew();

            while (true)
            {
                var at = LokiPoe.ObjectManager.GetObjectByName<AreaTransition>(name);
                if (at != null)
                {
                    if (at.IsTargetable)
                    {
                        break;
                    }
                }

                if (sw.ElapsedMilliseconds > timeout)
                {
                    CommunityLib.Log.ErrorFormat("[WaitForAreaTransition] Timeout.");
                    return false;
                }

                CommunityLib.Log.DebugFormat(
                    "[WaitForAreaTransition] We have been waiting {0} for the area transition {1} to be usable.",
                    sw.Elapsed, name);

                await Coroutines.LatencyWait();
            }

            return true;
        }

        /// <summary>
        /// This coroutine interacts with an area transition in order to change areas. It assumes
        /// you are in interaction range with the area transition itself. It can be used both in town,
        /// and out of town, given the previous conditions are met.
        /// </summary>
        /// <param name="obj">The area transition object to take.</param>
        /// <param name="newInstance">Should a new instance be created.</param>
        /// <param name="isLocal">Is the area transition local? In other words, should the couroutine not wait for an area change.</param>
        /// <param name="maxInstances">The max number of instance entries allowed to Join a new instance or -1 to not check.</param>
        /// <returns>A TakeAreaTransitionError that describes the result.</returns>
        public static async Task<Results.TakeAreaTransitionError> TakeAreaTransition(NetworkObject obj, bool newInstance,
            int maxInstances,
            bool isLocal = false)
        {
            CommunityLib.Log.InfoFormat("[TakeAreaTransition] {0} {1} {2}", obj.Name, newInstance ? "(new instance)" : "",
                isLocal ? "(local)" : "");

            await Coroutines.CloseBlockingWindows();

            await Coroutines.FinishCurrentAction();

            var hash = LokiPoe.LocalData.AreaHash;
            var pos = LokiPoe.MyPosition;

            if (!await InteractWith(obj, newInstance))
                return Results.TakeAreaTransitionError.InteractFailed;

            if (newInstance)
            {
                if (!await WaitForInstanceManager(5000))
                {
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();

                    return Results.TakeAreaTransitionError.InstanceManagerDidNotOpen;
                }

                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                await Coroutines.LatencyWait();

                await Coroutine.Sleep(1000); // Let the gui stay open a bit before clicking too fast.

                if (LokiPoe.InGameState.InstanceManagerUi.InstanceCount >= maxInstances)
                {
                    return Results.TakeAreaTransitionError.TooManyInstances;
                }

                var nierr = LokiPoe.InGameState.InstanceManagerUi.JoinNewInstance();
                if (nierr != LokiPoe.InGameState.JoinInstanceResult.None)
                {
                    CommunityLib.Log.ErrorFormat("[TakeAreaTransition] InstanceManagerUi.JoinNew returned {0}.", nierr);
                    return Results.TakeAreaTransitionError.JoinNewFailed;
                }

                // Wait for the action to take place first.
                await Coroutines.LatencyWait();

                await Coroutines.ReactionWait();
            }

            if (isLocal)
            {
                if (!await WaitForPositionChange(pos))
                {
                    CommunityLib.Log.ErrorFormat("[TakeAreaTransition] WaitForPositionChange failed.");
                    return Results.TakeAreaTransitionError.WaitForAreaChangeFailed;
                }
            }
            else
            {
                if (!await Areas.WaitForAreaChange(hash))
                {
                    CommunityLib.Log.ErrorFormat("[TakeAreaTransition] WaitForAreaChange failed.");
                    return Results.TakeAreaTransitionError.WaitForAreaChangeFailed;
                }
            }

            return Results.TakeAreaTransitionError.None;
        }
    }
}
