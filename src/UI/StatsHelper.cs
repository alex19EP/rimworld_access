using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimWorldAccess
{
    /// <summary>
    /// Helper class for fetching and formatting stats for Things.
    /// Uses RimWorld's native StatsReportUtility to ensure exact parity with vanilla display.
    /// </summary>
    public static class StatsHelper
    {
        // Cached reflection field for vanilla's stat entries
        private static readonly FieldInfo CachedDrawEntriesField = typeof(StatsReportUtility)
            .GetField("cachedDrawEntries", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// Gets all displayable stats for a Thing by triggering vanilla's stat system.
        /// This ensures exact parity with what vanilla displays in the info panel.
        /// </summary>
        public static List<StatDrawEntry> GetAllStats(Thing thing)
        {
            if (thing == null)
                return new List<StatDrawEntry>();

            try
            {
                // Reset vanilla's cache
                StatsReportUtility.Reset();

                // Trigger vanilla to populate its cache by calling DrawStatsReport
                // We use a dummy rect since we only care about the cache being populated
                var dummyRect = new Rect(0, 0, 100, 100);
                StatsReportUtility.DrawStatsReport(dummyRect, thing);

                // Read vanilla's cached entries
                var cached = CachedDrawEntriesField?.GetValue(null) as List<StatDrawEntry>;

                // Return a copy to avoid issues with vanilla modifying the list
                return cached != null ? new List<StatDrawEntry>(cached) : new List<StatDrawEntry>();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimWorld Access] Error getting stats for {thing.LabelCap}: {ex.Message}");
                return new List<StatDrawEntry>();
            }
        }

        /// <summary>
        /// Formats a list of stat entries into a readable string for screen readers.
        /// Stats are already sorted by vanilla, so we preserve that order.
        /// </summary>
        public static string FormatStatsForScreenReader(List<StatDrawEntry> stats, string objectLabel = null)
        {
            if (stats == null || stats.Count == 0)
                return "No stats available.";

            var sb = new StringBuilder();

            // Optional header
            if (!string.IsNullOrEmpty(objectLabel))
            {
                sb.AppendLine(objectLabel);
                sb.AppendLine();
            }

            // Group by category and format (vanilla already sorted the list)
            string currentCategory = null;
            foreach (var stat in stats)
            {
                // Add category header if this is a new category
                string categoryLabel = stat.category?.LabelCap.ToString() ?? "Other";
                if (categoryLabel != currentCategory)
                {
                    currentCategory = categoryLabel;
                    if (sb.Length > 0)
                        sb.AppendLine(); // Add spacing between categories
                    sb.AppendLine($"--- {currentCategory} ---");
                }

                // Format the stat line
                string label = stat.LabelCap.ToString().StripTags();
                string value = stat.ValueString.StripTags();

                // Skip description entry (it's usually empty in ValueString)
                if (label == "Description" && string.IsNullOrEmpty(value))
                    continue;

                // Replace dollar sign with "silver" for lore-friendly currency display
                if (value.Contains("$"))
                {
                    value = value.Replace("$", "").Trim() + " silver";
                }

                sb.AppendLine($"{label}: {value}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets stats grouped by category for hierarchical navigation.
        /// </summary>
        public static Dictionary<string, List<StatDrawEntry>> GetStatsGroupedByCategory(Thing thing)
        {
            var grouped = new Dictionary<string, List<StatDrawEntry>>();

            if (thing == null)
                return grouped;

            List<StatDrawEntry> allStats = GetAllStats(thing);

            foreach (var stat in allStats)
            {
                string categoryLabel = stat.category?.LabelCap.ToString() ?? "Other";

                if (!grouped.ContainsKey(categoryLabel))
                {
                    grouped[categoryLabel] = new List<StatDrawEntry>();
                }

                grouped[categoryLabel].Add(stat);
            }

            return grouped;
        }

        /// <summary>
        /// Checks if a Thing has any displayable stats.
        /// </summary>
        public static bool HasStats(Thing thing)
        {
            if (thing == null)
                return false;

            List<StatDrawEntry> stats = GetAllStats(thing);
            return stats != null && stats.Any();
        }
    }
}
