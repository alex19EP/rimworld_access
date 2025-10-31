using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace RimWorldAccess
{
    /// <summary>
    /// Helper class to query and format information about tiles on the map.
    /// Provides both summarized and detailed information for screen reader accessibility.
    /// </summary>
    public static class TileInfoHelper
    {
        /// <summary>
        /// Gets a concise summary of what's on a tile.
        /// Format: "[item1, item2, ... last item], indoors/outdoors, {lighting level}, at X, Z"
        /// </summary>
        public static string GetTileSummary(IntVec3 position, Map map)
        {
            if (map == null || !position.InBounds(map))
                return "Out of bounds";

            var sb = new StringBuilder();

            // Get all things at this position
            List<Thing> things = position.GetThingList(map);

            // List all things on the tile (up to 5 items, then summarize)
            int itemsListed = 0;
            int maxItemsToList = 5;
            bool addedSomething = false;

            foreach (var thing in things.Take(maxItemsToList))
            {
                if (addedSomething) sb.Append(", ");

                // Get the label for this thing
                string label = thing.LabelShort;

                // Special handling for forbidden items
                if (thing is Thing item)
                {
                    CompForbiddable forbiddable = item.TryGetComp<CompForbiddable>();
                    if (forbiddable != null && forbiddable.Forbidden)
                    {
                        label = "Forbidden " + label;
                    }
                }

                sb.Append(label);
                addedSomething = true;
                itemsListed++;
            }

            // If there are more items than we listed, add summary
            if (things.Count > maxItemsToList)
            {
                if (addedSomething) sb.Append(", ");
                sb.Append($"and {things.Count - maxItemsToList} more");
                addedSomething = true;
            }

            // If tile is empty, check if terrain has audio match
            if (!addedSomething)
            {
                TerrainDef terrain = position.GetTerrain(map);
                if (terrain != null && !TerrainAudioHelper.HasAudioMatch(terrain))
                {
                    // No audio match - announce the terrain name
                    sb.Append(terrain.LabelCap);
                    addedSomething = true;
                }
                else
                {
                    // Has audio match or no terrain - just say "Empty"
                    sb.Append("Empty");
                    addedSomething = true;
                }
            }

            // Add zone information if present
            Zone zone = position.GetZone(map);
            if (zone != null)
            {
                sb.Append($", in {zone.label}");
            }

            // Add indoor/outdoor status
            RoofDef roof = position.GetRoof(map);
            string locationStatus = (roof != null) ? "indoors" : "outdoors";
            sb.Append($", {locationStatus}");

            // Add light level
            PsychGlow lightLevel = map.glowGrid.PsychGlowAt(position);
            string lightDescription = lightLevel.GetLabel();
            sb.Append($", {lightDescription}");

            // Add coordinates
            sb.Append($", at {position.x}, {position.z}");

            return sb.ToString();
        }

        /// <summary>
        /// Gets detailed information about a tile for verbose mode.
        /// Includes all items, terrain, temperature, and other properties.
        /// </summary>
        public static string GetDetailedTileInfo(IntVec3 position, Map map)
        {
            if (map == null || !position.InBounds(map))
                return "Position out of bounds";

            var sb = new StringBuilder();
            sb.AppendLine($"=== Tile {position.x}, {position.z} ===");

            // Terrain
            TerrainDef terrain = position.GetTerrain(map);
            if (terrain != null)
            {
                sb.AppendLine($"Terrain: {terrain.LabelCap}");
            }

            // Get all things
            List<Thing> things = position.GetThingList(map);

            if (things.Count == 0)
            {
                sb.AppendLine("No objects on this tile");
            }
            else
            {
                // Group by category
                var pawns = things.OfType<Pawn>().ToList();
                var buildings = things.OfType<Building>().ToList();
                var plants = things.OfType<Plant>().ToList();
                var items = things.Where(t => !(t is Pawn) && !(t is Building) && !(t is Plant)).ToList();

                if (pawns.Count > 0)
                {
                    sb.AppendLine($"\nPawns ({pawns.Count}):");
                    foreach (var pawn in pawns)
                    {
                        sb.AppendLine($"  - {pawn.LabelShortCap}");
                    }
                }

                if (buildings.Count > 0)
                {
                    sb.AppendLine($"\nBuildings ({buildings.Count}):");
                    foreach (var building in buildings)
                    {
                        sb.Append($"  - {building.LabelShortCap}");

                        // Add temperature control information if building is a cooler/heater
                        string tempControlInfo = GetTemperatureControlInfo(building);
                        if (!string.IsNullOrEmpty(tempControlInfo))
                        {
                            sb.Append($" ({tempControlInfo})");
                        }

                        // Add power information if building has power components
                        string powerInfo = PowerInfoHelper.GetPowerInfo(building);
                        if (!string.IsNullOrEmpty(powerInfo))
                        {
                            if (!string.IsNullOrEmpty(tempControlInfo))
                                sb.Append($", {powerInfo}");
                            else
                                sb.Append($" ({powerInfo})");
                        }

                        sb.AppendLine();
                    }
                }

                if (items.Count > 0)
                {
                    sb.AppendLine($"\nItems ({items.Count}):");
                    foreach (var item in items.Take(20)) // Limit to 20 items
                    {
                        string label = item.LabelShortCap;
                        if (item.stackCount > 1)
                            label += $" x{item.stackCount}";

                        // Check if item is forbidden
                        CompForbiddable forbiddable = item.TryGetComp<CompForbiddable>();
                        if (forbiddable != null && forbiddable.Forbidden)
                        {
                            label = "Forbidden " + label;
                        }

                        sb.AppendLine($"  - {label}");
                    }
                    if (items.Count > 20)
                        sb.AppendLine($"  ... and {items.Count - 20} more items");
                }

                if (plants.Count > 0)
                {
                    sb.AppendLine($"\nPlants ({plants.Count}):");
                    foreach (var plant in plants)
                    {
                        sb.AppendLine($"  - {plant.LabelShortCap}");
                    }
                }
            }

            // Additional info
            sb.AppendLine("\n--- Environmental Info ---");

            // Temperature
            float temperature = position.GetTemperature(map);
            sb.AppendLine($"Temperature: {temperature:F1}°C");

            // Roof
            RoofDef roof = position.GetRoof(map);
            if (roof != null)
            {
                sb.AppendLine($"Roof: {roof.LabelCap}");
            }
            else
            {
                sb.AppendLine("Roof: None (outdoors)");
            }

            // Fog of war
            if (position.Fogged(map))
            {
                sb.AppendLine("Status: Fogged (not visible)");
            }

            // Zone
            Zone zone = position.GetZone(map);
            if (zone != null)
            {
                sb.AppendLine($"Zone: {zone.label}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets temperature control information for coolers and heaters.
        /// Returns direction (cooling/heating) and target temperature.
        /// </summary>
        private static string GetTemperatureControlInfo(Building building)
        {
            if (building == null)
                return null;

            // Check if this building has temperature control
            CompTempControl tempControl = building.TryGetComp<CompTempControl>();
            if (tempControl == null)
                return null;

            // Determine if this is a cooler or heater based on building type
            Building_TempControl tempControlBuilding = building as Building_TempControl;
            if (tempControlBuilding == null)
                return null;

            // For coolers specifically, we need to determine the cooling/heating direction
            string directionInfo = "";
            if (building.GetType().Name == "Building_Cooler")
            {
                // Coolers cool to the south (blue side) and heat to the north (red side)
                // IntVec3.South.RotatedBy(Rotation) gives the cooling direction
                // IntVec3.North.RotatedBy(Rotation) gives the heating direction
                Rot4 rotation = building.Rotation;

                // Get the actual cardinal direction for the blue (cooling) side
                IntVec3 coolingSide = IntVec3.South.RotatedBy(rotation);
                string coolingDir = GetCardinalDirection(coolingSide);

                // Get the actual cardinal direction for the red (heating) side
                IntVec3 heatingSide = IntVec3.North.RotatedBy(rotation);
                string heatingDir = GetCardinalDirection(heatingSide);

                directionInfo = $"cooling {coolingDir}, heating {heatingDir}";
            }
            else
            {
                // For other temperature control devices (heaters, vents, etc.)
                directionInfo = "temperature control";
            }

            // Add target temperature
            float targetTemp = tempControl.TargetTemperature;
            string tempString = targetTemp.ToStringTemperature("F0");

            return $"{directionInfo}, target {tempString}";
        }

        /// <summary>
        /// Converts an IntVec3 direction to a cardinal direction string.
        /// </summary>
        private static string GetCardinalDirection(IntVec3 direction)
        {
            if (direction == IntVec3.North) return "north";
            if (direction == IntVec3.South) return "south";
            if (direction == IntVec3.East) return "east";
            if (direction == IntVec3.West) return "west";
            return "unknown";
        }
    }
}
