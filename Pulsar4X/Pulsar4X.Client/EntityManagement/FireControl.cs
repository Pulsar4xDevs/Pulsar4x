using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.SDL2UI;
using Vector2 = System.Numerics.Vector2;
using Pulsar4X.Atb;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;

namespace Pulsar4X.ImGuiNetUI
{

    public class FireControl : PulsarGuiWindow
    {
        private EntityState? _orderEntityState;
        private bool _showOwnAsTarget;

        //private int _dragDropIndex;
        private string? _dragDropGuid;
        private int _dragDropId;


        private List<FireControlAbilityState> _fcStates = new List<FireControlAbilityState>();

        private Dictionary<string, WeaponState> _wpnDict = new ();
        private List<WeaponState> _allWeaponsStates = new List<WeaponState>();

        private OrdnanceDesign[] _allOrdnanceDesigns = new OrdnanceDesign[0];
        Dictionary<int, long> _storedOrdnance = new ();
        private bool _showOnlyCargoOrdnance = true;
        private GenericFiringWeaponsDB _activeWeapons;

        private List<EntityState> _filteredEnemies = new ();
        private List<EntityState> _filteredFriends = new ();

        private FireControl()
        {
            _flags = ImGuiWindowFlags.None;
        }



        public static FireControl GetInstance(EntityState orderEntity)
        {
            FireControl thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FireControl)))
            {
                thisitem = new FireControl();
                thisitem.SetEntityState(orderEntity);
                thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
            }
            else
            {
                thisitem = (FireControl)_uiState.LoadedWindows[typeof(FireControl)];
                if (thisitem._orderEntityState != orderEntity)
                {
                    thisitem.SetEntityState(orderEntity);
                    thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
                }
            }

            return thisitem;
        }

        internal override void Display()
        {
            if (!IsActive)
                return;
            ImGui.SetNextWindowSize(new Vector2(600f, 400f), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Fire Control", ref IsActive, _flags))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 400);
                DisplayFC();

                UnAssignedWeapons();

                ImGui.NewLine();

                DisplayOrdnance();

                ImGui.NextColumn();

                DisplayTargetColumn();

            }
        }



        void DisplayFC()
        {
            int fcindex = 0;
            foreach(FireControlAbilityState fc in _fcStates)
            {
                var startPoint = ImGui.GetCursorPos();
                BorderGroup.Begin(fc.Name + "##" + fcindex++);
                //ImGui.BeginChild("fcddarea"+i) ;//("##fcddarea"+i, new Vector2(_fcSizes[i].X -2, _fcSizes[i].Y - 2), false);

                if (fc.Target != null && fc.Target.IsValid)
                {
                    ImGui.Text(fc.TargetName);
                    if (fc.IsEngaging)
                    {
                        if (ImGui.Button("Cease Fire"))
                            OpenFire(fc.ComponentInstance.UniqueID, SetOpenFireControlOrder.FireModes.CeaseFire);
                    }
                    else
                    {
                        if (ImGui.Button("Open Fire"))
                            OpenFire(fc.ComponentInstance.UniqueID, SetOpenFireControlOrder.FireModes.OpenFire);
                    }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                    ImGui.Text("No target selected");
                    ImGui.PopStyleColor();
                }

                foreach (var wpn in fc.ChildrenStates)
                    ShowWeapon(_wpnDict[wpn.ID]);

                //ImGui.EndChild();
                BorderGroup.End();
                ImGui.SetCursorPos(startPoint);
                ImGui.InvisibleButton("fcddarea" + fcindex, BorderGroup.GetSize);

                if (ImGui.BeginDragDropTarget())
                {
                    var acceptPayload = ImGui.AcceptDragDropPayload("AssignAsTarget");
                    bool isDroppingSensorTarget = false;
                    unsafe
                    {
                        isDroppingSensorTarget = acceptPayload.NativePtr != null;
                    }

                    acceptPayload = ImGui.AcceptDragDropPayload("AssignWeapon");
                    bool isDroppingWeapon = false;
                    unsafe
                    {
                        isDroppingWeapon = acceptPayload.NativePtr != null;
                    }

                    if (isDroppingSensorTarget)
                        SetTarget(fc, _dragDropId);
                    if (isDroppingWeapon && !string.IsNullOrEmpty(_dragDropGuid))
                        SetWeapon(_wpnDict[_dragDropGuid], fc);
                    ImGui.EndDragDropTarget();
                }
                ImGui.NewLine();
            }

        }

        string GetRichWeaponName(WeaponState wpnState)
        {
            string weaponname = wpnState.Name + "\t";
            if (wpnState.FireWeaponInstructions.TryGetOrdnance(out var ordnanceDesign))
            {
                weaponname += ordnanceDesign.Name;
                weaponname += "(" + _storedOrdnance[ordnanceDesign.ID] + ")";
            }
            return weaponname;
        }


        void ShowWeapon(WeaponState wpn, int i = 0)
        {
            string id = wpn.ComponentInstance.UniqueID;
            int nameSize = 128;

            GenericWeaponAtb wpnAtb = wpn.ComponentInstance.Design.GetAttribute<GenericWeaponAtb>();
            int reloadAmount = wpn.InternalMagCurAmount;
            int reloadMax = wpnAtb.InternalMagSize;
            int reloadMin = wpnAtb.AmountPerShot * wpnAtb.MinShotsPerfire;

            ImGuiSelectableFlags flags = ImGuiSelectableFlags.None;
            var cpos = ImGui.GetCursorPos();

            ImGui.Text(GetRichWeaponName(wpn));
            var selectableSize = new Vector2(ImGui.GetColumnWidth(0) - 24, ImGui.GetTextLineHeightWithSpacing());
            Vector2 progsize = new Vector2(selectableSize.X - nameSize, selectableSize.Y);
            float reloadAmountPerc = (reloadAmount / reloadMax);
            ImGui.SetCursorPos(new Vector2( nameSize, cpos.Y));
            ImGui.ProgressBar(reloadAmountPerc, progsize);

            //draw an invisible button over everything for the drag and drop source.
            ImGui.SetCursorPos(cpos);
            ImGui.InvisibleButton(id, selectableSize);

            if (ImGui.BeginDragDropSource())
            {
                ImGui.Text(wpn.Name);
                unsafe
                {
                    int* tesnum = &i;
                    ImGui.SetDragDropPayload("AssignWeapon", new IntPtr(tesnum), sizeof(int));
                    _dragDropGuid = wpn.ID;
                }

                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                ImGuiPayloadPtr acceptPayload = ImGui.AcceptDragDropPayload("AssignOrdnance");
                bool isDropping = false;
                unsafe
                {
                    isDropping = acceptPayload.NativePtr != null;
                }

                if (isDropping && !string.IsNullOrEmpty(_dragDropGuid))
                    SetOrdnance(wpn, _dragDropGuid);

                ImGui.EndDragDropTarget();
            }
        }

        void UnAssignedWeapons()
        {
            Vector2 unAssStartPos = ImGui.GetCursorPos();
            BorderGroup.Begin("Un-Assigned Weapons");
            {
                foreach (WeaponState wpn in _allWeaponsStates.Where(wpn => wpn.ParentState == null))
                        ShowWeapon(wpn);
            }
            BorderGroup.End();
            var unAssSize = BorderGroup.GetSize;

            ImGui.SetCursorPos(unAssStartPos);
            ImGui.InvisibleButton("unassDnDArea", unAssSize);

            if (ImGui.BeginDragDropTarget())
            {

                var acceptPayload = ImGui.AcceptDragDropPayload("AssignWeapon");
                bool isDroppingWeapon = false;
                unsafe
                {
                    isDroppingWeapon = acceptPayload.NativePtr != null;
                }

                if (isDroppingWeapon && !string.IsNullOrEmpty(_dragDropGuid))
                    UnSetWeapon(_wpnDict[_dragDropGuid]);

                ImGui.EndDragDropTarget();
                ImGui.NewLine();
            }
        }

        void DisplayOrdnance()
        {
            BorderGroup.Begin("Ordnance");
            {
                for (int i = 0; i < _allOrdnanceDesigns.Length; i++)
                {
                    var ord = _allOrdnanceDesigns[i];
                    if (_storedOrdnance.ContainsKey(ord.ID))
                    {
                        ImGui.Selectable(ord.Name);
                        if (ImGui.BeginDragDropSource())
                        {
                            ImGui.Selectable(ord.Name);
                            unsafe
                            {
                                int* tesnum = &i;
                                ImGui.SetDragDropPayload("AssignOrdnance", new IntPtr(tesnum), sizeof(int));
                                _dragDropGuid = ord.UniqueID;
                            }

                            ImGui.EndDragDropSource();
                        }
                    }
                }
            }
            BorderGroup.End();
        }

        void DisplayTargetColumn()
        {
            if(_orderEntityState == null || _orderEntityState.StarSystemId == null) return;

            BorderGroup.Begin("Set Target:");
            ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

            ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
            foreach(var entityState in _filteredEnemies)
            {
                DisplayTarget(entityState);
            }
            ImGui.PopStyleColor();

            if (_showOwnAsTarget)
            {
                foreach(var entityState in _filteredFriends)
                {
                    DisplayTarget(entityState);
                }
            }

            BorderGroup.End();
        }

        private void DisplayTarget(EntityState entityState)
        {
            int i = entityState.Id;
            ImGui.Selectable(entityState.Name);
            if (ImGui.BeginDragDropSource())
            {
                ImGui.Text(entityState.Name);
                unsafe
                {
                    int* tesnum = &i;
                    ImGui.SetDragDropPayload("AssignAsTarget", new IntPtr(tesnum), sizeof(int));
                    _dragDropId = entityState.Id;
                }

                ImGui.EndDragDropSource();
            }
        }

        void SetEntityState(EntityState orderEntity)
        {
            // Remove old event listeners
            // Need to re-bind them incase the system has changed
            if(_orderEntityState != null)
            {
                _uiState.StarSystemStates[_orderEntityState.StarSystemId].OnEntityAdded -= SystemEntityAdded;
                _uiState.StarSystemStates[_orderEntityState.StarSystemId].OnEntityRemoved -= SystemEntityRemoved;
            }

            if(orderEntity.StarSystemId == null) return;

            _orderEntityState = orderEntity;

            _uiState.StarSystemStates[_orderEntityState.StarSystemId].OnEntityAdded += SystemEntityAdded;
            _uiState.StarSystemStates[_orderEntityState.StarSystemId].OnEntityRemoved += SystemEntityRemoved;

            var instancesDB = orderEntity.GetDataBlob<ComponentInstancesDB>();
            if (orderEntity.HasDataBlob(typeof(FireControlAbilityDB)))
            {
                instancesDB.TryGetStates<FireControlAbilityState>(out _fcStates);
            }
            else
            {
                IsActive = false;
                return;
            }


            if (instancesDB.TryGetStates<WeaponState>(out _allWeaponsStates))
            {
                foreach (var wpn in _allWeaponsStates)
                {
                    _wpnDict[wpn.ID] = wpn;
                }
            }

            if (orderEntity.TryGetDataBlob(out GenericFiringWeaponsDB activeWeapons))
            {
                _activeWeapons = activeWeapons;
            }
            else
            {
                //_activeWeapons = new GenericFiringWeaponsDB(new ComponentInstance[0]);
            }

            RefreshTargetLists();
        }

        private void SystemEntityRemoved(SystemState systemState, int entityId)
        {
            RefreshTargetLists();
        }

        private void SystemEntityAdded(SystemState systemState, Entity entity)
        {
            RefreshTargetLists();
        }

        private void RefreshTargetLists()
        {
            if(_orderEntityState == null || _orderEntityState.StarSystemId == null) return;

            var systemState = _uiState.StarSystemStates[_orderEntityState.StarSystemId];
            _filteredEnemies = systemState.GetFilteredEntities(DataStructures.EntityFilter.Hostile, _orderEntityState.FactionId, typeof(PositionDB));
            _filteredFriends = systemState.GetFilteredEntities(DataStructures.EntityFilter.Friendly, _orderEntityState.FactionId, typeof(PositionDB));
        }

        public override void OnSystemTickChange(DateTime newdate)
        {
            _allOrdnanceDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns.Values.ToArray();
            var ctypes = new List<string>(); //there are likely to be not very many of these, proibly only one.
            foreach (var ordDes in _allOrdnanceDesigns)
            {
                if (!ctypes.Contains(ordDes.CargoTypeID))
                    ctypes.Add(ordDes.CargoTypeID);
            }

            if(_orderEntityState == null) return;

            foreach (var cargoType in ctypes)
            {
                if(!_orderEntityState.HasDataBlob<VolumeStorageDB>())
                    continue;
                if (_orderEntityState.GetDataBlob<VolumeStorageDB>().TypeStores.ContainsKey(cargoType))
                {
                    var shipOrdnances = _orderEntityState.GetDataBlob<VolumeStorageDB>().TypeStores[cargoType].CurrentStoreInUnits;

                    foreach (var ordType in shipOrdnances)
                        _storedOrdnance[ordType.Key] = ordType.Value;
                }
            }

        }

        void UnSetWeapon(WeaponState wpnState)
        {
            SetWeapon(wpnState);
        }

        void SetWeapon(WeaponState wpnState, FireControlAbilityState? fcState = null)
        {
            List<string> WpnIDs;
            //var curWpns = fcState.AssignedWeapons;
            if (fcState != null)
            {
                WpnIDs = fcState.GetChildrenIDs<IList>();
                WpnIDs.Add(wpnState.ID);
                SetWeapons(WpnIDs, fcState.ID);
                return;
            }
            else if(wpnState.ParentState != null)
            {
                WpnIDs = wpnState.ParentState.GetChildrenIDs<IList>();
                WpnIDs.Remove(wpnState.ID);
                SetWeapons(WpnIDs, wpnState.ParentState.ID);
            }
        }

        void SetWeapons(List<string> wpnsAssignd, string firecontrolID)
        {
            if(_orderEntityState != null)
                SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Id, _orderEntityState.Id, firecontrolID, wpnsAssignd);
        }

        void SetOrdnance(WeaponState wpn, string ordnanceAssigned)
        {
            if(_orderEntityState != null)
                SetOrdinanceToWpnOrder.CreateCommand(_uiState.PrimarySystemDateTime, _uiState.Faction, _orderEntityState.Id, wpn, ordnanceAssigned);
        }

        void SetTarget(FireControlAbilityState fcState, int targetID)
        {
            var fcGuid = fcState.ComponentInstance.UniqueID;
            if(_orderEntityState != null)
                SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Id, _orderEntityState.Id, fcGuid, targetID);
        }

        private void OpenFire(string fcID, SetOpenFireControlOrder.FireModes mode)
        {
            if(_orderEntityState != null)
                SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction.Id, _orderEntityState.Id, fcID, mode);
        }
    }
}
