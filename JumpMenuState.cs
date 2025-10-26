using System.Collections.Generic;
using Verse;
using RimWorld;
using static RimWorldAccess.JumpMenuHelper;

namespace RimWorldAccess
{
    /// <summary>
    /// Manages the windowless jump menu state for navigating to items on the map.
    /// Provides hierarchical navigation through categories and items sorted by distance.
    /// </summary>
    public static class JumpMenuState
    {
        private static bool isActive = false;
        private static List<JumpMenuCategory> categories = null;
        private static int currentIndex = 0; // Index in the flat navigation list

        /// <summary>
        /// Gets whether the jump menu is currently active.
        /// </summary>
        public static bool IsActive => isActive;

        /// <summary>
        /// Opens the jump menu and collects all items on the map.
        /// </summary>
        public static void Open()
        {
            if (Find.CurrentMap == null)
            {
                ClipboardHelper.CopyToClipboard("No map available");
                return;
            }

            if (!MapNavigationState.IsInitialized)
            {
                ClipboardHelper.CopyToClipboard("Map navigation not initialized");
                return;
            }

            // Collect items from the map
            IntVec3 cursorPos = MapNavigationState.CurrentCursorPosition;
            categories = JumpMenuHelper.CollectMapItems(Find.CurrentMap, cursorPos);

            if (categories.Count == 0)
            {
                ClipboardHelper.CopyToClipboard("No items found on map");
                return;
            }

            isActive = true;
            currentIndex = 0;

            // Announce the first category
            AnnounceCurrentSelection();
        }

        /// <summary>
        /// Closes the jump menu.
        /// </summary>
        public static void Close()
        {
            isActive = false;
            categories = null;
            currentIndex = 0;
        }

        /// <summary>
        /// Moves selection to the next visible item (category or item in expanded category).
        /// </summary>
        public static void SelectNext()
        {
            if (categories == null || categories.Count == 0)
                return;

            List<object> flatList = BuildFlatNavigationList();
            if (flatList.Count == 0)
                return;

            currentIndex = (currentIndex + 1) % flatList.Count;
            AnnounceCurrentSelection();
        }

        /// <summary>
        /// Moves selection to the previous visible item (category or item in expanded category).
        /// </summary>
        public static void SelectPrevious()
        {
            if (categories == null || categories.Count == 0)
                return;

            List<object> flatList = BuildFlatNavigationList();
            if (flatList.Count == 0)
                return;

            currentIndex = (currentIndex - 1 + flatList.Count) % flatList.Count;
            AnnounceCurrentSelection();
        }

        /// <summary>
        /// Expands the currently selected category if it's a category.
        /// </summary>
        public static void ExpandCategory()
        {
            if (categories == null || categories.Count == 0)
                return;

            List<object> flatList = BuildFlatNavigationList();
            if (currentIndex < 0 || currentIndex >= flatList.Count)
                return;

            object selected = flatList[currentIndex];
            if (selected is JumpMenuCategory category)
            {
                if (!category.IsExpanded && category.Items.Count > 0)
                {
                    category.IsExpanded = true;
                    // Move to the first item in the expanded category
                    currentIndex++;
                    AnnounceCurrentSelection();
                }
                else if (category.IsExpanded)
                {
                    ClipboardHelper.CopyToClipboard("Already expanded");
                }
                else
                {
                    ClipboardHelper.CopyToClipboard("Category is empty");
                }
            }
            else
            {
                ClipboardHelper.CopyToClipboard("Not a category");
            }
        }

