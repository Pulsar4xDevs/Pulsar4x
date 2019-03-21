using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
namespace Pulsar4X.SDL2UI
{

    public class CargoListPannelSimple
    {
        StaticDataStore _staticData;
        EntityState _entityState;
        CargoStorageDB _storageDatablob;
        Dictionary<Guid, CargoTypeStoreVM> _cargoResourceStoresDict = new Dictionary<Guid, CargoTypeStoreVM>();
        public List<CargoTypeStoreVM> CargoResourceStores { get; } = new List<CargoTypeStoreVM>();
        public CargoItemVM SelectedItem = null;
        public CargoListPannelSimple(StaticDataStore staticData, EntityState entity)
        {
            _staticData = staticData;
            _entityState = entity;
            _storageDatablob = entity.Entity.GetDataBlob<CargoStorageDB>();
            Update();
        }

        public void Update()
        {
            foreach (var kvp in _storageDatablob.StoredCargoTypes)
            {
                if (!_cargoResourceStoresDict.ContainsKey(kvp.Key))
                {
                    var newCargoTypeStoreVM = new CargoTypeStoreVM(_staticData, kvp.Key, kvp.Value);
                    _cargoResourceStoresDict.Add(kvp.Key, newCargoTypeStoreVM);
                    CargoResourceStores.Add(newCargoTypeStoreVM);
                }
                _cargoResourceStoresDict[kvp.Key].Update();
            }

            foreach (var key in _cargoResourceStoresDict.Keys.ToArray())
            {
                if (!_storageDatablob.StoredCargoTypes.ContainsKey(key))
                {
                    CargoResourceStores.Remove(_cargoResourceStoresDict[key]);
                    _cargoResourceStoresDict.Remove(key);
                }
            }
        }



        public void Display()
        {

            var width = ImGui.GetWindowWidth() * 0.5f;
            
            ImGui.BeginChild(_entityState.Name, new System.Numerics.Vector2(240, 200), true);
            foreach (var storetype in CargoResourceStores)
            {
                if (ImGui.CollapsingHeader(storetype.HeaderText + "###" + _entityState.Name + storetype.StorageTypeName, ImGuiTreeNodeFlags.CollapsingHeader))
                {

                    foreach (CargoItemVM item in storetype.CargoItems)
                    {
                        if(ImGui.Selectable(item.ItemName))
                        {
                            SelectedItem = item;
                  
                        }
                        ImGui.SameLine();
                        ImGui.Text(item.ItemWeightPerUnit);
                        ImGui.SameLine();
                        ImGui.Text(item.NumberOfItems);
                        ImGui.SameLine();
                        ImGui.Text(item.TotalWeight);

                    }
                }
            }
            ImGui.EndChild();
        }

    }

    public delegate void CargoItemSelectedHandler(CargoListPannelComplex cargoPannel);
    public class CargoListPannelComplex
    {
        StaticDataStore _staticData;
        EntityState _entityState;
        CargoStorageDB _storageDatablob;
        Dictionary<Guid, CargoTypeStoreVM> _cargoResourceStoresDict = new Dictionary<Guid, CargoTypeStoreVM>();
        public List<CargoTypeStoreVM> CargoResourceStores { get; } = new List<CargoTypeStoreVM>();
        public CargoItemVM SelectedCargoVM = null;
        public CargoListPannelComplex(StaticDataStore staticData, EntityState entity)
        {
            _staticData = staticData;
            _entityState = entity;
            _storageDatablob = entity.Entity.GetDataBlob<CargoStorageDB>();
            Update();
        }

        public event CargoItemSelectedHandler CargoItemSelectedEvent;

        public void Update()
        {
            foreach (var kvp in _storageDatablob.StoredCargoTypes)
            {
                if (!_cargoResourceStoresDict.ContainsKey(kvp.Key))
                {
                    var newCargoTypeStoreVM = new CargoTypeStoreVM(_staticData, kvp.Key, kvp.Value);
                    _cargoResourceStoresDict.Add(kvp.Key, newCargoTypeStoreVM);
                    CargoResourceStores.Add(newCargoTypeStoreVM);
                }
                _cargoResourceStoresDict[kvp.Key].Update();
            }

            foreach (var key in _cargoResourceStoresDict.Keys.ToArray())
            {
                if (!_storageDatablob.StoredCargoTypes.ContainsKey(key))
                {
                    CargoResourceStores.Remove(_cargoResourceStoresDict[key]);
                    _cargoResourceStoresDict.Remove(key);
                }
            }
        }

