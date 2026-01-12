using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimWorldAccess
{
    /// <summary>
    /// Stateless helper for keyboard-based object selection.
    /// Delegates to RimWorld's native Selector system.
    /// All selection state lives in Find.Selector.
    /// </summary>
    public static class SelectionHelper
    {
        /// <summary>
        /// Selects the object at the cursor position.
        /// If called repeatedly at the same position, cycles through overlapping objects.
        /// </summary>
        /// <param name="cursorPosition">The map position to select at</param>
        /// <param name="map">The current map</param>
        /// <param name="additive">If true, adds to selection instead of replacing</param>
        /// <returns>True if selection changed, false otherwise</returns>
        public static bool SelectAtPosition(IntVec3 cursorPosition, Map map, bool additive)
        {
            if (map == null || !cursorPosition.IsValid || !cursorPosition.InBounds(map))
            {
                TolkHelper.Speak("Invalid position");
                return false;
            }

            // Get all selectable objects at this position using RimWorld's native method
            List<object> selectableObjects = Selector.SelectableObjectsAt(cursorPosition, map).ToList();

            // Sort to match RimWorld's mouse selection behavior:
            // Building_Storage (shelves) are sorted FIRST, then everything else
            // This allows selecting shelves before the items stored on them
            selectableObjects = selectableObjects.OrderBy(x => (x is Building_Storage) ? 0 : 1).ToList();

            if (selectableObjects.Count == 0)
            {
                if (!additive)
                {
                    // Clear selection when clicking empty space (matches mouse behavior)
                    if (Find.Selector.NumSelected > 0)
                    {
                        SelectionNotificationPatch.SuppressNextAnnouncement();
                        Find.Selector.ClearSelection();
                        TolkHelper.Speak("Deselected");
                        return true;
                    }
                }
                TolkHelper.Speak("Nothing selectable here");
                return false;
            }

            if (additive)
            {
                return HandleAdditiveSelection(selectableObjects);
            }
            else
            {
                return HandleNormalSelection(selectableObjects, cursorPosition, map);
            }
        }

        /// <summary>
        /// Handles normal selection (without Shift key).
        /// Uses RimWorld's SelectNextAt for cycling through overlapping objects.
        /// </summary>
        private static bool HandleNormalSelection(List<object> selectableObjects, IntVec3 position, Map map)
        {
            // Check if we already have exactly one object selected at this position
            // If so, use RimWorld's cycling mechanism
            if (Find.Selector.NumSelected == 1)
            {
                object currentlySelected = Find.Selector.SingleSelectedObject;
                if (currentlySelected != null && selectableObjects.Contains(currentlySelected))
                {
                    // Use RimWorld's native cycling - it handles the index wrapping
                    Find.Selector.SelectNextAt(position, map);

                    object newSelection = Find.Selector.SingleSelectedObject;
                    string label = GetObjectLabel(newSelection);

                    if (selectableObjects.Count > 1)
                    {
                        int currentIndex = selectableObjects.IndexOf(newSelection);
                        TolkHelper.Speak($"Selected {label}, {currentIndex + 1} of {selectableObjects.Count}");
                    }
                    else
                    {
                        TolkHelper.Speak($"Selected {label}");
                    }
                    return true;
                }
            }

            // First time selecting at this position - select the first object
            SelectionNotificationPatch.SuppressNextAnnouncement();
            Find.Selector.ClearSelection();
            object firstObject = selectableObjects[0];
            SelectionNotificationPatch.SuppressNextAnnouncement();
            Find.Selector.Select(firstObject, playSound: false, forceDesignatorDeselect: false);

            string firstLabel = GetObjectLabel(firstObject);
            if (selectableObjects.Count > 1)
            {
                TolkHelper.Speak($"Selected {firstLabel}, 1 of {selectableObjects.Count}");
            }
            else
            {
                TolkHelper.Speak($"Selected {firstLabel}");
            }
            return true;
        }

        /// <summary>
        /// Handles additive selection (with Shift key).
        /// Toggles selection of objects at the position.
        /// </summary>
        private static bool HandleAdditiveSelection(List<object> selectableObjects)
        {
            // Check if any object at this position is already selected
            object alreadySelected = selectableObjects.FirstOrDefault(obj => Find.Selector.IsSelected(obj));

            if (alreadySelected != null)
            {
                // Deselect - matches RimWorld's Shift+click behavior
                SelectionNotificationPatch.SuppressNextAnnouncement();
                Find.Selector.Deselect(alreadySelected);

                int remaining = Find.Selector.NumSelected;
                if (remaining > 0)
                {
                    TolkHelper.Speak($"Deselected, {remaining} remaining");
                }
                else
                {
                    TolkHelper.Speak("Deselected");
                }
                return true;
            }
            else
            {
                // Add first object at position to selection
                object toSelect = selectableObjects[0];
                SelectionNotificationPatch.SuppressNextAnnouncement();
                Find.Selector.Select(toSelect, playSound: false, forceDesignatorDeselect: false);

                string label = GetObjectLabel(toSelect);
                int total = Find.Selector.NumSelected;
                TolkHelper.Speak($"Added {label}, {total} selected");
                return true;
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public static void ClearSelection()
        {
            if (Find.Selector.NumSelected > 0)
            {
                SelectionNotificationPatch.SuppressNextAnnouncement();
                Find.Selector.ClearSelection();
                TolkHelper.Speak("Deselected all");
            }
        }

        /// <summary>
        /// Gets a human-readable label for a selectable object.
        /// </summary>
        /// <param name="obj">The object to label</param>
        /// <returns>A concise label for screen reader announcement</returns>
        public static string GetObjectLabel(object obj)
        {
            if (obj == null)
                return "Unknown";

            if (obj is Pawn pawn)
            {
                // For pawns, use their short name
                string label = pawn.LabelShort;

                // Add suffix for special pawn types
                string suffix = TileInfoHelper.GetPawnSuffix(pawn);
                if (!string.IsNullOrEmpty(suffix))
                {
                    label += suffix;
                }
                return label;
            }

            if (obj is Thing thing)
            {
                // For items, include stack count if > 1
                string label = thing.LabelCapNoCount;
                if (thing.stackCount > 1)
                {
                    label += $" x{thing.stackCount}";
                }
                return label;
            }

            if (obj is Zone zone)
            {
                return zone.label ?? "Zone";
            }

            if (obj is Plan plan)
            {
                return plan.RenamableLabel ?? "Plan";
            }

            return obj.ToString();
        }

        /// <summary>
        /// Gets a summary of the current selection for announcement.
        /// </summary>
        /// <returns>A summary string describing the current selection</returns>
        public static string GetSelectionSummary()
        {
            int count = Find.Selector.NumSelected;

            if (count == 0)
                return "Nothing selected";

            if (count == 1)
            {
                object selected = Find.Selector.SingleSelectedObject;
                return $"Selected: {GetObjectLabel(selected)}";
            }

            // Multiple selection - count by type
            var pawns = Find.Selector.SelectedPawns;
            int pawnCount = pawns.Count;
            int otherCount = count - pawnCount;

            if (pawnCount > 0 && otherCount == 0)
            {
                return $"{pawnCount} pawns selected";
            }
            else if (pawnCount == 0 && otherCount > 0)
            {
                return $"{otherCount} objects selected";
            }
            else
            {
                return $"{pawnCount} pawns and {otherCount} other objects selected";
            }
        }
    }
}
