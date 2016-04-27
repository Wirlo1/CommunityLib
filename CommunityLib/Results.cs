namespace CommunityLib
{
    public static class Results
    {
        public enum ClearCursorResults
        {
            None,
            InventoryNotOpened,
            NoSpaceInInventory,
            MaxTriesReached
        }

        public enum NotificationResult
        {
            None,
            ApiKeyError,
            TokenError,
            CredentialsError,
            WebRequestError,
            Bullshit
        }

        public enum FindItemInTabResult
        {
            None,
            GuiNotOpened,
            SwitchToTabFailed,
            GoToFirstTabFailed,
            GoToLastTabFailed,
            ItemFoundInTab,
            ItemNotFoundInTab,
        }

        /// <summary>
        /// Errors for the FastGoToHideOutFunction
        /// </summary>
        public enum FastGoToHideoutResult
        {
            /// <summary>Function ran succesfully. The bot is in hideout.</summary>
            None,
            /// <summary>You can't go to hideout using this function from outside the town</summary>
            NotInTown,
            NotInGame,
            NoHideout,
            TimeOut,
        }

        /// <summary>
        /// Errors for the OpenStash function.
        /// </summary>
        public enum OpenStashError
        {
            /// <summary>None, the stash has been interacted with and the stash panel is opened.</summary>
            None,
            /// <summary>There was an error moving to stash.</summary>
            CouldNotMoveToStash,
            /// <summary>No stash object was detected.</summary>
            NoStash,
            /// <summary>Interaction with the stash failed.</summary>
            InteractFailed,
            /// <summary>The stash panel did not open.</summary>
            StashPanelDidNotOpen,
        }

        /// <summary>
        /// Errors for the TalkToNpc function.
        /// </summary>
        public enum TalkToNpcError
        {
            /// <summary>None, the npc has been interacted with and the npc dialog panel is opened.</summary>
            None,
            /// <summary>There was an error moving to the npc.</summary>
            CouldNotMoveToNpc,
            /// <summary>No waypoint object was detected.</summary>
            NoNpc,
            /// <summary>Interaction with the npc failed.</summary>
            InteractFailed,
            /// <summary>The npc's dialog panel did not open.</summary>
            NpcDialogPanelDidNotOpen,
        }

        /// <summary>
        /// Errors for the OpenWaypoint function.
        /// </summary>
        public enum OpenWaypointError
        {
            /// <summary>None, the waypoint has been interacted with and the world panel is opened.</summary>
            None,
            /// <summary>There was an error moving to the waypoint.</summary>
            CouldNotMoveToWaypoint,
            /// <summary>No waypoint object was detected.</summary>
            NoWaypoint,
            /// <summary>Interaction with the waypoint failed.</summary>
            InteractFailed,
            /// <summary>The world panel did not open.</summary>
            WorldPanelDidNotOpen,
        }

        /// <summary>
        /// Errors for the TakeWaypointTo coroutine.
        /// </summary>
        public enum TakeAreaTransitionError
        {
            /// <summary>None, the area transition has been taken to the desired area.</summary>
            None,

            /// <summary>No area transition object was detected.</summary>
            NoAreaTransitions,

            /// <summary>Interaction with the area transition failed.</summary>
            InteractFailed,

            /// <summary>The instance manager panel did not open.</summary>
            InstanceManagerDidNotOpen,

            /// <summary>The JoinNew function failed with an error.</summary>
            JoinNewFailed,

            /// <summary>An area change did not happen after taking the area transition.</summary>
            WaitForAreaChangeFailed,

            /// <summary>Too many instances are listed based on user configuration.</summary>
            TooManyInstances,
        }

    }
}
