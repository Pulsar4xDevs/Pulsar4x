using System;
using System.Numerics;
using ImGuiNET;
using Microsoft.VisualBasic;
using Pulsar4X.ECSLib;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.ImGuiNetUI.EntityManagement;

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
            if (ImGui.Begin(Title + " (" + EntityState.BodyType.ToDescription() + ")###" + Entity.Guid, ref IsActive, _flags))
            {
                DisplayActions();

                ImGui.BeginTabBar("Tab bar!###Tabs" + Entity.Guid);

                DisplayInfoTab();
                DisplayConditionalTabs();

                ImGui.EndTabBar();
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

            if(Entity.HasDataBlob<VolumeStorageDB>())
            {
                // Cargo Transfer
                ImGui.PushID(1);
                ImGui.SameLine();
                if(ImGui.ImageButton(_uiState.Img_Cargo(), ButtonSize))
                {
                    var instance = CargoTransfer.GetInstance(_uiState.Game.StaticData, EntityState);
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
                    var instance = ColonyPanel.GetInstance(_uiState.Game.StaticData, EntityState);
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
        }

        private void DisplayInfoTab()
        {
            if(ImGui.BeginTabItem("Info"))
            {
                if(Entity.HasDataBlob<SystemBodyInfoDB>())
                {
                    ImGui.Text("Body Type: " + Entity.GetDataBlob<SystemBodyInfoDB>().BodyType.ToDescription());
                }

                if(Entity.HasDataBlob<MassVolumeDB>())
                {
                    MassVolumeDB mvDb = Entity.GetDataBlob<MassVolumeDB>();
                    ImGui.Text("Radius: " + Stringify.Distance(mvDb.RadiusInM));
                    ImGui.Text("Mass: " + mvDb.MassDry.ToString() + " kg");
                    ImGui.Text("Volume: " + Stringify.Volume(mvDb.Volume_m3));
                    ImGui.Text("Density: " + mvDb.DensityDry_gcm.ToString("##0.000") + " kg/m^3");
                }

                if(Entity.HasDataBlob<PositionDB>())
                {
                    Entity parent = Entity.GetDataBlob<PositionDB>().Parent;
                    if(parent != null)
                    {
                        ImGui.Text("Orbiting: ");
                        ImGui.SameLine();
                        if(ImGui.SmallButton(parent.GetName(_uiState.Faction.Guid)))
                        {
                            _uiState.EntityClicked(parent.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                        }
                    }
                }

                if(Entity.HasDataBlob<ColonyInfoDB>())
                {
                    Entity.GetDataBlob<ColonyInfoDB>().Display(EntityState, _uiState);
                }

                ImGui.EndTabItem();
            }
        }

        private void DisplayConditionalTabs()
        {
            foreach(var db in Entity.DataBlobs)
            {
                if(db is AtmosphereDB && ImGui.BeginTabItem("Atmosphere"))
                {
                    ((AtmosphereDB)db).Display(EntityState, _uiState);
                    ImGui.EndTabItem();
                }

                if(db is MineralsDB && ImGui.BeginTabItem("Minerals"))
                {
                    ((MineralsDB)db).Display(EntityState, _uiState);
                    ImGui.EndTabItem();
                }

                if(db is StarInfoDB && ImGui.BeginTabItem("Star Info"))
                {
                    ((StarInfoDB)db).Display(EntityState, _uiState);
                    ImGui.EndTabItem();
                }
                if(db is ComponentInstancesDB && ImGui.BeginTabItem("Components"))
                {
                    ((ComponentInstancesDB)db).Display(EntityState, _uiState);
                    ImGui.EndTabItem();
                }
                if(db is VolumeStorageDB && ImGui.BeginTabItem("Storage"))
                {
                    ((VolumeStorageDB)db).Display(EntityState, _uiState);
                    ImGui.EndTabItem();
                }
            }

            // Mining tab
            if(Entity.CanShowMiningTab())
            {
                if(ImGui.BeginTabItem("Mining"))
                {
                    Entity.DisplayMining(_uiState);
                    ImGui.EndTabItem();
                }
            }
        }
    }
}