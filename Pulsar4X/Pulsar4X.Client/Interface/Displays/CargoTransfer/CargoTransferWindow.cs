using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;

namespace Pulsar4X.SDL2UI
{
    public class CargoTransferWindow : PulsarGuiWindow
    {
        FactionDataStore? _staticData;
        EntityState? _selectedEntityLeft;
        EntityState? _selectedEntityRight;

        CargoListPanelComplex? _cargoList1;
        CargoListPanelComplex? CargoListLeft
        {
            get { return _cargoList1; }
            set
            {
                _cargoList1 = value;
                if(value != null)
                    value.CargoItemSelectedEvent += OnCargoItemSelectedEvent;
            }
        }
        CargoListPanelComplex? _cargoList2;
        CargoListPanelComplex? CargoListRight
        {
            get { return _cargoList2; }
            set
            {
                _cargoList2 = value;
                if(value != null)
                    value.CargoItemSelectedEvent += OnCargoItemSelectedEvent;
            }
        }
        CargoListPanelComplex? SelectedCargoPanel;
        CargoListPanelComplex? UnselectedCargoPanel;
        bool _hasCargoAbilityLeft;
        bool _isSelectingRight = false;
        bool _hasCargoAbilityRight;
        Dictionary<Guid, bool> headersOpenDict = new Dictionary<Guid, bool>();

        int _transferRate = 0;
        double _dvDifference_ms;
        double _dvMaxRangeDiff_ms;

        private CargoTransferWindow()
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            //ClickedEntityIsPrimary = false;
        }

