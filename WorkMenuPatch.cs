using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimWorldAccess
{
    /// <summary>
    /// Harmony patch that intercepts keyboard input when the work menu is active.
    /// Handles navigation (Up/Down arrows), toggling (Space), and confirmation (Enter/Escape).
    /// </summary>
    [HarmonyPatch(typeof(UIRoot))]
    [HarmonyPatch("UIRootOnGUI")]
    public static class WorkMenuPatch
    {
        /// <summary>
        /// Prefix patch that intercepts keyboard events when work menu is active.
        /// Returns false to prevent game from processing the event if we handle it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix()
        {
            // Only process if work menu is active
            if (!WorkMenuState.IsActive)
                return;

            // Only process keyboard events
            if (Event.current.type != EventType.KeyDown)
                return;

            bool handled = false;
            KeyCode key = Event.current.keyCode;

            // Handle Arrow Up: Navigate up
            if (key == KeyCode.UpArrow)
            {
                WorkMenuState.MoveUp();
                handled = true;
            }
            // Handle Arrow Down: Navigate down
            else if (key == KeyCode.DownArrow)
            {
                WorkMenuState.MoveDown();
                handled = true;
            }
            // Handle Space: Toggle selected work type
            else if (key == KeyCode.Space)
            {
                WorkMenuState.ToggleSelected();
                handled = true;
            }
            // Handle Enter/Return: Confirm and apply changes
            else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                WorkMenuState.Confirm();
                handled = true;
            }
            // Handle Escape: Cancel without applying changes
            else if (key == KeyCode.Escape)
            {
                WorkMenuState.Cancel();
                handled = true;
            }

            // Consume the event if we handled it
            if (handled)
            {
                Event.current.Use();
            }
        }

        /// <summary>
        /// Postfix patch that draws visual feedback for the work menu.
        /// Shows a highlighted overlay for the currently selected work type.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix()
        {
            // Only draw if work menu is active
            if (!WorkMenuState.IsActive)
                return;

            // Draw a semi-transparent overlay to indicate menu is active
            DrawMenuOverlay();
        }

        /// <summary>
        /// Draws a visual overlay indicating the work menu is active.
        /// Shows instructions and current selection.
        /// </summary>
        private static void DrawMenuOverlay()
        {
            // Get screen dimensions
            float screenWidth = UI.screenWidth;
            float screenHeight = UI.screenHeight;

            // Create a rect for the overlay (top-center of screen)
            float overlayWidth = 600f;
            float overlayHeight = 100f;
            float overlayX = (screenWidth - overlayWidth) / 2f;
            float overlayY = 20f;

            Rect overlayRect = new Rect(overlayX, overlayY, overlayWidth, overlayHeight);

            // Draw semi-transparent background
            Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            Widgets.DrawBoxSolid(overlayRect, backgroundColor);

            // Draw border
            Color borderColor = new Color(0.5f, 0.7f, 1.0f, 1.0f);
            Widgets.DrawBox(overlayRect, 2);

            // Draw text
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            string pawnName = WorkMenuState.CurrentPawn != null ? WorkMenuState.CurrentPawn.LabelShort : "Unknown";
            string title = $"Work Assignment Menu - {pawnName}";
            string instructions = "Arrow Keys: Navigate | Space: Toggle | Enter: Confirm | Escape: Cancel";

            Rect titleRect = new Rect(overlayX, overlayY + 10f, overlayWidth, 30f);
            Rect instructionsRect = new Rect(overlayX, overlayY + 45f, overlayWidth, 30f);

            Widgets.Label(titleRect, title);

            Text.Font = GameFont.Tiny;
            Widgets.Label(instructionsRect, instructions);

            // Reset text anchor
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
}
