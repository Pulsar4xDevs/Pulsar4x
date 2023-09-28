using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.SDL2UI;

using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;
using Pulsar4X.Atb;
using Pulsar4X.Datablobs;

namespace Pulsar4X.ImGuiNetUI
{

    public class FireControl : PulsarGuiWindow
    {
        private EntityState _orderEntityState;
        private Entity _orderEntity => _orderEntityState.Entity;
        SensorContact[] _allSensorContacts = new SensorContact[0];
        string[] _ownEntityNames = new string[0];
        EntityState[] _ownEntites = new EntityState[0];
        private bool _showOwnAsTarget;

        private string _factionID;

        //private int _dragDropIndex;
        private string _dragDropGuid;


        private int _selectedFCIndex = -1;
        private FireControlAbilityState[] _fcStates = new FireControlAbilityState[0];
        private Vector2[] _fcSizes = new Vector2[0];

        private Dictionary<string, WeaponState> _wpnDict = new ();
        private WeaponState[] _allWeaponsStates = new WeaponState[0];

        private Dictionary<string, string> _weaponNames = new ();
        private Dictionary<string, (float reload, float min, float max)> _reloadState = new ();
        //private WeaponState[] _unAssignedWeapons = new WeaponState[0];
        private OrdnanceDesign[] _allOrdnanceDesigns = new OrdnanceDesign[0];
        Dictionary<int, long> _storedOrdnance = new ();
        private bool _showOnlyCargoOrdnance = true;


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
                thisitem.HardRefresh(orderEntity);
                thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
            }
            else
            {
                thisitem = (FireControl)_uiState.LoadedWindows[typeof(FireControl)];
                if (thisitem._orderEntityState != orderEntity)
                {
                    thisitem.HardRefresh(orderEntity);
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
            for (int i = 0; i < _fcStates.Length; i++)
            {
                var fc = _fcStates[i];


                var startPoint = ImGui.GetCursorPos();
                BorderGroup.Begin(fc.Name + "##" + i);
                //ImGui.BeginChild("fcddarea"+i) ;//("##fcddarea"+i, new Vector2(_fcSizes[i].X -2, _fcSizes[i].Y - 2), false);

                ImGui.Text(fc.TargetName);
                if (fc.Target != null)
                {
                    if (fc.IsEngaging)
                    {
                        if (ImGui.Button("Cease Fire"))
                            OpenFire(_fcStates[i].ComponentInstance.UniqueID, SetOpenFireControlOrder.FireModes.CeaseFire);
                    }
                    else
                    {
                        if (ImGui.Button("Open Fire"))
                            OpenFire(_fcStates[i].ComponentInstance.UniqueID, SetOpenFireControlOrder.FireModes.OpenFire);
                    }
                }

                foreach (var wpn in fc.ChildrenStates)
                {
                    ImGui.Selectable(_weaponNames[wpn.ID]);
                    //wpn.ComponentInstance.GetAbilityState<WeaponState>().InternalMagCurAmount
                    var reloadState = _reloadState[wpn.ID];
                    ImGui.Text("Reload:" + reloadState.reload + "/" + reloadState.min + "/" + reloadState.max);

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
                        bool isDroppingOrdnance = false;
                        unsafe
                        {
                            isDroppingOrdnance = acceptPayload.NativePtr != null;
                        }

                        if (isDroppingOrdnance)
                            SetOrdnance((WeaponState)wpn, _dragDropGuid);

                    }

                }

                //ImGui.EndChild();
                BorderGroup.End();
                _fcSizes[i] = BorderGroup.GetSize;
                ImGui.SetCursorPos(startPoint);
                ImGui.InvisibleButton("fcddarea" + i, _fcSizes[i]);

                if (ImGui.BeginDragDropTarget())
                {
                    var acceptPayload = ImGui.AcceptDragDropPayload("AssignSensorAsTarget");
                    bool isDroppingSensorTarget = false;
                    unsafe
                    {
                        isDroppingSensorTarget = acceptPayload.NativePtr != null;
                    }

                    acceptPayload = ImGui.AcceptDragDropPayload("AssignOwnAsTarget");
                    bool isDroppingOwnTarget = false;
                    unsafe
                    {
                        isDroppingOwnTarget = acceptPayload.NativePtr != null;
                    }

                    acceptPayload = ImGui.AcceptDragDropPayload("AssignWeapon");
                    bool isDroppingWeapon = false;
                    unsafe
                    {
                        isDroppingWeapon = acceptPayload.NativePtr != null;
                    }

                    if (isDroppingSensorTarget)
                        SetTarget(fc, _dragDropGuid);
                    if (isDroppingOwnTarget)
                        SetTarget(fc, _dragDropGuid);
                    if (isDroppingWeapon)
                        SetWeapon(_dragDropGuid, fc);
                    ImGui.EndDragDropTarget();
                }
                ImGui.NewLine();
            }

        }