        public static CargoTransferWindow GetInstance(FactionDataStore staticData, EntityState selectedEntity1)
        {

            CargoTransferWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(CargoTransferWindow)))
            {
                instance = new CargoTransferWindow
                {
                    _staticData = staticData,
                    _selectedEntityLeft = _uiState.PrimaryEntity
                };
                instance.HardRefresh();
            }
            else
            {
                instance = (CargoTransferWindow)_uiState.LoadedWindows[typeof(CargoTransferWindow)];
                if (instance._selectedEntityLeft != selectedEntity1)
                {
                    instance.HardRefresh();
                }
            }

            return instance;
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            if(IsActive) //lets not update unless the window is actualy being displayed.
            {
                if (_cargoList1 != null)
                    _cargoList1.Update();
                if (_cargoList2 != null)
                    _cargoList2.Update();
            }
        }

        void HardRefresh()
        {
            if(_staticData == null) return;

            _selectedEntityLeft = _uiState.PrimaryEntity;
            _selectedEntityRight = null;
            CargoListRight = null;
            _hasCargoAbilityRight = false;
            _transferRate = 0;
            _isSelectingRight = false;
            if(_selectedEntityLeft != null && _selectedEntityLeft.Entity.HasDataBlob<CargoStorageDB>())
            {
                CargoListLeft = new CargoListPanelComplex(_staticData, _selectedEntityLeft, headersOpenDict);
                _hasCargoAbilityLeft = true;
            }
            else
                _hasCargoAbilityLeft = false;


            if (_uiState.PrimaryEntity != _uiState.LastClickedEntity)
            {
                if(_isSelectingRight)
                {
                    _selectedEntityRight = _uiState.LastClickedEntity;
                    _isSelectingRight = false;
                }
                if (_selectedEntityRight != null && _selectedEntityLeft != null && _selectedEntityLeft.Entity.HasDataBlob<CargoStorageDB>())
                {
                    if (!_hasCargoAbilityRight)
                        CargoListRight = new CargoListPanelComplex(_staticData, _selectedEntityRight, headersOpenDict);
                    _hasCargoAbilityRight = true;
                }
                else
                    _hasCargoAbilityRight = false;
            }
        }

        internal void Set2ndCargo(EntityState entity)
        {
            if (_selectedEntityLeft != null && _selectedEntityLeft.Entity.HasDataBlob<CargoStorageDB>())
            {
                _selectedEntityRight = entity;
                if (_staticData != null && entity.Entity.HasDataBlob<CargoStorageDB>())
                {
                    CargoListRight = new CargoListPanelComplex(_staticData, _selectedEntityRight, headersOpenDict);

                    CalcTransferRate();

                    _hasCargoAbilityRight = true;
                }
                else
                {
                    CargoListRight = null;
                    _hasCargoAbilityRight = false;
                    _transferRate = 0;
                }
            }
        }

        void CalcTransferRate()
        {
            if(_selectedEntityLeft == null || _selectedEntityRight == null)
                throw new NullReferenceException();

            double? dvDif;

            dvDif = CargoTransferProcessor.CalcDVDifference_m(_selectedEntityLeft.Entity, _selectedEntityRight.Entity);

            if (dvDif == null)
            {
                _transferRate = 0;
            }
            else
            {
                var cargoDBLeft = _selectedEntityLeft.Entity.GetDataBlob<CargoStorageDB>();
                var cargoDBRight = _selectedEntityRight.Entity.GetDataBlob<CargoStorageDB>();
                _dvMaxRangeDiff_ms = Math.Max(cargoDBLeft.TransferRangeDv_mps, cargoDBRight.TransferRangeDv_mps);
                _dvDifference_ms = (double)dvDif;
                _transferRate = CargoTransferProcessor.CalcTransferRate(_dvDifference_ms,
                    cargoDBLeft,
                    cargoDBRight);
            }
        }


        // called when item on transfer screen is clicked
        // ought to update currently selected item
        void OnCargoItemSelectedEvent(CargoListPanelComplex cargoPannel)
        {
            SelectedCargoPanel = cargoPannel;
            if (cargoPannel == CargoListLeft)
                UnselectedCargoPanel = CargoListRight;
            else UnselectedCargoPanel = CargoListLeft;

            if(UnselectedCargoPanel != null)
                UnselectedCargoPanel.selectedCargo = null;

        }


        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if(button == MouseButtons.Primary)
            {
                if(_selectedEntityLeft != null && _selectedEntityLeft.Entity.Id != entity.Entity.Id && _isSelectingRight)
                    Set2ndCargo(entity);
                else
                {
                    HardRefresh();
                }
            }
        }

        private void MoveItems(int amount)
        {
            if(SelectedCargoPanel == null
                || SelectedCargoPanel.selectedCargo == null
                || UnselectedCargoPanel == null)
                throw new NullReferenceException();

            var selectedCargoItem = SelectedCargoPanel.selectedCargo;
            SelectedCargoPanel.AddUICargoIn(selectedCargoItem, -amount);
            UnselectedCargoPanel.AddUICargoIn(selectedCargoItem, amount);
        }

        private void ActionXferOrder()
        {
            if(_selectedEntityLeft == null
                || _selectedEntityRight == null
                || CargoListLeft == null
                || CargoListRight == null)
                throw new NullReferenceException();


            CargoTransferOrder.CreateCommands(
                _uiState.Faction.Id,
                _selectedEntityLeft.Entity,
                _selectedEntityRight.Entity,
                CargoListLeft.GetAllToMove()
                );
            
            CargoListLeft.ClearUINumbers();
            CargoListRight.ClearUINumbers();
        }


        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("Cargo", ref IsActive, _flags))
                {
                    if (_hasCargoAbilityLeft && CargoListLeft != null)
                    {
                        CargoListLeft.Display();
                        ImGui.SameLine();
                        ImGui.BeginChild("xfer", new Vector2(100, 200));
                        ImGui.Text("Transfer");

                        if (SelectedCargoPanel != null && SelectedCargoPanel.selectedCargo != null)
                        {
                            if (UnselectedCargoPanel != null && UnselectedCargoPanel.CanStore(SelectedCargoPanel.selectedCargo.CargoTypeID))
                            {
                                if (ImGui.Button("x100"))
                                { MoveItems(100); }
                                ImGui.SameLine();
                                if (ImGui.Button("x10"))
                                { MoveItems(10); }
                                ImGui.SameLine();
                                if (ImGui.Button("x1"))
                                { MoveItems(1); }
                                if (ImGui.Button("Action Order"))
                                { ActionXferOrder(); }
                            }

                            if (UnselectedCargoPanel != null && UnselectedCargoPanel.CanInstall(SelectedCargoPanel.selectedCargo))
                            {
                                if (ImGui.Button("Install"))
                                {
                                    ComponentInstance specificComponent = new((ComponentDesign)SelectedCargoPanel.selectedCargo);
                                    CargoInstallOrder.CreateCommand(
                                        _uiState.Faction.Id,
                                        _selectedEntityLeft.Entity, 
                                        _selectedEntityRight.Entity,
                                        specificComponent
                                        );
                                }
                            }
                            //else
                                //can't transfer due to target unable to store this type
                        }

                        ImGui.EndChild();
                        ImGui.SameLine();
                        if (_hasCargoAbilityRight && CargoListRight != null)
                        {

                            CargoListRight.Display();
                            ImGui.Text("DeltaV Difference: " + Stringify.Velocity(_dvDifference_ms));
                            ImGui.Text("Max DeltaV Difference: " + Stringify.Velocity(_dvMaxRangeDiff_ms));
                            ImGui.Text("Transfer Rate Kg/s: " + _transferRate);

                        }

                        string label = "Click to Select Entity For Transfer";
                        if (_isSelectingRight)
                            label = "Select Entity For Transfer";
                        else if (ImGui.SmallButton(label))
                        {
                            _isSelectingRight = !_isSelectingRight;
                            if (_isSelectingRight)
                                ClickedEntityIsPrimary = false;
                            else
                                ClickedEntityIsPrimary = true;

                        }
                        if(!_isSelectingRight)
                            ClickedEntityIsPrimary = true;
                    }

                }
                ImGui.End();
            }
        }
    }


}