        internal void AddUICargoIn(ICargoable cargoItem, long itemCount)
        {
            CargoTypeStoreVM store;
            if (_cargoResourceStoresDict.ContainsKey(cargoItem.CargoTypeID))
            {
                store = _cargoResourceStoresDict[cargoItem.CargoTypeID];
                CargoItemVM item = store.GetOrAddItemVM(cargoItem);
                item.ItemIncomingAmount += itemCount;
            }
            else throw new Exception("Can't contain this type of cargo");
        }

        internal void AddUICargoOut(ICargoable cargoItem, long itemCount)
        {
            CargoTypeStoreVM store;
            if (_cargoResourceStoresDict.ContainsKey(cargoItem.CargoTypeID))
            {
                store = _cargoResourceStoresDict[cargoItem.CargoTypeID];
                CargoItemVM item = store.GetItemVM(cargoItem.ID);
                if(item.ItemIncomingAmount > 0)
                {
                    var amount = Math.Min(itemCount, item.ItemIncomingAmount);
                    item.ItemIncomingAmount -= amount;
                    itemCount -= amount;
                }
                item.ItemOutgoingAmount += itemCount;
            }
            else throw new Exception("Doesn't store this type of cargo");

        }

        internal long AmountInStoreAndMove(Guid typeID, Guid cargoID)
        {
            long amount = 0;
            if (_cargoResourceStoresDict.ContainsKey(typeID))
            {
                CargoTypeStoreVM store = _cargoResourceStoresDict[typeID];
                amount = store.GetItemVM(cargoID).ItemsTotal;
            }

            return amount;
        }

        internal bool HasCargoInStore(ICargoable cargoItem)
        {

            if (_cargoResourceStoresDict.ContainsKey(cargoItem.CargoTypeID))
            {
                CargoTypeStoreVM store = _cargoResourceStoresDict[cargoItem.CargoTypeID];
                return store.ContainsItem(cargoItem.ID); 
            }
            return false;
        }

        public void Display()
        {

            var width = ImGui.GetWindowWidth() * 0.5f;

            ImGui.BeginChild(_entityState.Name, new System.Numerics.Vector2(240, 200), true);
            foreach (var storetype in CargoResourceStores)
            {
                if (ImGui.CollapsingHeader(storetype.HeaderText + "###" + _entityState.Name + storetype.StorageTypeName, ImGuiTreeNodeFlags.CollapsingHeader))
                {

                    foreach (CargoItemVM item in storetype.CargoItems)
                    {
                        bool isSelected = SelectedCargoVM == item;
                        if (ImGui.Selectable(item.ItemName, isSelected))
                        {
                            SelectedCargoVM = item;
                            CargoItemSelectedEvent.Invoke(this);
                        }
                        ImGui.SameLine();
                        ImGui.Text(item.ItemWeightPerUnit);
                        ImGui.SameLine();
                        ImGui.Text(item.NumberOfItems);
                        ImGui.SameLine();
                        ImGui.Text(item.TotalWeight);

                        ImGui.Text("+" + item.ItemIncomingAmount.ToString());
                        ImGui.SameLine();
                        ImGui.Text(item.GetIncomingWeight());
                        ImGui.SameLine();
                        ImGui.Text("-" + item.ItemOutgoingAmount.ToString());
                        ImGui.SameLine();
                        ImGui.Text(item.GetOutgoungWeight());

                    }
                }
            }
            ImGui.EndChild();
        }

    }

    public class CargoTransfer : PulsarGuiWindow
    {
        StaticDataStore _staticData;
        EntityState _selectedEntity1;
        EntityState _selectedEntity2;

        CargoListPannelComplex _cargoList1;
        CargoListPannelComplex CargoListLeft
        {
            get { return _cargoList1; }
            set { _cargoList1 = value; value.CargoItemSelectedEvent += OnCargoItemSelectedEvent; }
        }
        CargoListPannelComplex _cargoList2;
        CargoListPannelComplex CargoListRight
        {
            get { return _cargoList2; }
            set { _cargoList2 = value; value.CargoItemSelectedEvent += OnCargoItemSelectedEvent; }
        }
        CargoListPannelComplex SelectedCargoPannel;
        CargoListPannelComplex UnselectedCargoPannel;
        bool _hasCargoAbilityLeft;
        bool _hasCargoAbilityRight;