        void UnAssignedWeapons()
        {
            Vector2 unAssStartPos = ImGui.GetCursorPos();
            BorderGroup.Begin("Un Assigned Weapons");
            {
                for (int i = 0; i < _allWeaponsStates.Length; i++)
                {
                    var wpn = _allWeaponsStates[i];



                    if (wpn.ParentState == null)
                    {
                        ImGui.Selectable(_weaponNames[wpn.ID] + "##" + wpn.ComponentInstance.UniqueID);
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

                            if (isDropping)
                                SetOrdnance(wpn, _dragDropGuid);

                            ImGui.EndDragDropTarget();
                        }
                    }
                }
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

                if (isDroppingWeapon)
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
            BorderGroup.Begin("Set Target:");
            ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

            for (int i = 0; i < _allSensorContacts.Length; i++)
            {
                var contact = _allSensorContacts[i];
                ImGui.Selectable(contact.Name);
                if (ImGui.BeginDragDropSource())
                {
                    ImGui.Text(contact.Name);
                    unsafe
                    {
                        int* tesnum = &i;
                        ImGui.SetDragDropPayload("AssignSensorAsTarget", new IntPtr(tesnum), sizeof(int));

                        _dragDropGuid = contact.ActualEntityGuid;
                    }

                    ImGui.EndDragDropSource();
                }
            }

            if (_showOwnAsTarget)
            {
                for (int i = 0; i < _ownEntites.Length; i++)
                {
                    var contact = _ownEntites[i];
                    ImGui.Selectable(contact.Name);
                    if (ImGui.BeginDragDropSource())
                    {
                        ImGui.Text(contact.Name);
                        unsafe
                        {
                            int* tesnum = &i;
                            ImGui.SetDragDropPayload("AssignOwnAsTarget", new IntPtr(tesnum), sizeof(int));
                            _dragDropGuid = contact.Entity.Guid;
                        }

                        ImGui.EndDragDropSource();
                    }
                }
            }

            BorderGroup.End();
        }

