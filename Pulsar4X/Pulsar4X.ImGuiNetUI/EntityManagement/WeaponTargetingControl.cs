using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Linq;
using System.Collections.Concurrent;

namespace Pulsar4X.SDL2UI
{
    public class WeaponTargetingControl : PulsarGuiWindow
    {
        Entity _orderingEntity;
        SystemState _sysState;


        Dictionary<Guid, string> _weaponNames = new Dictionary<Guid, string>();
        List<Guid> _unAssignedWeapons = new List<Guid>();

        FireControlAbilityDB _shipFCDB;
        int _selectedItemIndex = -1;
        Guid _selectedFCGuid;
        List<Guid> _selectedFCAssignedWeapons = new List<Guid>();
        System.Numerics.Vector2 _selectableBtnSize = new System.Numerics.Vector2(100, 18);

        Dictionary<Guid, string> _systemEntityNames = new Dictionary<Guid, string>();
        Dictionary<Guid, SensorContact> _sensorContacts = new Dictionary<Guid, SensorContact>();

        private WeaponTargetingControl(EntityState entity)
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            _orderingEntity = entity.Entity;
        }

        private void OpenFire(Guid fcID, SetOpenFireControlOrder.FireModes mode)
        {
            SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction, _orderingEntity, fcID, mode);
        }

        internal static WeaponTargetingControl GetInstance(EntityState entity)
        {
            WeaponTargetingControl instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(WeaponTargetingControl)))
            {
                instance = new WeaponTargetingControl(entity);
            }
            else
                instance = (WeaponTargetingControl)_uiState.LoadedWindows[typeof(WeaponTargetingControl)];
            instance.SetOrderEntity(entity);
            instance._sysState = _uiState.StarSystemStates[_uiState.SelectedSystem.Guid];
            


            return instance;
        }

        internal void SetOrderEntity(EntityState entity)
        {
            _orderingEntity = entity.Entity;
            _shipFCDB = _orderingEntity.GetDataBlob<FireControlAbilityDB>();
            _weaponNames = new Dictionary<Guid, string>();
            _unAssignedWeapons = new List<Guid>();
            _systemEntityNames = new Dictionary<Guid, string>();
            _sensorContacts = new Dictionary<Guid, SensorContact>();

            var instancesDB = entity.Entity.GetDataBlob<ComponentInstancesDB>();
            
            if( instancesDB.TryGetComponentsByAttribute<BeamFireControlAtbDB>(out var fcinstances))
            {
                foreach (var instanceData in fcinstances)
                {
                    var fcdata = instanceData.Design.GetAttribute<BeamFireControlAtbDB>();
                    
                }
                
            }
        
            
  
            for (int weaponInstanceIndex = 0; weaponInstanceIndex < _shipFCDB.WeaponInstances.Count; weaponInstanceIndex++)
            {
                var weaponInstanace = _shipFCDB.WeaponInstances[weaponInstanceIndex];
                
                string wpnname = weaponInstanace.GetName();
                _weaponNames.Add(weaponInstanace.ID, wpnname);
                //_weapons.Add(wpnname);
                if (weaponInstanace.GetAbilityState<WeaponState>().Master == null)
                    _unAssignedWeapons.Add(weaponInstanace.ID);
            }
            foreach (var item in _uiState.SelectedSystem.GetSensorContacts(_uiState.Faction.Guid).GetAllContacts())
            {
                var entityItem = item.ActualEntity;
                string name = entityItem.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                _systemEntityNames.Add(item.ActualEntityGuid, name);
                _sensorContacts.Add(item.ActualEntityGuid, item);
            }
            /*
            foreach (var item in _uiState.FactionUIState.GetEntitiesForSystem(_orderingEntity.Manager))
            {
                if (item.HasDataBlob<NameDB>() && item.HasDataBlob<PositionDB>())
                {
                    string name = item.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                    _systemEntityNames.Add(item.ID, name);
                    _systemEntites.Add(item.ID, item);

                }
            }*/

        }

        public override void OnSystemTickChange(DateTime newDateTime)
        {
            OnPhysicsUpdate();
        }


        internal void OnPhysicsUpdate()
        {



        }

        void PreFrameSetup()
        {
            foreach (var changeData in _sysState.SensorChanges)
            {
                HandleChangeData(changeData);
            }
            foreach (var changeData in _sysState.SystemChanges)
            {
                HandleChangeData(changeData);
            }

            //Code below shouldn't be neccisary but for some reason,
            //the code above was not catching all the removed objects.
            foreach (var item in _sysState.EntitysToBin) 
            {
                if (_sensorContacts.ContainsKey(item))
                    _sensorContacts.Remove(item);
                if (_systemEntityNames.ContainsKey(item))
                    _systemEntityNames.Remove(item);
            }

        }

        void HandleChangeData(EntityChangeData changeData)
        {
            if (changeData.Entity.IsValid)
            {
                switch (changeData.ChangeType)
                {
                    case EntityChangeData.EntityChangeType.EntityAdded:

                        string name = changeData.Entity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                        _systemEntityNames.Add(changeData.Entity.Guid, name);
                        _sensorContacts[changeData.Entity.Guid] = _sysState.SystemContacts.GetSensorContact(changeData.Entity.Guid);
                        break;
                    case EntityChangeData.EntityChangeType.EntityRemoved:
                        _systemEntityNames.Remove(changeData.Entity.Guid);
                        _sensorContacts.Remove(changeData.Entity.Guid);
                        break;
                }
            }
        }

        internal override void Display()
        {
            if (_sysState != null)
                PreFrameSetup();
            if (IsActive)
            {
                var size = new System.Numerics.Vector2(200, 100);
                var pos = new System.Numerics.Vector2(_uiState.MainWinSize.X / 2 - size.X / 2, _uiState.MainWinSize.Y / 2 - size.Y / 2);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

                if (ImGui.Begin("Weapon Targeting", ref IsActive, _flags))
                {
                    
                    int selectable = 0;
                    bool selected = false;
                    ImGui.BeginGroup();
                    foreach (var fc in _shipFCDB.FireControlInstances)
                    {
                        var fcAbility = fc.GetAbilityState<FireControlAbilityState>();
                        if (ImGui.Selectable("FC " + selectable, selected, ImGuiSelectableFlags.None, _selectableBtnSize))
                        {
                            _selectedItemIndex = selectable;
                            _selectedFCGuid = fc.ID;
                            _selectedFCAssignedWeapons = new List<Guid>();
                            foreach (var item in fcAbility.AssignedWeapons)
                            {
                                _selectedFCAssignedWeapons.Add(item.ID);
                            }

                        }
                        /*
                        ImGui.Text("Assigned Weapons: ");
                        foreach (var weapon in fc.AssignedWeapons)
                        {
                            bool isEnabled = weapon.GetDataBlob<ComponentInstanceInfoDB>().IsEnabled;
                            if (ImGui.Checkbox(weaponNames[weapon.ID], ref isEnabled))
                            {
                                //give an enable/disable order. 
                            }
                        }*/
                        ImGui.Text("Target: ");
                        ImGui.SameLine();
                        if (fcAbility.Target == null || !fcAbility.Target.IsValid)
                            ImGui.Text("No Target");
                        else
                            ImGui.Text(fcAbility.Target.GetDataBlob<NameDB>().GetName(_uiState.Faction));
                        selectable++;

                        if (fcAbility.IsEngaging)
                        {
                            if (ImGui.Button("Cease Fire"))
                                OpenFire(fc.ID, SetOpenFireControlOrder.FireModes.CeaseFire);
                        }
                        else
                        {
                            if (ImGui.Button("Open Fire"))
                                OpenFire(fc.ID, SetOpenFireControlOrder.FireModes.OpenFire);
                        }
                    }
                    ImGui.EndGroup();
                    if (_selectedItemIndex > -1)
                    {

                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        {
                            ImGui.BeginGroup();
                            //ImGui.BeginChild("AssignedWeapons", true);

                            ImGui.Text("Assigned Weapons");
                            foreach (var wpn in _selectedFCAssignedWeapons.ToArray())
                            {
                                if (ImGui.Button(_weaponNames[wpn]))
                                {
                                    
                                    _unAssignedWeapons.Add(wpn);
                                    _selectedFCAssignedWeapons.Remove(wpn);
                                    
                                }
                            }
                            ImGui.EndGroup();
                            ImGui.BeginGroup();
                            //ImGui.EndChild();
                            //ImGui.BeginChild("Un Assigned Weapons", true);

                            ImGui.Text("Un Assigned Weapons");
                            foreach (var wpn in _unAssignedWeapons.ToArray())
                            {
                                if (ImGui.Button(_weaponNames[wpn]))
                                {
                                    _selectedFCAssignedWeapons.Add(wpn);
                                    _unAssignedWeapons.Remove(wpn);
                                    SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderingEntity.Guid, _selectedFCGuid, _selectedFCAssignedWeapons.ToArray());
                                }
                            }
                            ImGui.EndGroup();
                            //ImGui.EndChild();

                        }
                        ImGui.EndGroup();
                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        {
                            ImGui.Text("Set Target");
                            foreach (var item in _systemEntityNames)
                            {
                                if (ImGui.SmallButton(item.Value))
                                {
                                    SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderingEntity.Guid, _selectedFCGuid, item.Key);
                                }
                            }
                        }
                        ImGui.EndGroup();
                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        {
                            ImGui.Text("Range in AU");
                            foreach (var item in _sensorContacts)
                            {
                                var targetEntity = _sensorContacts[item.Key];
                                double distance = _orderingEntity.GetDataBlob<PositionDB>().GetDistanceTo_AU(targetEntity.Position);
                                ImGui.Text(distance.ToString());
                            }

                        }
                        ImGui.EndGroup();
                    }
                }
                ImGui.End();
            }
        }
    }
}