        public static CargoTransfer GetInstance(StaticDataStore staticData, EntityState selectedEntity1)
        {
            CargoTransfer instance;
            //if (selectedEntity1.CmdRef == null)
            //    selectedEntity1.CmdRef = CommandReferences.CreateForEntity(_state.Game, selectedEntity1.Entity);
            if (!_state.LoadedWindows.ContainsKey(typeof(CargoTransfer)))
            {
                instance = new CargoTransfer
                {
                    _staticData = staticData,
                    _selectedEntity1 = selectedEntity1
                };
            }
            else
            {
                instance = (CargoTransfer)_state.LoadedWindows[typeof(CargoTransfer)];
                instance._selectedEntity1 = selectedEntity1;

            }
            if (instance._selectedEntity1.Entity.HasDataBlob<CargoStorageDB>())
            {
                instance.CargoListLeft = new CargoListPannelComplex(staticData, selectedEntity1);
                instance._hasCargoAbilityLeft = true;
            }
            else
                instance._hasCargoAbilityLeft = false;



            if (instance._selectedEntity2 != null && instance._selectedEntity1.Entity.HasDataBlob<CargoStorageDB>())
            {
                if (!instance._hasCargoAbilityRight)
                    instance.CargoListRight = new CargoListPannelComplex(staticData, instance._selectedEntity2);
                instance._hasCargoAbilityRight = true;
            }
            else
                instance._hasCargoAbilityRight = false;
            return instance;
        }

        internal void Set2ndCargo(EntityState entity)
        {
            if (_selectedEntity1.Entity.HasDataBlob<CargoStorageDB>())
            {
                _selectedEntity2 = entity;
                CargoListRight = new CargoListPannelComplex(_staticData, _selectedEntity2);
                _hasCargoAbilityRight = true;
            }
        }

        void OnCargoItemSelectedEvent(CargoListPannelComplex cargoPannel)
        {
            SelectedCargoPannel = cargoPannel;
            if (cargoPannel == CargoListLeft)
                UnselectedCargoPannel = CargoListRight;
            else UnselectedCargoPannel = CargoListLeft;
            UnselectedCargoPannel.SelectedCargoVM = null;
            
        }


        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if(button == MouseButtons.Primary)
            {
                Set2ndCargo(entity); 
            }
        }

        private void MoveItems(long amount)
        {
            var selectedCargoVM = SelectedCargoPannel.SelectedCargoVM;
            var selectedCargoItem = selectedCargoVM.CargoableItem;
            if (SelectedCargoPannel == CargoListRight) //move item left
            {
                SelectedCargoPannel.AddUICargoOut(selectedCargoVM.CargoableItem, amount);
                UnselectedCargoPannel.AddUICargoIn(selectedCargoVM.CargoableItem, amount);
            }
            else //left side is selected, move item right
            {
                //if(CargoListRight.AmountInStoreAndMove(selectedCargoItem.CargoTypeID, selectedCargoItem.ID) > amount) lets not check, we should allow it to go into the red in the ui. 
                SelectedCargoPannel.AddUICargoOut(selectedCargoVM.CargoableItem, amount);
                UnselectedCargoPannel.AddUICargoIn(selectedCargoVM.CargoableItem, amount);
            }
        }

        private void ActionXferOrder()
        { }
        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("Cargo", ref IsActive, _flags))
                {
                    if (_hasCargoAbilityLeft)
                    {
                        CargoListLeft.Display();
                        ImGui.SameLine();
                        ImGui.BeginChild("xfer", new System.Numerics.Vector2(100, 200));
                        ImGui.Text("Transfer");

                        if (SelectedCargoPannel != null && SelectedCargoPannel.SelectedCargoVM != null)
                        {
                            if (ImGui.Button("x100"))
                            { MoveItems(100); }
                            ImGui.SameLine();
                            if (ImGui.Button("x10"))
                            { MoveItems(10); }
                            ImGui.SameLine();
                            if (ImGui.Button("x1"))
                            { MoveItems(1); }
                        }
                        if(ImGui.Button("Action Order"))
                        { ActionXferOrder(); }
                        ImGui.EndChild();
                        ImGui.SameLine();
                        if (_hasCargoAbilityRight)
                            CargoListRight.Display();
                        else
                            ImGui.Text("Select Entity For Transfer");
                    }

                }
                ImGui.End();
            }
        }
    }


}

