using System;
using Pulsar4X.Engine;

namespace Pulsar4X.Colonies
{
    public enum HexTileType
    {
        Empty,
        Residential,
        Industrial,
        Commercial,
        Agricultural,
        Administrative,
        Military,
        Research,
        Energy,
        Mining,
        Transportation,
        Recreation,
        Waste
    }

    /// <summary>
    /// Represents a single hex tile in a colony hex map
    /// </summary>
    public class HexTile
    {
        public HexCoordinate Coordinate { get; }
        public HexTileType TileType { get; set; }
        public string? BuildingId { get; set; }
        public Entity? Building { get; set; }
        public bool IsOccupied => Building != null || !string.IsNullOrEmpty(BuildingId);
        
        /// <summary>
        /// Resource modifier for this tile (mining, agriculture, etc.)
        /// </summary>
        public float ResourceModifier { get; set; } = 1.0f;
        
        /// <summary>
        /// Pollution level of this tile
        /// </summary>
        public float PollutionLevel { get; set; } = 0.0f;
        
        /// <summary>
        /// Infrastructure level (roads, utilities, etc.)
        /// </summary>
        public int InfrastructureLevel { get; set; } = 0;

        public HexTile(HexCoordinate coordinate)
        {
            Coordinate = coordinate;
            TileType = HexTileType.Empty;
        }

        /// <summary>
        /// Calculate efficiency based on infrastructure and pollution
        /// </summary>
        public float GetEfficiency()
        {
            float infrastructureBonus = InfrastructureLevel * 0.1f;
            float pollutionPenalty = PollutionLevel * 0.2f;
            return Math.Max(0.1f, ResourceModifier + infrastructureBonus - pollutionPenalty);
        }

        /// <summary>
        /// Check if this tile can support the given building type
        /// </summary>
        public bool CanPlaceBuilding(HexTileType buildingType)
        {
            if (IsOccupied) return false;
            
            // Basic placement rules - can be expanded
            return buildingType switch
            {
                HexTileType.Mining => ResourceModifier > 1.0f, // Need resource deposits
                HexTileType.Agricultural => PollutionLevel < 0.5f, // Need clean environment
                _ => true // Most buildings can be placed anywhere
            };
        }

        public void PlaceBuilding(HexTileType buildingType, string? buildingId = null, Entity? buildingEntity = null)
        {
            if (!CanPlaceBuilding(buildingType))
                throw new InvalidOperationException($"Cannot place {buildingType} building at {Coordinate}");

            TileType = buildingType;
            BuildingId = buildingId;
            Building = buildingEntity;
        }

        public void RemoveBuilding()
        {
            TileType = HexTileType.Empty;
            BuildingId = null;
            Building = null;
        }
    }
}