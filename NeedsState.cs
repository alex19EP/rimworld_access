using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RimWorldAccess
{
    /// <summary>
    /// State class for displaying needs information of the pawn at the cursor position.
    /// Triggered by Alt+N key combination.
    /// </summary>
    public static class NeedsState
    {
        /// <summary>
        /// Displays needs information for the pawn at the current cursor position.
        /// Shows all needs with their current percentages and trends.
        /// </summary>
        public static void DisplayNeedsInfo()
        {
            // Check if we're in-game
            if (Current.ProgramState != ProgramState.Playing)
            {
                ClipboardHelper.CopyToClipboard("Not in game");
                return;
            }

            // Check if there's a current map
            if (Find.CurrentMap == null)
            {
                ClipboardHelper.CopyToClipboard("No map loaded");
                return;
            }

            // Check if map navigation is initialized
            if (!MapNavigationState.IsInitialized)
            {
                ClipboardHelper.CopyToClipboard("Map navigation not initialized");
                return;
            }

            // Get the cursor position
            IntVec3 cursorPosition = MapNavigationState.CurrentCursorPosition;

            // Validate cursor position
            if (!cursorPosition.IsValid || !cursorPosition.InBounds(Find.CurrentMap))
            {
                ClipboardHelper.CopyToClipboard("Invalid cursor position");
                return;
            }

            // Get all pawns at the cursor position
            var pawnsAtPosition = Find.CurrentMap.thingGrid.ThingsListAt(cursorPosition)
                .OfType<Pawn>()
                .ToList();

            if (pawnsAtPosition.Count == 0)
            {
                ClipboardHelper.CopyToClipboard("No pawn at cursor position");
                return;
            }

            // Get the first pawn at the position
            Pawn pawnAtCursor = pawnsAtPosition.First();

            // Get needs information using PawnInfoHelper
            string needsInfo = PawnInfoHelper.GetNeedsInfo(pawnAtCursor);

            // Copy to clipboard for screen reader
            ClipboardHelper.CopyToClipboard(needsInfo);
        }
    }
}