        void HardRefresh(EntityState orderEntity)
        {

            _orderEntityState = orderEntity;
            var instancesDB = orderEntity.Entity.GetDataBlob<ComponentInstancesDB>();
            if (orderEntity.DataBlobs.ContainsKey(typeof(FireControlAbilityDB)))
            {
                if (instancesDB.TryGetStates<FireControlAbilityState>(out _fcStates))
                {

                }

                _fcSizes = new Vector2[_fcStates.Length];
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

            var sysstate = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            var contacts = sysstate.SystemContacts;
            _allSensorContacts = new SensorContact[0];
            if (contacts != null)
            {
                _allSensorContacts = contacts.GetAllContacts().ToArray();
            }
            _ownEntites = sysstate.EntityStatesWithPosition.Values.ToArray();
            RefreshWpnNamesCashe();
            RefreshReloadStateCashe();
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

            foreach (var cargoType in ctypes)
            {
                if(!_orderEntity.HasDataBlob<VolumeStorageDB>())
                    continue;
                if (_orderEntity.GetDataBlob<VolumeStorageDB>().TypeStores.ContainsKey(cargoType))
                {
                    var shipOrdnances = _orderEntity.GetDataBlob<VolumeStorageDB>().TypeStores[cargoType].CurrentStoreInUnits;

                    foreach (var ordType in shipOrdnances)
                        _storedOrdnance[ordType.Key] = ordType.Value;
                }
            }

            RefreshWpnNamesCashe();
            RefreshReloadStateCashe();
        }

        void RefreshWpnNamesCashe()
        {
            _weaponNames = new Dictionary<string, string>();
            for (int i = 0; i < _allWeaponsStates.Length; i++)
            {

                var wpn = _allWeaponsStates[i];

                var assOrdName = "";
                string assOrdCount = "(0)";
                if (wpn.FireWeaponInstructions.TryGetOrdnance(out var ordnanceDesign))
                {
                    assOrdName = ordnanceDesign.Name;
                    assOrdCount = "(" + _storedOrdnance[ordnanceDesign.ID] + ")";
                }

                _weaponNames[wpn.ID] = wpn.Name + "\t" + assOrdName + assOrdCount;
            }
        }

        void RefreshReloadStateCashe()
        {
            _reloadState = new Dictionary<string, (float reload, float min, float max)>();
            for (int i = 0; i < _allWeaponsStates.Length; i++)
            {
                var wpnState = _allWeaponsStates[i];
                GenericWeaponAtb wpnAtb = wpnState.ComponentInstance.Design.GetAttribute<GenericWeaponAtb>();
                var reloadAmount = wpnState.InternalMagCurAmount;
                var reloadMax = wpnAtb.InternalMagSize;
                var reloadMin = wpnAtb.AmountPerShot * wpnAtb.MinShotsPerfire;
                _reloadState[wpnState.ID] = (reloadAmount, reloadMin, reloadMax);
            }
        }

        void UnSetWeapon(WeaponState wpnState)
        {
            var fcState = wpnState.ParentState;
            var curWpnIDs = fcState.GetChildrenIDs();
            var newArry = new string[curWpnIDs.Length - 1];
            int j = 0;
            for (int i = 0; i < curWpnIDs.Length; i++)
            {
                if (curWpnIDs[i] != wpnState.ID)
                {
                    newArry[j] = curWpnIDs[i];
                    j++;
                }
            }

            SetWeapons(newArry, fcState.ID);
            //if(wpnState.ParentState == null)


        }

        void SetWeapon(string wpnID, FireControlAbilityState fcState)
        {
            //var curWpns = fcState.AssignedWeapons;
            var curWpnIDs = fcState.GetChildrenIDs();
            var newArry = new string[curWpnIDs.Length + 1];
            for (int i = 0; i < curWpnIDs.Length; i++)
            {
                newArry[i] = curWpnIDs[i];
            }

            newArry[curWpnIDs.Length] = wpnID;
            SetWeapons(newArry, fcState.ID);
        }

        void SetWeapons(string[] wpnsAssignd, string firecontrolID)
        {
            SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, firecontrolID, wpnsAssignd);
        }

        void SetOrdnance(WeaponState wpn, string ordnanceAssigned)
        {
            SetOrdinanceToWpnOrder.CreateCommand(_uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, wpn.ID, ordnanceAssigned);
            RefreshWpnNamesCashe(); //refresh this or it wont show the change till after a systemtick.
            RefreshReloadStateCashe();
        }

        void SetTarget(FireControlAbilityState fcState, string targetID)
        {
            var fcGuid = fcState.ComponentInstance.UniqueID;
            SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, fcGuid, targetID);
        }

        private void OpenFire(string fcID, SetOpenFireControlOrder.FireModes mode)
        {
            SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction, _orderEntity, fcID, mode);
        }
    }
}
