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

        public enum FastGoToHideoutResult
        {
            None,
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
    }
}
