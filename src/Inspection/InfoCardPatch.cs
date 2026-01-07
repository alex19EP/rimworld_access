using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimWorldAccess
{
    /// <summary>
    /// Harmony patches for Dialog_InfoCard to enable keyboard accessibility.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_InfoCard))]
    public static class InfoCardPatch
    {
        // Track which dialog we've initialized for
        private static Dialog_InfoCard initializedDialog = null;
        private static int framesSinceOpen = 0;
        private static bool hasAnnounced = false;

        /// <summary>
        /// Postfix patch for DoWindowContents to handle keyboard input and initialize state.
        /// </summary>
        [HarmonyPatch("DoWindowContents")]
        [HarmonyPostfix]
        public static void DoWindowContents_Postfix(Dialog_InfoCard __instance, Rect inRect)
        {
            // Check if this is a new/different dialog
            if (initializedDialog != __instance)
            {
                // New dialog - reset and initialize
                if (InfoCardState.IsActive)
                {
                    InfoCardState.Close();
                }
                initializedDialog = __instance;
                framesSinceOpen = 0;
                hasAnnounced = false;
                InfoCardState.Open(__instance, announceOpening: false);
            }

            framesSinceOpen++;

            // Wait a few frames for stats to populate, then announce and rebuild
            if (!hasAnnounced && framesSinceOpen >= 3)
            {
                hasAnnounced = true;
                InfoCardState.RebuildAndAnnounce();
            }

            // Handle keyboard input
            if (InfoCardState.IsActive)
            {
                Event current = Event.current;
                if (current.type == EventType.KeyDown)
                {
                    InfoCardState.HandleInput(current);
                }
            }
        }

        /// <summary>
        /// Postfix patch for Close to clean up accessibility state.
        /// </summary>
        [HarmonyPatch("Close")]
        [HarmonyPostfix]
        public static void Close_Postfix()
        {
            initializedDialog = null;
            framesSinceOpen = 0;
            hasAnnounced = false;
            if (InfoCardState.IsActive)
            {
                InfoCardState.Close();
            }
        }
    }
}
