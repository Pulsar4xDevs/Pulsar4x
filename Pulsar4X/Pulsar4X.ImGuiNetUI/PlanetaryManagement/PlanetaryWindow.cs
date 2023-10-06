using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    class PlanetaryWindow : NonUniquePulsarGuiWindow
    {
        private readonly List<Mineral> _mineralDefinitions = null;
        private readonly int _maxMineralNameLength = 0;
        private const string _amountFormat = "#,###,###,###,###,###,##0";   // big enough to render 64 integers

        private enum PlanetarySubWindows{
            generalInfo,
            installations,
            mineralDeposits
        }

        private PlanetarySubWindows _selectedSubWindow = PlanetarySubWindows.generalInfo;

        public PlanetaryWindow(EntityState entity, GlobalUIState state)
        {
            _state = state;
            SetName("PlanetaryWindow|" + entity.Entity.Id.ToString());
            if (_mineralDefinitions == null) {
                _mineralDefinitions = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineralsList().ToList();
                _maxMineralNameLength = _mineralDefinitions.Max(x => x.Name.Length);
            }
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            onEntityChange(entity);
        }

        internal void onEntityChange(EntityState entity)
        {
            _lookedAtEntity = entity;
        }

        internal static PlanetaryWindow GetInstance(EntityState entity, GlobalUIState state)
        {
            string name = "PlanetaryWindow|" + entity.Entity.Id.ToString();
            PlanetaryWindow thisItem;
            if (!_uiState.LoadedNonUniqueWindows.ContainsKey(name))
            {
                thisItem = new PlanetaryWindow(entity, state);
                thisItem.StartDisplay();
            }
            else
            {
                thisItem = (PlanetaryWindow)_uiState.LoadedNonUniqueWindows[name];
                thisItem.onEntityChange(entity);
            }

            return thisItem;
        }


        internal override void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button)
        {

        }

        internal override void Display()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400,400),ImGuiCond.Once);
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

            MineralsDB mindb;
            if (_lookedAtEntity.Entity.TryGetDatablob<MineralsDB>(out mindb) && mindb.Minerals.Any() )
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
            var headerRow = new List<KeyValuePair<string, TextAlign>>
            {
                new KeyValuePair<string, TextAlign>("", TextAlign.Left),
                new KeyValuePair<string, TextAlign>("", TextAlign.Right)
            };

            List<string[]> rowData = new List<string[]>();

            if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>())
            {
                SystemBodyInfoDB sysBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                rowData.Add(new string[] { "Body Type", sysBodyInfo.BodyType.ToDescription() });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<StarInfoDB>())
            {
                StarInfoDB starInfo = _lookedAtEntity.Entity.GetDataBlob<StarInfoDB>();
                rowData.Add(new string[] { "Spectral Type", starInfo.SpectralType.ToDescription() + starInfo.SpectralSubDivision });
                rowData.Add(new string[] { "Luminosity Type", starInfo.LuminosityClass.ToDescription() });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<MassVolumeDB>())
            {
                var tempMassVolume = _lookedAtEntity.Entity.GetDataBlob<MassVolumeDB>();
                rowData.Add(new string[] { "Radius", Stringify.Distance(tempMassVolume.RadiusInM) });
                rowData.Add(new string[] { "Mass", tempMassVolume.MassDry.ToString() + " kg" });
                rowData.Add(new string[] { "Volume", Stringify.Volume(tempMassVolume.Volume_m3) });
                rowData.Add(new string[] { "Density", tempMassVolume.DensityDry_gcm.ToString("##0.000") + " kg/m^3" });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<ColonyInfoDB>())
            {
                rowData.Add(new string[] { "-----", "" });
                rowData.Add(new string[] { "Populations", "" });
                ColonyInfoDB tempColonyInfo = _lookedAtEntity.Entity.GetDataBlob<ColonyInfoDB>();
                foreach (var popPerSpecies in tempColonyInfo.Population)
                {
                    var speciesEntity = _uiState.Game.GlobalManager.GetGlobalEntityById(popPerSpecies.Key);
                    rowData.Add(new string[] {" " + speciesEntity.GetDefaultName(), Stringify.Quantity(popPerSpecies.Value, "0.0##", true) });
                }
            }


            if (_lookedAtEntity.Entity.HasDataBlob<StarInfoDB>())
            {
                StarInfoDB starInfo = _lookedAtEntity.Entity.GetDataBlob<StarInfoDB>();
                rowData.Add(new string[] { "Surface Temp", starInfo.Temperature.ToString("###,##0.00") + "°C" });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<AtmosphereDB>())
            {
                AtmosphereDB atmosInfo = _lookedAtEntity.Entity.GetDataBlob<AtmosphereDB>();
                rowData.Add(new string[] { "-----", "" });
                rowData.Add(new string[] { "Hydroshpere", atmosInfo.Hydrosphere ? "YES" : "NO" });
                if (atmosInfo.Hydrosphere)
                {
                    rowData.Add(new string[] { "  Extent", atmosInfo.HydrosphereExtent.ToString() + " %" });
                }

                if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>())
                {
                    SystemBodyInfoDB sysBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                    rowData.Add(new string[] { "Base Temp", sysBodyInfo.BaseTemperature.ToString("###,##0.00") + "°C" });
                }
                rowData.Add(new string[] { "Surface Temp", atmosInfo.SurfaceTemperature.ToString("###,##0.00") + "°C" });

                rowData.Add(new string[] { "-----", "" });
                rowData.Add(new string[] { "Atmosphere", "" });
                rowData.Add(new string[] { "Pressure", atmosInfo.Pressure + " atm" });

                rowData.Add(new string[] { "Composition", "" });
                foreach (var atmosGas in atmosInfo.CompositionByPercent)
                {
                    var blueprint = _uiState.Game.AtmosphericGases[atmosGas.Key];

                    if (Math.Round(atmosGas.Value, 4) > 0)
                    {
                        rowData.Add(new string[] { "  " + blueprint.Name, Stringify.Quantity(Math.Round(atmosGas.Value, 4), "0.0###") + " %" });
                    }
                    else
                    {
                        rowData.Add(new string[] { "  " + blueprint.Name, "trace amounts" });
                    }
                }
            }

            Helpers.RenderImgUITextTable(headerRow.ToArray(), rowData);
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
            var headerRow = new List<KeyValuePair<string, TextAlign>>
            {
                new KeyValuePair<string, TextAlign>("Mineral", TextAlign.Left),
                new KeyValuePair<string, TextAlign>("Available", TextAlign.Center),
                new KeyValuePair<string, TextAlign>("Accessibility", TextAlign.Right)
            };

            if (_lookedAtEntity.Entity.HasDataBlob<MineralsDB>())
            {
                var mineRates = new Dictionary<int, long>();

                MineralsDB mineralsDB = _lookedAtEntity.Entity.GetDataBlob<MineralsDB>();
                SystemBodyInfoDB systemBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                if (systemBodyInfo.Colonies.Any())
                {
                    // if colonies exists then
                    headerRow.Add(new KeyValuePair<string, TextAlign>("Mining Rate", TextAlign.Right));
                    foreach (Entity colonyEntity in systemBodyInfo.Colonies)
                    {
                        var colonyRates = MiningHelper.CalculateActualMiningRates(colonyEntity);
                        foreach (var rate in colonyRates)
                        {
                            if (!mineRates.ContainsKey(rate.Key))
                            {
                                mineRates.Add(rate.Key, 0);
                            }
                            mineRates[rate.Key] += rate.Value;
                        }
                    }
                }

                var deposits = mineralsDB.Minerals.Where(x => x.Value.Amount > 0);
                if (deposits.Any())
                {
                    var maxMineralQuantity = mineralsDB.Minerals.Values.Max(x => x.Amount).ToString(_amountFormat).Length;

                    List<string[]> rowData = new List<string[]>();
                    var row = new List<string>();
                    foreach (var key in mineralsDB.Minerals.Keys)
                    {
                        row.Clear();
                        var mineralData = _mineralDefinitions.FirstOrDefault(x => x.ID == key);
                        if (mineralData != null)
                        {
                            var mineralValues = mineralsDB.Minerals[key];

                            row.Add(mineralData.Name);
                            row.Add(mineralValues.Amount.ToString(_amountFormat));
                            row.Add(mineralValues.Accessibility.ToString("0.00"));
                            if (mineRates.Any())
                            {
                                var rate = Stringify.Quantity(mineRates.ContainsKey(key) ? mineRates[key] : 0) + "/day";
                                row.Add(rate);
                            }

                            rowData.Add(row.ToArray());
                        }
                    }

                    Helpers.RenderImgUITextTable(headerRow.ToArray(), rowData);
                }
            }
        }
    }
}