        /// <summary>
        /// Collapses the currently selected category or the parent category of the selected item.
        /// </summary>
        public static void CollapseCategory()
        {
            if (categories == null || categories.Count == 0)
                return;

            List<object> flatList = BuildFlatNavigationList();
            if (currentIndex < 0 || currentIndex >= flatList.Count)
                return;

            object selected = flatList[currentIndex];

            if (selected is JumpMenuCategory category)
            {
                // Collapse the selected category
                if (category.IsExpanded)
                {
                    category.IsExpanded = false;
                    AnnounceCurrentSelection();
                }
                else
                {
                    ClipboardHelper.CopyToClipboard("Already collapsed");
                }
            }
            else if (selected is JumpMenuItem item)
            {
                // Find and collapse the parent category
                JumpMenuCategory parentCategory = FindParentCategory(item);
                if (parentCategory != null)
                {
                    parentCategory.IsExpanded = false;
                    // Move selection back to the parent category
                    currentIndex = FindCategoryIndex(parentCategory);
                    AnnounceCurrentSelection();
                }
            }
        }

        /// <summary>
        /// Jumps the cursor to the currently selected item and closes the menu.
        /// </summary>
        public static void JumpToSelected()
        {
            if (categories == null || categories.Count == 0)
                return;

            List<object> flatList = BuildFlatNavigationList();
            if (currentIndex < 0 || currentIndex >= flatList.Count)
                return;

            object selected = flatList[currentIndex];

            if (selected is JumpMenuItem item)
            {
                // Jump to the item's position
                IntVec3 targetPosition = item.Thing.Position;

                // Update map navigation state
                MapNavigationState.CurrentCursorPosition = targetPosition;

                // Jump camera to position
                Find.CameraDriver.JumpToCurrentMapLoc(targetPosition);

                // Close the menu
                Close();

                // Announce the jump
                ClipboardHelper.CopyToClipboard($"Jumped to {item.Label} at {targetPosition.x}, {targetPosition.z}");
            }
            else if (selected is JumpMenuCategory category)
            {
                ClipboardHelper.CopyToClipboard("Select an item, not a category");
            }
        }

        /// <summary>
        /// Builds a flat list of all visible items for navigation.
        /// Includes categories and items in expanded categories.
        /// </summary>
        private static List<object> BuildFlatNavigationList()
        {
            List<object> flatList = new List<object>();

            if (categories == null)
                return flatList;

            foreach (var category in categories)
            {
                // Add the category itself
                flatList.Add(category);

                // If expanded, add all items in the category
                if (category.IsExpanded)
                {
                    foreach (var item in category.Items)
                    {
                        flatList.Add(item);
                    }
                }
            }

            return flatList;
        }

        /// <summary>
        /// Announces the currently selected item or category to the clipboard.
        /// </summary>
        private static void AnnounceCurrentSelection()
        {
            List<object> flatList = BuildFlatNavigationList();
            if (currentIndex < 0 || currentIndex >= flatList.Count)
                return;

            object selected = flatList[currentIndex];

            if (selected is JumpMenuCategory category)
            {
                string expandedState = category.IsExpanded ? "expanded" : "collapsed";
                string announcement = $"{category.Name} ({category.Items.Count} items) - {expandedState}";
                ClipboardHelper.CopyToClipboard(announcement);
            }
            else if (selected is JumpMenuItem item)
            {
                string announcement = $"{item.Label} - {item.Distance:F1} tiles away";
                ClipboardHelper.CopyToClipboard(announcement);
            }
        }

        /// <summary>
        /// Finds the parent category of a given item.
        /// </summary>
        private static JumpMenuCategory FindParentCategory(JumpMenuItem item)
        {
            if (categories == null)
                return null;

            foreach (var category in categories)
            {
                if (category.Items.Contains(item))
                    return category;
            }

            return null;
        }

        /// <summary>
        /// Finds the index of a category in the flat navigation list.
        /// </summary>
        private static int FindCategoryIndex(JumpMenuCategory targetCategory)
        {
            List<object> flatList = BuildFlatNavigationList();

            for (int i = 0; i < flatList.Count; i++)
            {
                if (flatList[i] is JumpMenuCategory category && category == targetCategory)
                    return i;
            }

            return 0; // Default to first item if not found
        }
    }
}
