using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Client.Interface;

namespace Pulsar4X.Client
{
    public class EntityWindow : NonUniquePulsarGuiWindow
    {
        public Entity Entity { get; private set; }
        public EntityState EntityState { get; private set; }
        public string Title { get; private set; }

        private Vector2 ButtonSize = new Vector2(32, 32);

        // Animation constants
        private const float WindowWidth = 624f;
        private const float WindowHeight = 364f;
        private const float AnimationDuration = 0.2f; // seconds
        private const float BottomMargin = 4f;
        private const float RightMargin = 4f;

        // Animation state
        private enum AnimationState { Closed, Opening, Open, Closing }
        private AnimationState _animationState = AnimationState.Closed;
        private float _animationProgress = 0f;
        private DateTime _animationStartTime;

        public EntityWindow(EntityState entityState)
        {
            Entity = entityState.Entity;
            EntityState = entityState;
            _flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar;

            if(_uiState.Faction != null && Entity.TryGetDataBlob<NameDB>(out var nameDB))
            {
                Title = nameDB.GetName(_uiState.Faction);
            }
            else
            {
                Title = "Unknown";
            }
        }

        public new void SetActive(bool activeVal = true)
        {
            if (activeVal && !IsActive)
            {
                // Starting to open
                _animationState = AnimationState.Opening;
                _animationStartTime = DateTime.Now;
                _animationProgress = 0f;
                IsActive = true;
            }
            else if (!activeVal && IsActive)
            {
                // Starting to close
                _animationState = AnimationState.Closing;
                _animationStartTime = DateTime.Now;
                _animationProgress = 1f;
            }
        }

        public new void ToggleActive()
        {
            SetActive(!IsActive);
        }

        private float EaseOutCubic(float t)
        {
            return 1f - MathF.Pow(1f - t, 3f);
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private void UpdateAnimation()
        {
            if (_animationState == AnimationState.Open || _animationState == AnimationState.Closed)
                return;

            float elapsed = (float)(DateTime.Now - _animationStartTime).TotalSeconds;
            float t = Math.Clamp(elapsed / AnimationDuration, 0f, 1f);

            if (_animationState == AnimationState.Opening)
            {
                _animationProgress = EaseOutCubic(t);
                if (t >= 1f)
                {
                    _animationState = AnimationState.Open;
                    _animationProgress = 1f;
                }
            }
            else if (_animationState == AnimationState.Closing)
            {
                _animationProgress = 1f - EaseInCubic(t);
                if (t >= 1f)
                {
                    _animationState = AnimationState.Closed;
                    _animationProgress = 0f;
                    IsActive = false;
                }
            }
        }

        private Vector2 CalculateWindowPosition()
        {
            var viewportSize = _uiState.MainWinSize;

            // Final position: bottom right corner
            float finalX = viewportSize.X - WindowWidth - RightMargin;
            float finalY = viewportSize.Y - WindowHeight - BottomMargin;

            // Animate from right (offscreen beyond right edge) into final position
            // When progress is 0, window is offscreen to the right
            // When progress is 1, window is at its final position
            float startX = viewportSize.X; // Start completely off-screen to the right
            float currentX = startX + (finalX - startX) * _animationProgress;

            return new Vector2(currentX, finalY);
        }

        internal override void Display()
        {
            if(!IsActive && _animationState == AnimationState.Closed) return;

            UpdateAnimation();

            // Don't render if fully closed
            if (_animationState == AnimationState.Closed) return;

            var windowPos = CalculateWindowPosition();
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(WindowWidth, WindowHeight), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.8f);

            // Remove window border
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

            // Track if window is closed via the X button
            bool windowOpen = true;
            if (Window.Begin(Title + " (" + EntityState.BodyType.ToDescription() + ")" + "###" + Entity.Id, ref windowOpen, _flags))
            {
                DisplayHeader();
                DisplayActions();
                DisplayInfo();
                DisplayConditional();
            }
            Window.End();

            ImGui.PopStyleVar();

            // Handle close button click
            if (!windowOpen && _animationState != AnimationState.Closing)
            {
                SetActive(false);
            }
        }

        private void DisplayHeader()
        {
            ImGui.PushFont(Styles.MediumFont, 16f);
            ImGui.Text(Title.ToUpper());
            ImGui.PopFont();
        }

