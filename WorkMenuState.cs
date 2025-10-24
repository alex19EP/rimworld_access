using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RimWorldAccess
{
    /// <summary>
    /// Manages the state and navigation for the interactive work assignment menu.
    /// Tracks work types, their enabled/disabled states, and pending changes.
    /// </summary>
    public static class WorkMenuState
    {
        private static bool isActive = false;
        private static Pawn currentPawn = null;
        private static int selectedIndex = 0;
        private static List<WorkTypeEntry> workEntries = new List<WorkTypeEntry>();

        // Track changes made during the menu session
        private static Dictionary<WorkTypeDef, bool> pendingChanges = new Dictionary<WorkTypeDef, bool>();

        public static bool IsActive => isActive;
        public static int SelectedIndex => selectedIndex;
        public static List<WorkTypeEntry> WorkEntries => workEntries;
        public static Pawn CurrentPawn => currentPawn;

        /// <summary>
        /// Opens the work menu for the specified pawn.
        /// </summary>
        public static void Open(Pawn pawn)
        {
            if (pawn == null || pawn.workSettings == null)
                return;

            isActive = true;
            currentPawn = pawn;
            selectedIndex = 0;
            pendingChanges.Clear();

            // Build the list of work types
            workEntries.Clear();
            var allWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;

            foreach (var workType in allWorkTypes)
            {
                if (workType.visible)
                {
                    bool isDisabled = pawn.WorkTypeIsDisabled(workType);
                    bool isEnabled = !isDisabled && pawn.workSettings.WorkIsActive(workType);

                    workEntries.Add(new WorkTypeEntry
                    {
                        WorkType = workType,
                        IsDisabled = isDisabled,
                        IsEnabled = isEnabled,
                        OriginalState = isEnabled
                    });
                }
            }

            // Sort by label for easier navigation
            workEntries = workEntries.OrderBy(e => e.WorkType.labelShort).ToList();

            // Announce menu opened
            UpdateClipboard();
        }

        /// <summary>
        /// Closes the menu without applying changes.
        /// </summary>
        public static void Cancel()
        {
            isActive = false;
            currentPawn = null;
            selectedIndex = 0;
            workEntries.Clear();
            pendingChanges.Clear();

            ClipboardHelper.CopyToClipboard("Work menu cancelled");
        }

        /// <summary>
        /// Closes the menu and applies all pending changes to the pawn's work settings.
        /// </summary>
        public static void Confirm()
        {
            if (currentPawn == null || currentPawn.workSettings == null)
            {
                Cancel();
                return;
            }

            int changesApplied = 0;

            foreach (var entry in workEntries)
            {
                if (!entry.IsDisabled && entry.IsEnabled != entry.OriginalState)
                {
                    if (entry.IsEnabled)
                    {
                        // Enable the work type with default priority
                        currentPawn.workSettings.SetPriority(entry.WorkType, 3);
                    }
                    else
                    {
                        // Disable the work type
                        currentPawn.workSettings.SetPriority(entry.WorkType, 0);
                    }
                    changesApplied++;
                }
            }

            string message = changesApplied > 0
                ? $"Applied {changesApplied} work assignment changes for {currentPawn.LabelShort}"
                : $"No changes made for {currentPawn.LabelShort}";

            ClipboardHelper.CopyToClipboard(message);

            isActive = false;
            currentPawn = null;
            selectedIndex = 0;
            workEntries.Clear();
            pendingChanges.Clear();
        }

        /// <summary>
        /// Moves selection up in the list (wraps around).
        /// </summary>
        public static void MoveUp()
        {
            if (workEntries.Count == 0)
                return;

            selectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = workEntries.Count - 1;

            UpdateClipboard();
        }

        /// <summary>
        /// Moves selection down in the list (wraps around).
        /// </summary>
        public static void MoveDown()
        {
            if (workEntries.Count == 0)
                return;

            selectedIndex++;
            if (selectedIndex >= workEntries.Count)
                selectedIndex = 0;

            UpdateClipboard();
        }

        /// <summary>
        /// Toggles the enabled state of the currently selected work type.
        /// Only works if the work type is not permanently disabled.
        /// </summary>
        public static void ToggleSelected()
        {
            if (workEntries.Count == 0 || selectedIndex < 0 || selectedIndex >= workEntries.Count)
                return;

            var entry = workEntries[selectedIndex];

            if (entry.IsDisabled)
            {
                ClipboardHelper.CopyToClipboard($"{entry.WorkType.labelShort}: Disabled - cannot toggle");
                return;
            }

            entry.IsEnabled = !entry.IsEnabled;
            UpdateClipboard();
        }

        /// <summary>
        /// Gets the current selection as a formatted string for screen reader.
        /// </summary>
        private static void UpdateClipboard()
        {
            if (workEntries.Count == 0 || selectedIndex < 0 || selectedIndex >= workEntries.Count)
            {
                ClipboardHelper.CopyToClipboard("No work types available");
                return;
            }

            var entry = workEntries[selectedIndex];
            string status;

            if (entry.IsDisabled)
            {
                status = "Disabled";
            }
            else if (entry.IsEnabled)
            {
                status = entry.OriginalState ? "Selected" : "Selected (pending)";
            }
            else
            {
                status = entry.OriginalState ? "Unselected (pending)" : "Unselected";
            }

            string message = $"{status}: {entry.WorkType.labelShort}";
            ClipboardHelper.CopyToClipboard(message);
        }

        /// <summary>
        /// Represents a work type entry in the menu.
        /// </summary>
        public class WorkTypeEntry
        {
            public WorkTypeDef WorkType { get; set; }
            public bool IsDisabled { get; set; }  // Permanently disabled (cannot be changed)
            public bool IsEnabled { get; set; }    // Current state in the menu
            public bool OriginalState { get; set; } // Original state when menu opened
        }
    }
}
