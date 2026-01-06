using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RimWorldAccess
{
    /// <summary>
    /// Manages keyboard navigation for building inspection and ITabs.
    /// Handles switching between different inspect tabs and opening tab-specific menus.
    /// </summary>
    public static class BuildingInspectState
    {
        private static Thing selectedBuilding = null;
        private static List<InspectTabBase> availableTabs = null;
        private static int selectedTabIndex = 0;
        private static bool isActive = false;

        public static bool IsActive => isActive;
        public static Thing SelectedBuilding => selectedBuilding;

        /// <summary>
        /// Opens the building inspect menu for the given building.
        /// </summary>
        public static void Open(Thing building)
        {
            if (building == null)
            {
                TolkHelper.Speak("No building to inspect");
                return;
            }

            selectedBuilding = building;
            isActive = true;

            // Get all available tabs for this building
            availableTabs = building.GetInspectTabs().ToList();
            selectedTabIndex = 0;

            if (availableTabs.Count == 0)
            {
                TolkHelper.Speak($"{building.LabelCap} - No available tabs");
                Close();
                return;
            }

            // Announce the building and first tab
            string announcement = $"Inspecting: {building.LabelCap}";
            if (availableTabs.Count > 0)
            {
                announcement += $" - Tab: {availableTabs[selectedTabIndex].labelKey.Translate()}";
            }
            TolkHelper.Speak(announcement);
        }

        /// <summary>
        /// Closes the building inspect menu.
        /// </summary>
        public static void Close()
        {
            selectedBuilding = null;
            availableTabs = null;
            selectedTabIndex = 0;
            isActive = false;
        }

        /// <summary>
        /// Selects the next tab.
        /// </summary>
        public static void SelectNextTab()
        {
            if (availableTabs == null || availableTabs.Count == 0)
                return;

            selectedTabIndex = MenuHelper.SelectNext(selectedTabIndex, availableTabs.Count);
            AnnounceCurrentTab();
        }

        /// <summary>
        /// Selects the previous tab.
        /// </summary>
        public static void SelectPreviousTab()
        {
            if (availableTabs == null || availableTabs.Count == 0)
                return;

            selectedTabIndex = MenuHelper.SelectPrevious(selectedTabIndex, availableTabs.Count);
            AnnounceCurrentTab();
        }

        /// <summary>
        /// Opens the currently selected tab's menu (if supported).
        /// Uses TabRegistry to determine how to handle each tab type.
        /// </summary>
        public static void OpenCurrentTab()
        {
            if (availableTabs == null || selectedTabIndex < 0 || selectedTabIndex >= availableTabs.Count)
                return;

            InspectTabBase currentTab = availableTabs[selectedTabIndex];
            TabHandlerType handlerType = TabRegistry.GetHandlerForTab(currentTab);
            string tabName = TabRegistry.GetCategoryNameForTab(currentTab);

            switch (handlerType)
            {
                case TabHandlerType.Action:
                    // Try to open the action menu for this tab
                    if (TryOpenTabAction(currentTab))
                        return;
                    // If action failed, fall through to provide feedback
                    TolkHelper.Speak($"Could not open {tabName} menu");
                    break;

                case TabHandlerType.RichNavigation:
                    // Tab has rich navigation - suggest using windowless inspection
                    TolkHelper.Speak($"Tab {tabName} has rich navigation. Press I to inspect with keyboard navigation.");
                    break;

                case TabHandlerType.BasicInspectString:
                    // Tab uses basic fallback - show basic info
                    if (selectedBuilding is Thing thing)
                    {
                        string info = TabRegistry.GetFallbackInfo(thing, currentTab);
                        if (!string.IsNullOrEmpty(info))
                        {
                            // Strip tags and announce first part
                            info = info.StripTags();
                            // Limit to reasonable length for speech
                            if (info.Length > 300)
                                info = info.Substring(0, 300) + "...";
                            TolkHelper.Speak($"{tabName}: {info}");
                        }
                        else
                        {
                            TolkHelper.Speak($"Tab {tabName} has no accessible information. Press I for full inspection.");
                        }
                    }
                    break;

                default:
                    TolkHelper.Speak($"Tab {tabName} not yet supported for keyboard access");
                    break;
            }
        }

        /// <summary>
        /// Tries to open an action for a specific tab type.
        /// Returns true if successful, false otherwise.
        /// </summary>
        private static bool TryOpenTabAction(InspectTabBase tab)
        {
            // Bills tab
            if (tab is ITab_Bills)
            {
                if (selectedBuilding is IBillGiver billGiver)
                {
                    IntVec3 pos = selectedBuilding.Position;
                    Close();
                    BillsMenuState.Open(billGiver, pos);
                    return true;
                }
            }

            // Storage tab
            if (tab.GetType().Name == "ITab_Storage" || tab.GetType().Name == "ITab_BiosculpterNutritionStorage")
            {
                if (selectedBuilding is IStoreSettingsParent storageParent)
                {
                    StorageSettings settings = storageParent.GetStoreSettings();
                    if (settings != null)
                    {
                        Close();
                        StorageSettingsMenuState.Open(settings);
                        return true;
                    }
                }
            }

            // Shells tab (ammunition storage)
            if (tab.GetType().Name == "ITab_Shells")
            {
                if (selectedBuilding is IStoreSettingsParent shellStorage)
                {
                    StorageSettings settings = shellStorage.GetStoreSettings();
                    if (settings != null)
                    {
                        Close();
                        StorageSettingsMenuState.Open(settings);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Opens settings for the building directly without going through tabs.
        /// Used for buildings with simple settings like temperature control.
        /// </summary>
        public static void OpenBuildingSettings()
        {
            if (selectedBuilding == null)
                return;

            // First, try to open the current tab (bills, storage, etc.)
            // This way Enter key works for common use cases like bills menu
            if (TryOpenCurrentTab())
            {
                return;
            }

            // Check if building is a bed
            if (selectedBuilding is Building_Bed bed)
            {
                // Close building inspect and open bed assignment menu
                Close();
                BedAssignmentState.Open(bed);
                return;
            }

            // Check if building has temperature control
            if (selectedBuilding is Building building)
            {
                CompTempControl tempControl = building.TryGetComp<CompTempControl>();
                if (tempControl != null)
                {
                    // Close building inspect and open temperature control menu
                    Close();
                    TempControlMenuState.Open(building);
                    return;
                }
            }

            // Check if building is a plant grower (hydroponics basin, etc.)
            if (selectedBuilding is IPlantToGrowSettable plantGrower)
            {
                // Close building inspect and open plant selection menu
                Close();
                PlantSelectionMenuState.Open(plantGrower);
                return;
            }

            // If no recognized settings, announce
            TolkHelper.Speak($"{selectedBuilding.LabelCap} has no keyboard-accessible settings");
        }

        /// <summary>
        /// Tries to open the current tab. Returns true if successful, false otherwise.
        /// </summary>
        private static bool TryOpenCurrentTab()
        {
            if (availableTabs == null || selectedTabIndex < 0 || selectedTabIndex >= availableTabs.Count)
                return false;

            InspectTabBase currentTab = availableTabs[selectedTabIndex];
            return TryOpenTabAction(currentTab);
        }

        /// <summary>
        /// Announces the currently selected tab using TabRegistry for the label.
        /// </summary>
        private static void AnnounceCurrentTab()
        {
            if (availableTabs == null || selectedTabIndex >= availableTabs.Count)
                return;

            InspectTabBase currentTab = availableTabs[selectedTabIndex];
            string tabName = TabRegistry.GetCategoryNameForTab(currentTab);
            bool isKnown = TabRegistry.IsKnownTab(currentTab.GetType());

            // Include position info
            string position = MenuHelper.FormatPosition(selectedTabIndex, availableTabs.Count);

            // Build announcement
            string announcement = $"Tab: {tabName}";
            if (!string.IsNullOrEmpty(position))
                announcement += $" {position}";

            // Add hint for unknown tabs
            if (!isKnown)
                announcement += " (unrecognized tab)";

            TolkHelper.Speak(announcement);
        }
    }
}
