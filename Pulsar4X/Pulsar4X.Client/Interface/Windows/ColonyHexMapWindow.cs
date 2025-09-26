using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Colonies;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Names;

namespace Pulsar4X.Client
{
    public class ColonyHexMapWindow : PulsarGuiWindow
    {
        private Entity? _selectedColony;
        private ColonyHexMapDB? _hexMapDB;
        private HexCoordinate? _selectedHex;
        private Vector2 _mapOffset = Vector2.Zero;
        private float _hexSize = 30.0f;
        private readonly float _minHexSize = 15.0f;
        private readonly float _maxHexSize = 60.0f;
        private bool _showGrid = true;
        private bool _showCoordinates = false;
        
        // Colors for different tile types
        private readonly Dictionary<HexTileType, uint> _tileColors = new()
        {
            { HexTileType.Empty, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f)) },
            { HexTileType.Residential, ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.7f, 0.3f, 1.0f)) },
            { HexTileType.Industrial, ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.6f, 0.2f, 1.0f)) },
            { HexTileType.Commercial, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.6f, 0.8f, 1.0f)) },
            { HexTileType.Agricultural, ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.8f, 0.2f, 1.0f)) },
            { HexTileType.Administrative, ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 1.0f)) },
            { HexTileType.Military, ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.2f, 0.2f, 1.0f)) },
            { HexTileType.Research, ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.8f, 1.0f)) },
            { HexTileType.Energy, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.2f, 1.0f)) },
            { HexTileType.Mining, ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.4f, 0.2f, 1.0f)) },
            { HexTileType.Transportation, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 1.0f)) },
            { HexTileType.Recreation, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.8f, 0.8f, 1.0f)) },
            { HexTileType.Waste, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.3f, 0.1f, 1.0f)) }
        };

        private ColonyHexMapWindow()
        {
            _flags = ImGuiWindowFlags.None;
        }

        internal static ColonyHexMapWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ColonyHexMapWindow)))
            {
                _uiState.LoadedWindows[typeof(ColonyHexMapWindow)] = new ColonyHexMapWindow();
            }
            return (ColonyHexMapWindow)_uiState.LoadedWindows[typeof(ColonyHexMapWindow)];
        }

        public void SetSelectedColony(Entity colony)
        {
            _selectedColony = colony;
            
            // Try to get existing hex map or create a new one
            if (!colony.TryGetDataBlob<ColonyHexMapDB>(out _hexMapDB))
            {
                _hexMapDB = new ColonyHexMapDB();
                colony.SetDataBlob(_hexMapDB);
                
                // Update hex map based on existing admin buildings
                ColonyHexMapProcessor.ForceUpdateColonyHexMap(colony);
            }
        }

        internal override void Display()
        {
            if (!IsActive || _selectedColony == null || _hexMapDB == null)
                return;

            var colonyName = _selectedColony.GetDataBlob<NameDB>()?.GetName(_uiState.Faction?.Id ?? -1) ?? "Unknown Colony";
            
            if (ImGui.Begin($"Colony Hex Map - {colonyName}###ColonyHexMap", ref IsActive, _flags))
            {
                DrawControls();
                ImGui.Separator();
                DrawHexMap();
                ImGui.Separator();
                DrawTileInfo();
            }
            ImGui.End();
        }

        private void DrawControls()
        {
            // Zoom controls
            ImGui.Text("Zoom:");
            ImGui.SameLine();
            if (ImGui.SliderFloat("##zoom", ref _hexSize, _minHexSize, _maxHexSize, "%.1f"))
            {
                // Clamp hex size
                _hexSize = Math.Clamp(_hexSize, _minHexSize, _maxHexSize);
            }

            // Display options
            ImGui.SameLine();
            ImGui.Checkbox("Grid", ref _showGrid);
            ImGui.SameLine();
            ImGui.Checkbox("Coordinates", ref _showCoordinates);

            // Map info
            ImGui.Text($"Map Radius: {_hexMapDB.CurrentRadius}/{_hexMapDB.MaxRadius}");
            ImGui.SameLine();
            ImGui.Text($"Tiles: {_hexMapDB.HexTiles.Count}");
            ImGui.SameLine();
            ImGui.Text($"Efficiency: {_hexMapDB.GetOverallEfficiency():P1}");

            // Reset view button
            ImGui.SameLine();
            if (ImGui.Button("Reset View"))
            {
                _mapOffset = Vector2.Zero;
                _hexSize = 30.0f;
            }
        }

        private void DrawHexMap()
        {
            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var canvasSize = ImGui.GetContentRegionAvail();
            var canvasCenter = canvasPos + canvasSize * 0.5f + _mapOffset;

            // Handle mouse input for panning
            ImGui.InvisibleButton("canvas", canvasSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
            var io = ImGui.GetIO();
            
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                _mapOffset += io.MouseDelta;
            }

            // Handle mouse wheel for zooming
            if (ImGui.IsItemHovered())
            {
                var wheel = io.MouseWheel;
                if (wheel != 0)
                {
                    var oldHexSize = _hexSize;
                    _hexSize = Math.Clamp(_hexSize + wheel * 3.0f, _minHexSize, _maxHexSize);
                    
                    // Adjust offset to zoom towards mouse position
                    if (Math.Abs(oldHexSize - _hexSize) > 0.1f)
                    {
                        var mousePos = io.MousePos;
                        var mousePosInCanvas = mousePos - canvasCenter;
                        var scaleFactor = _hexSize / oldHexSize;
                        _mapOffset = _mapOffset * scaleFactor + mousePosInCanvas * (1 - scaleFactor);
                    }
                }
            }

            // Draw hexagons
            foreach (var kvp in _hexMapDB.HexTiles)
            {
                var coordinate = kvp.Key;
                var tile = kvp.Value;
                
                var hexCenter = HexToPixel(coordinate, canvasCenter);
                
                // Skip hexes outside visible area
                if (hexCenter.X < canvasPos.X - _hexSize * 2 || hexCenter.X > canvasPos.X + canvasSize.X + _hexSize * 2 ||
                    hexCenter.Y < canvasPos.Y - _hexSize * 2 || hexCenter.Y > canvasPos.Y + canvasSize.Y + _hexSize * 2)
                    continue;

                DrawHexagon(drawList, hexCenter, tile);
                
                // Check for mouse click on this hex
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var mousePos = io.MousePos;
                    if (IsPointInHex(mousePos, hexCenter))
                    {
                        _selectedHex = coordinate;
                    }
                }
            }

            // Highlight selected hex
            if (_selectedHex.HasValue && _hexMapDB.IsValidCoordinate(_selectedHex.Value))
            {
                var selectedCenter = HexToPixel(_selectedHex.Value, canvasCenter);
                DrawHexagonOutline(drawList, selectedCenter, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), 3.0f);
            }
        }

        private void DrawHexagon(ImDrawListPtr drawList, Vector2 center, HexTile tile)
        {
            var color = _tileColors[tile.TileType];
            var points = GetHexagonPoints(center);
            
            // Fill hexagon with smooth polygon fill
            drawList.AddConvexPolyFilled(ref points[0], points.Length, color);

            // Draw outline if grid is enabled
            if (_showGrid)
            {
                var outlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                for (int i = 0; i < points.Length; i++)
                {
                    var nextI = (i + 1) % points.Length;
                    drawList.AddLine(points[i], points[nextI], outlineColor, 1.0f);
                }
            }

            // Draw coordinates if enabled
            if (_showCoordinates && _hexSize > 25)
            {
                var coordText = $"{tile.Coordinate.Q},{tile.Coordinate.R}";
                var textSize = ImGui.CalcTextSize(coordText);
                var textPos = center - textSize * 0.5f;
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), coordText);
            }
        }

        private void DrawHexagonOutline(ImDrawListPtr drawList, Vector2 center, uint color, float thickness)
        {
            var points = GetHexagonPoints(center);
            for (int i = 0; i < points.Length; i++)
            {
                var nextI = (i + 1) % points.Length;
                drawList.AddLine(points[i], points[nextI], color, thickness);
            }
        }

        private Vector2[] GetHexagonPoints(Vector2 center)
        {
            var points = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                var angle = i * Math.PI / 3;
                points[i] = new Vector2(
                    center.X + _hexSize * (float)Math.Cos(angle),
                    center.Y + _hexSize * (float)Math.Sin(angle)
                );
            }
            return points;
        }

        private Vector2 HexToPixel(HexCoordinate coord, Vector2 origin)
        {
            var x = _hexSize * (3.0f / 2.0f * coord.Q);
            var y = _hexSize * (Math.Sqrt(3) / 2.0f * coord.Q + Math.Sqrt(3) * coord.R);
            return origin + new Vector2((float)x, (float)y);
        }

        private bool IsPointInHex(Vector2 point, Vector2 hexCenter)
        {
            var distance = Vector2.Distance(point, hexCenter);
            return distance <= _hexSize;
        }

        private void DrawTileInfo()
        {
            if (!_selectedHex.HasValue)
            {
                ImGui.Text("Select a hex tile to view details");
                return;
            }

            var tile = _hexMapDB.GetTile(_selectedHex.Value);
            if (tile == null) return;

            ImGui.Text($"Coordinate: {tile.Coordinate}");
            ImGui.Text($"Type: {tile.TileType}");
            ImGui.Text($"Efficiency: {tile.GetEfficiency():P1}");
            ImGui.Text($"Infrastructure: Level {tile.InfrastructureLevel}");
            ImGui.Text($"Pollution: {tile.PollutionLevel:P1}");
            ImGui.Text($"Resource Modifier: {tile.ResourceModifier:F2}x");
            
            if (tile.IsOccupied)
            {
                ImGui.Text($"Building: {tile.BuildingId ?? "Unknown"}");
            }

            // Tile type selection
            ImGui.Separator();
            ImGui.Text("Change Tile Type:");
            foreach (var tileType in Enum.GetValues<HexTileType>())
            {
                if (ImGui.Button(tileType.ToString()))
                {
                    if (tile.CanPlaceBuilding(tileType))
                    {
                        tile.PlaceBuilding(tileType);
                    }
                }
                if ((int)tileType % 3 != 2) ImGui.SameLine();
            }
        }
    }
}