        private void DisplayActions()
        {
            // Pin Camera
            ImGui.PushID(EntityState.Id);
            if(ImGui.ImageButton("###entitywindowactions", _uiState.Img_Pin().ToTextureRef(), ButtonSize))
            {
                _uiState.Camera.PinToEntity(Entity);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(GlobalUIState.NamesForMenus[typeof(PinCameraBlankMenuHelper)]);
            ImGui.PopID();
            /*
            if(Entity.HasDataBlob<CargoStorageDB>())
            {
                // Cargo Transfer
                ImGui.PushID(1);
                ImGui.SameLine();
                if(ImGui.ImageButton(_uiState.Img_Cargo(), ButtonSize))
                {
                    var instance = CargoTransferWindow.GetInstance(_uiState.Faction.GetDataBlob<FactionInfoDB>().Data, EntityState);
                    instance.ToggleActive();
                    _uiState.ActiveWindow = instance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(GlobalUIState.NamesForMenus[typeof(CargoTransferWindow)]);
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

            if(Entity.HasDataBlob<CargoStorageDB>() && Entity.HasDataBlob<NewtonThrustAbilityDB>())
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
            if(_uiState.Game == null
                || _uiState.Faction == null)
                return;

            if(ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if(Entity.HasDataBlob<ShipInfoDB>() && Entity.HasDataBlob<CargoStorageDB>())
                {
                    var cargoLibrary = Entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
                    var (fuelType, fuelPercent) = Entity.GetFuelInfo(cargoLibrary);
                    string fuelStr = "Fuel (" + (fuelPercent * 100) + "%) ";
                    if (Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out var newtDB))
                        fuelStr += Stringify.Velocity(newtDB.DeltaV) + " Δv";
                    var size = ImGui.GetContentRegionAvail();
                    ImGui.ProgressBar((float)fuelPercent, new Vector2(size.X, 24), fuelStr);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(fuelType?.Name ?? "Unknown");
                    }
                }

                ImGui.Columns(2);

                if(Entity.TryGetDataBlob<SystemBodyInfoDB>(out var systemBodyInfoDB))
                {
                    DisplayHelpers.PrintRow("Body Type", systemBodyInfoDB.BodyType.ToDescription());
                }

                if(Entity.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB))
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

                if(Entity.TryGetDataBlob<PositionDB>(out var positionDB))
                {
                    Entity? parent = positionDB.Parent;
                    if(parent != null)
                    {
                        if (Entity.TryGetDataBlob<WarpMovingDB>(out var movedb))
                        {
                            DisplayHelpers.PrintRow("Warping", Stringify.Velocity(movedb.CurrentNonNewtonionVectorMS.Length()));
                        }
                        else
                        {
                            DisplayHelpers.PrintFormattedCell("Orbiting");
                            if(ImGui.SmallButton(parent.GetName(_uiState.Faction.Id)))
                            {
                                _uiState.EntityClicked(parent.Id, _uiState.SelectedStarSystemId, MouseButtons.Primary);
                            }
                            ImGui.NextColumn();
                            ImGui.Separator();
                        }

                    }
                }
                if(Entity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfoDB))
                {
                    colonyInfoDB.Display(EntityState, _uiState);
                }

                if(Entity.TryGetDataBlob<StarInfoDB>(out var starInfoDB))
                {
                    starInfoDB.Display(EntityState, _uiState);
                }

                if(Entity.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB) && !geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id))
                {
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Geo Surveyable", "Yes");
                }

                if(Entity.TryGetDataBlob<JPSurveyableDB>(out var jPSurveyableDB))
                {
                    ImGui.Columns(1);
                    Displays.GravitationalAnomlay(_uiState, jPSurveyableDB);
                }

                ImGui.Columns(1);
            }
        }

        private void DisplayConditional()
        {
            if(Entity.Manager == null
                || _uiState.Faction == null) return;

            bool isGeoSurveyed = Entity.HasDataBlob<GeoSurveyableDB>() ? Entity.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(_uiState.Faction.Id) : false;

            foreach(var db in Entity.Manager.GetAllDataBlobsForEntity(Entity.Id))
            {
                if( db is OrderableDB)
                {
                    var orderableDB = (OrderableDB)db;
                    if (orderableDB.ActionList.Count == 0)
                        continue;
                    if (ImGui.CollapsingHeader("Orders", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp;
                        if (ImGui.BeginTable("OrdersTable", 3, Styles.TableFlags))
                        {

                            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthStretch, 0.1f);
                            ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthStretch, 0.2f);
                            ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch, 0.7f);
                            ImGui.TableHeadersRow();

                            var actions = orderableDB.ActionList.ToArray();
                            for (int i = 0; i < actions.Length; i++)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text((i + 1).ToString());
                                ImGui.TableNextColumn();
                                ImGui.Text(actions[i].Name);
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("IsRunning: " + actions[i].IsRunning);
                                    ImGui.Text("IsFinished: " + actions[i].GetIsFinished);
                                    ImGui.EndTooltip();
                                }
                                ImGui.TableNextColumn();
                                ImGui.Text(actions[i].Details);
                            }

                            ImGui.EndTable();
                        }

                    }
                }
                else if(isGeoSurveyed && db is AtmosphereDB)
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
                else if(db is CargoStorageDB)
                {
                    ((CargoStorageDB)db).Display(EntityState, _uiState);
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