using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
{
    public class EntityWindow : NonUniquePulsarGuiWindow
    {
        public Entity Entity { get; private set; }
        public EntityState EntityState { get; private set; }
        public String Title { get; private set; }

        private Vector2 ButtonSize = new Vector2(32, 32);

        public EntityWindow(EntityState entityState)
        {
            Entity = entityState.Entity;
            EntityState = entityState;

            if(Entity.HasDataBlob<NameDB>())
            {
                Title = Entity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
            }
            else
            {
                Title = "Unknown";
            }
        }

        internal override void Display()
        {
            if(!IsActive) return;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(512, 325), ImGuiCond.Once);
            if (ImGui.Begin(Title + " (" + EntityState.BodyType.ToDescription() + ")###" + Entity.Id, ref IsActive, _flags))
            {
                DisplayActions();
                DisplayInfo();
                DisplayConditional();

                ImGui.End();
            }
        }

        private void DisplayActions()
        {
            // Pin Camera
            ImGui.PushID(0);
            if(ImGui.ImageButton(_uiState.Img_Pin(), ButtonSize))
            {
                _uiState.Camera.PinToEntity(Entity);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(GlobalUIState.NamesForMenus[typeof(PinCameraBlankMenuHelper)]);
            ImGui.PopID();
            /*
            if(Entity.HasDataBlob<VolumeStorageDB>())
            {
                // Cargo Transfer
                ImGui.PushID(1);
                ImGui.SameLine();
                if(ImGui.ImageButton(_uiState.Img_Cargo(), ButtonSize))
                {
                    var instance = CargoTransfer.GetInstance(_uiState.Faction.GetDataBlob<FactionInfoDB>().Data, EntityState);
                    instance.ToggleActive();
                    _uiState.ActiveWindow = instance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(GlobalUIState.NamesForMenus[typeof(CargoTransfer)]);
                ImGui.PopID();
            }

            if(Entity.HasDataBlob<FireControlAbilityDB>())
            {
                // Fire Control
                ImGui.PushID(2);
                ImGui.SameLine();
                if(ImGui.ImageButton(_uiState.Img_Firecon(), ButtonSize))
                {
                    var instance = FireControl.GetInstance(EntityState);
                    instance.SetActive(true);
                    _uiState.ActiveWindow = instance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(GlobalUIState.NamesForMenus[typeof(FireControl)]);
                ImGui.PopID();
            }

            if(Entity.HasDataBlob<ColonyInfoDB>())
            {
                // Colony
                ImGui.PushID(3);
                ImGui.SameLine();
                if(ImGui.ImageButton(_uiState.Img_Industry(), ButtonSize))
                {
                    var instance = ColonyPanel.GetInstance(_uiState.Faction.GetDataBlob<FactionInfoDB>().Data, EntityState);
                    instance.SetActive(true);
                    _uiState.ActiveWindow = instance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open Industry");
                ImGui.PopID();
            }

            if(Entity.HasDataBlob<WarpAbilityDB>())
            {
                ImGui.SameLine();
                bool buttonresult = ImGui.SmallButton(GlobalUIState.NamesForMenus[typeof(WarpOrderWindow)]);
                EntityUIWindows.OpenUIWindow(typeof(WarpOrderWindow), EntityState, _uiState, buttonresult);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open warp menu");
            }

            if(Entity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                ImGui.SameLine();
                bool buttonresult = ImGui.SmallButton(GlobalUIState.NamesForMenus[typeof(ChangeCurrentOrbitWindow)]);
                EntityUIWindows.OpenUIWindow(typeof(ChangeCurrentOrbitWindow), EntityState, _uiState, buttonresult);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Change current orbit");

                ImGui.SameLine();
                buttonresult = ImGui.SmallButton(GlobalUIState.NamesForMenus[typeof(NavWindow)]);
                EntityUIWindows.OpenUIWindow(typeof(NavWindow), EntityState, _uiState, buttonresult);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open nav window");
            }

            if(EntityState.BodyType != UserOrbitSettings.OrbitBodyType.Ship)
            {
                ImGui.SameLine();
                bool buttonresult = ImGui.SmallButton(GlobalUIState.NamesForMenus[typeof(PlanetaryWindow)]);
                EntityUIWindows.OpenUIWindow(typeof(PlanetaryWindow), EntityState, _uiState, buttonresult);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open planetary window");
            }

            if(Entity.HasDataBlob<VolumeStorageDB>() && Entity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                ImGui.SameLine();
                bool buttonresult = ImGui.SmallButton(GlobalUIState.NamesForMenus[typeof(LogiShipWindow)]);
                EntityUIWindows.OpenUIWindow(typeof(LogiShipWindow), EntityState, _uiState, buttonresult);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open logistics window");
            }
            */
        }

        private void DisplayInfo()
        {
            if(ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if(Entity.HasDataBlob<ShipInfoDB>() && Entity.HasDataBlob<VolumeStorageDB>())
                {
                    var cargoLibrary = Entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
                    var (fuelType, fuelPercent) = Entity.GetFuelInfo(cargoLibrary);
                    var size = ImGui.GetContentRegionAvail();
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Styles.SelectedColor);
                    ImGui.ProgressBar((float)fuelPercent, new Vector2(size.X, 24), "Fuel (" + (fuelPercent * 100) + "%)");
                    ImGui.PopStyleColor();
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(fuelType?.Name ?? "Unknown");
                    }
                }

                ImGui.Columns(2);

                if(Entity.TryGetDatablob<SystemBodyInfoDB>(out var systemBodyInfoDB))
                {
                    DisplayHelpers.PrintRow("Body Type", systemBodyInfoDB.BodyType.ToDescription());
                }

                if(Entity.TryGetDatablob<MassVolumeDB>(out var massVolumeDB))
                {
                    DisplayHelpers.PrintRow("Radius", Stringify.Distance(massVolumeDB.RadiusInM));
                    DisplayHelpers.PrintRow("Mass", Stringify.Mass(massVolumeDB.MassTotal));
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Dry: " + Stringify.Mass(massVolumeDB.MassDry));
                    }
                    DisplayHelpers.PrintRow("Volume", Stringify.Volume(massVolumeDB.Volume_m3));
                    DisplayHelpers.PrintRow("Density", massVolumeDB.DensityDry_gcm.ToString("##0.000") + " kg/m^3");
                }

                if(Entity.TryGetDatablob<PositionDB>(out var positionDB))
                {
                    Entity? parent = positionDB.Parent;
                    if(parent != null)
                    {
                        if (Entity.TryGetDatablob<WarpMovingDB>(out var movedb))
                        {
                            DisplayHelpers.PrintRow("Warping", Stringify.Velocity(movedb.CurrentNonNewtonionVectorMS.Length()));
                        }
                        else
                        {
                            DisplayHelpers.PrintFormattedCell("Orbiting");
                            if(ImGui.SmallButton(parent.GetName(_uiState.Faction.Id)))
                            {
                                _uiState.EntityClicked(parent.Id, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                            }
                            ImGui.NextColumn();
                            ImGui.Separator();
                        }
                        
                    }
                }
                if(Entity.HasDataBlob<ColonyInfoDB>())
                {
                    Entity.GetDataBlob<ColonyInfoDB>().Display(EntityState, _uiState);
                }

                if(Entity.TryGetDatablob<StarInfoDB>(out var starInfoDB))
                {
                    starInfoDB.Display(EntityState, _uiState);
                }

                if(Entity.TryGetDatablob<GeoSurveyableDB>(out var geoSurveyableDB) && !geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id))
                {
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Geo Surveyable", "Yes");
                }

                if(Entity.TryGetDatablob<JPSurveyableDB>(out var jPSurveyableDB))
                {
                    ImGui.Columns(1);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                    ImGui.Text("Gravitational anomaly!");
                    ImGui.PopStyleColor();
                    ImGui.NewLine();
                    ImGui.TextWrapped("Order a fleet equipped with a gravitational surveyor to survey this location. A successful survey may reveal a Jump Point to another system.");
                    ImGui.NewLine();
                    ImGui.TextWrapped("Survey Points Required: " + jPSurveyableDB.PointsRequired);
                }

                ImGui.Columns(1);
            }
        }

        private void DisplayConditional()
        {
            if(Entity.Manager == null) return;

            bool isGeoSurveyed = Entity.HasDataBlob<GeoSurveyableDB>() ? Entity.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(_uiState.Faction.Id) : false;

            foreach(var db in Entity.Manager.GetAllDataBlobsForEntity(Entity.Id))
            {
                if(isGeoSurveyed && db is AtmosphereDB)
                {
                    ((AtmosphereDB)db).Display(EntityState, _uiState);
                }
                else if(isGeoSurveyed && db is MineralsDB && ImGui.CollapsingHeader("Minerals", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ((MineralsDB)db).Display(EntityState, _uiState);
                }
                else if(db is ComponentInstancesDB && ImGui.CollapsingHeader("Components", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ((ComponentInstancesDB)db).Display(EntityState, _uiState);
                }
                else if(db is VolumeStorageDB)
                {
                    ((VolumeStorageDB)db).Display(EntityState, _uiState);
                }
                // else if(db is EnergyGenAbilityDB && ImGui.CollapsingHeader("Power", ImGuiTreeNodeFlags.DefaultOpen))
                // {
                //     ((EnergyGenAbilityDB)db).Display(EntityState, _uiState);
                // }
                // else if(db is FleetDB && ImGui.CollapsingHeader("Ships", ImGuiTreeNodeFlags.DefaultOpen))
                // {
                // }
            }

            // Mining tab
            if(Entity.CanShowMiningTab())
            {
                if(ImGui.CollapsingHeader("Mining", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    Entity.DisplayMining(_uiState);
                }
            }
        }
    }
}