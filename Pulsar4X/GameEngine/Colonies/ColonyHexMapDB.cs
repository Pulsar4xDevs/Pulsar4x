using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using GameEngine.People;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// DataBlob that stores the hex map for a colony
    /// </summary>
    public class ColonyHexMapDB : BaseDataBlob
    {
        private Dictionary<HexCoordinate, HexTile> _hexTiles;
        
        /// <summary>
        /// Maximum radius of the hex map (determined by administration building size)
        /// </summary>
        public int MaxRadius { get; private set; }
        
        /// <summary>
        /// Current radius being used
        /// </summary>
        public int CurrentRadius { get; private set; }
        
        /// <summary>
        /// All hex tiles in the map
        /// </summary>
        public IReadOnlyDictionary<HexCoordinate, HexTile> HexTiles => _hexTiles;

        public ColonyHexMapDB()
        {
            _hexTiles = new Dictionary<HexCoordinate, HexTile>();
            MaxRadius = 1;
            CurrentRadius = 1;
            InitializeMap();
        }

        public ColonyHexMapDB(int maxRadius)
        {
            _hexTiles = new Dictionary<HexCoordinate, HexTile>();
            MaxRadius = Math.Max(1, maxRadius);
            CurrentRadius = MaxRadius;
            InitializeMap();
        }

        /// <summary>
        /// Update the maximum radius based on administration building size
        /// </summary>
        public void UpdateMaxRadius(int officeSpace)
        {
            // Formula: hex radius scales with square root of office space
            int newMaxRadius = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(officeSpace / 100.0)));
            
            if (newMaxRadius != MaxRadius)
            {
                MaxRadius = newMaxRadius;
                if (CurrentRadius > MaxRadius)
                {
                    CurrentRadius = MaxRadius;
                }
                InitializeMap();
            }
        }

        /// <summary>
        /// Expand the current usable radius (up to max radius)
        /// </summary>
        public bool ExpandRadius(int newRadius)
        {
            if (newRadius <= MaxRadius && newRadius > CurrentRadius)
            {
                CurrentRadius = newRadius;
                InitializeMap();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Initialize or reinitialize the hex map
        /// </summary>
        private void InitializeMap()
        {
            var oldTiles = new Dictionary<HexCoordinate, HexTile>(_hexTiles);
            _hexTiles.Clear();

            // Generate all hexes within current radius
            for (int q = -CurrentRadius; q <= CurrentRadius; q++)
            {
                int r1 = Math.Max(-CurrentRadius, -q - CurrentRadius);
                int r2 = Math.Min(CurrentRadius, -q + CurrentRadius);
                
                for (int r = r1; r <= r2; r++)
                {
                    var coord = new HexCoordinate(q, r);
                    
                    // Preserve existing tile data if it exists
                    if (oldTiles.TryGetValue(coord, out var existingTile))
                    {
                        _hexTiles[coord] = existingTile;
                    }
                    else
                    {
                        _hexTiles[coord] = new HexTile(coord);
                    }
                }
            }
        }

        /// <summary>
        /// Get a hex tile at the specified coordinate
        /// </summary>
        public HexTile? GetTile(HexCoordinate coordinate)
        {
            return _hexTiles.TryGetValue(coordinate, out var tile) ? tile : null;
        }

        /// <summary>
        /// Get all tiles of a specific type
        /// </summary>
        public IEnumerable<HexTile> GetTilesByType(HexTileType tileType)
        {
            return _hexTiles.Values.Where(tile => tile.TileType == tileType);
        }

        /// <summary>
        /// Get all tiles within a radius of a coordinate
        /// </summary>
        public IEnumerable<HexTile> GetTilesInRadius(HexCoordinate center, int radius)
        {
            return _hexTiles.Values.Where(tile => tile.Coordinate.DistanceTo(center) <= radius);
        }

        /// <summary>
        /// Get the center tile (0,0)
        /// </summary>
        public HexTile GetCenterTile()
        {
            return _hexTiles[new HexCoordinate(0, 0)];
        }

        /// <summary>
        /// Check if a coordinate is valid (within current radius)
        /// </summary>
        public bool IsValidCoordinate(HexCoordinate coordinate)
        {
            return _hexTiles.ContainsKey(coordinate);
        }

        /// <summary>
        /// Get total number of tiles of each type
        /// </summary>
        public Dictionary<HexTileType, int> GetTileTypeCount()
        {
            var counts = new Dictionary<HexTileType, int>();
            foreach (var tileType in Enum.GetValues<HexTileType>())
            {
                counts[tileType] = 0;
            }

            foreach (var tile in _hexTiles.Values)
            {
                counts[tile.TileType]++;
            }

            return counts;
        }

        /// <summary>
        /// Calculate total map efficiency
        /// </summary>
        public float GetOverallEfficiency()
        {
            if (!_hexTiles.Any()) return 0f;

            var occupiedTiles = _hexTiles.Values.Where(t => t.IsOccupied);
            if (!occupiedTiles.Any()) return 0f;

            return occupiedTiles.Average(t => t.GetEfficiency());
        }
    }
}