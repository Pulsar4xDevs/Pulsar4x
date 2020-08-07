using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;

namespace Pulsar4X.SDL2UI
{
    class PlanetaryWindow : PulsarGuiWindow
    {
        private readonly List<MineralSD> _mineralDefinitions = null;
        private readonly int _maxMineralNameLength = 0;
        private const string _amountFormat = "#,###,###,###,###,###,##0";   // big enough to render 64 integers

        private enum PlanetarySubWindows{
            generalInfo, 
            installations,
            mineralDeposits
        }
        private EntityState _lookedAtEntity;

        private PlanetarySubWindows _selectedSubWindow = PlanetarySubWindows.generalInfo;

        private PlanetaryWindow(EntityState entity)
        {
            if (_mineralDefinitions == null) {
                _mineralDefinitions = _uiState.Game.StaticData.CargoGoods.GetMineralsList();
                _maxMineralNameLength = _mineralDefinitions.Max(x => x.Name.Length);
            }
            //_flags = ImGuiWindowFlags.NoCollapse;

            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            onEntityChange(entity);
        }


        internal void onEntityChange(EntityState entity)
        {
            _lookedAtEntity = entity;
        }

        internal static PlanetaryWindow GetInstance(EntityState entity)
        {

            PlanetaryWindow thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(PlanetaryWindow)))
            {
                thisItem = new PlanetaryWindow(entity);
            }
            else
            {
                thisItem = (PlanetaryWindow)_uiState.LoadedWindows[typeof(PlanetaryWindow)];
                thisItem.onEntityChange(entity);
            }


            return thisItem;


        }


        internal override void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button)
        {

        }

        internal override void Display()
        {
            ImGui.SetNextWindowSize(new Vector2(400,400),ImGuiCond.Once);
            if (IsActive == true && ImGui.Begin("Planetary Window: " + _lookedAtEntity.Name, ref IsActive, _flags))
            {
                RenderTabOptions();

                ImGui.BeginChild("data");
                switch(_selectedSubWindow){
                    case PlanetarySubWindows.generalInfo:
                        RenderGeneralInfo();
                        break;
                    case PlanetarySubWindows.installations:
                        RenderInstallations();
                        break;
                    case PlanetarySubWindows.mineralDeposits:
                        RenderMineralDeposits();
                        break;
                    default:
                        break;
                }
                ImGui.EndChild();
                ImGui.End();
            }

        }

        private void RenderTabOptions()
        {
            if (ImGui.SmallButton("General Info"))
            {
                _selectedSubWindow = PlanetarySubWindows.generalInfo;
            }

            if (_lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Installations"))
                {
                    _selectedSubWindow = PlanetarySubWindows.installations;
                }
            }

            if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>() && _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>().Minerals.Any())
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Mineral Deposits"))
                {
                    _selectedSubWindow = PlanetarySubWindows.mineralDeposits;
                }
            }
        }

        private void RenderGeneralInfo()
        {
            if (_lookedAtEntity.Entity.HasDataBlob<MassVolumeDB>())
            {
                var tempMassVolume = _lookedAtEntity.Entity.GetDataBlob<MassVolumeDB>();
                ImGui.Text("Radius: " + Stringify.Distance(tempMassVolume.RadiusInM));
                ImGui.Text("Mass: " + tempMassVolume.MassDry.ToString() + " kg");
                ImGui.Text("Volume: " + tempMassVolume.Volume_m3.ToString() + " m^3");
                ImGui.Text("Density: " + tempMassVolume.Density_gcm + " kg/m^3");
            }

            if (_lookedAtEntity.Entity.HasDataBlob<ColonyInfoDB>())
            {
                ImGui.Text("---");
                ColonyInfoDB tempColonyInfo = _lookedAtEntity.Entity.GetDataBlob<ColonyInfoDB>();
                ImGui.Text("Populations: ");
                foreach (var popPerSpecies in tempColonyInfo.Population)
                {
                    ImGui.Text("  " + Stringify.Quantity(popPerSpecies.Value, "0.0##" ,true) + " of species: ");
                    ImGui.SameLine();
                    if (popPerSpecies.Key.HasDataBlob<NameDB>())
                    {
                        ImGui.Text(popPerSpecies.Key.GetDataBlob<NameDB>().DefaultName);
                    }
                    else
                    {
                        ImGui.Text("unknown.");
                    }
                }
            }
        }

        private void RenderInstallations()
        {
            if (_lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())
            {
                InstallationsDB tempInstallations = _lookedAtEntity.Entity.GetDataBlob<InstallationsDB>();
            }
        }

        private void RenderMineralDeposits()
        {
            var headerRow = new KeyValuePair<string, TextAlign>[3];
            headerRow[0] = new KeyValuePair<string, TextAlign>("Mineral", TextAlign.Left);
            headerRow[1] = new KeyValuePair<string, TextAlign>("Available", TextAlign.Center);
            headerRow[2] = new KeyValuePair<string, TextAlign>("Accessibility", TextAlign.Right);

            if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>())
            {
                SystemBodyInfoDB systemBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                var deposits = systemBodyInfo.Minerals.Where(x => x.Value.Amount > 0);
                if (deposits.Any())
                {
                    var maxMineralQuantity = systemBodyInfo.Minerals.Values.Max(x => x.Amount).ToString(_amountFormat).Length;

                    List<string[]> rowData = new List<string[]>();
                    foreach (var key in systemBodyInfo.Minerals.Keys)
                    {
                        var mineralData = _mineralDefinitions.FirstOrDefault(x => x.ID == key);
                        if (mineralData != null)
                        {
                            var mineralValues = systemBodyInfo.Minerals[key];
                            var row = new string[3];
                            row[0] = mineralData.Name;
                            row[1] = mineralValues.Amount.ToString(_amountFormat);
                            row[2] = mineralValues.Accessibility.ToString("0.00");

                            rowData.Add(row);
                        }
                    }

                    Helpers.RenderImgUITextTable(headerRow, rowData);
                }
            }
        }
    }
}

