using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimWorldAccess
{
    /// <summary>
    /// Patches RimWorld's Selector to announce selection changes when they occur from
    /// gizmos or other game actions (not from our keyboard selection which already announces).
    /// </summary>
    [HarmonyPatch]
    public static class SelectionNotificationPatch
    {
        /// <summary>
        /// Flag to suppress selection announcements when we're doing our own selection
        /// (via SelectionHelper) that already announces.
        /// </summary>
        private static bool suppressAnnouncement = false;

        /// <summary>
        /// Set to true before performing our own selection operations.
        /// </summary>
        public static void SuppressNextAnnouncement()
        {
            suppressAnnouncement = true;
        }

        /// <summary>
        /// Track the previous selection to detect changes.
        /// </summary>
        private static HashSet<object> previousSelection = new HashSet<object>();

        /// <summary>
        /// Postfix patch on Selector.Select to announce selection changes.
        /// </summary>
        [HarmonyPatch(typeof(Selector), "Select")]
        [HarmonyPostfix]
        public static void Select_Postfix(Selector __instance, object obj, bool playSound)
        {
            // Don't announce if we're suppressing (our own selection that already announced)
            if (suppressAnnouncement)
            {
                suppressAnnouncement = false;
                return;
            }

            // Don't announce if playSound is false - this typically means it's an internal/silent selection
            // (like the temporary selections in GizmoNavigationState for tab visibility)
            // EXCEPT when gizmo navigation is executing - then we want to announce regardless
            if (!playSound && !GizmoNavigationState.IsExecutingGizmo)
            {
                return;
            }

            // Don't announce during gizmo menu navigation (temporary selections for visibility checks)
            if (GizmoNavigationState.IsActive && !GizmoNavigationState.IsExecutingGizmo)
            {
                return;
            }

            // Don't announce during inspection menu (it does its own announcing)
            if (WindowlessInspectionState.IsActive)
            {
                return;
            }

            // Announce the newly selected object
            if (obj != null)
            {
                string label = SelectionHelper.GetObjectLabel(obj);
                TolkHelper.Speak($"Selected {label}");
            }
        }

        /// <summary>
        /// Postfix patch on Selector.ClearSelection to announce deselection.
        /// </summary>
        [HarmonyPatch(typeof(Selector), "ClearSelection")]
        [HarmonyPostfix]
        public static void ClearSelection_Postfix(Selector __instance)
        {
            // Don't announce if suppressing
            if (suppressAnnouncement)
            {
                suppressAnnouncement = false;
                return;
            }

            // Don't announce during gizmo menu navigation
            if (GizmoNavigationState.IsActive)
            {
                return;
            }

            // Don't announce during inspection menu
            if (WindowlessInspectionState.IsActive)
            {
                return;
            }

            // Don't announce - deselection is typically just cleanup and not interesting to announce
            // Only the [ key handler announces deselection when user explicitly clears selection
        }
    }
}
