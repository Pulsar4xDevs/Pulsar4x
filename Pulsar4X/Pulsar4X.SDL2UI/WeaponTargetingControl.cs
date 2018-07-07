using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class WeaponTargetingControl : PulsarGuiWindow
    {
        Entity _orderingEntity;


        Dictionary<Guid, string> _weaponNames = new Dictionary<Guid, string>();
        List<Guid> _unAssignedWeapons = new List<Guid>();

        FireControlAbilityDB _shipFCDB;
        int _selectedItemIndex = -1;
        Guid _selectedFC;
        List<Guid> _selectedFCAssignedWeapons = new List<Guid>();
        ImVec2 _selectableBtnSize = new ImVec2(100, 18);

        Dictionary<Guid, string> systemEntityNames = new Dictionary<Guid, string>();  
        //List<Guid> knownSystemEntites = new List<Guid>();

        private WeaponTargetingControl(EntityState entity)
        {
            _orderingEntity = entity.Entity;
            IsActive = true;
        }

        internal static WeaponTargetingControl GetInstance(EntityState entity)
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(WeaponTargetingControl)))
            {
                return new WeaponTargetingControl(entity);
            }
            var instance = (WeaponTargetingControl)_state.LoadedWindows[typeof(WeaponTargetingControl)];
            instance.SetOrderEntity(entity);
            return instance;
        }

        internal void SetOrderEntity(EntityState entity)
        {
            _orderingEntity = entity.Entity;
            _shipFCDB = _orderingEntity.GetDataBlob<FireControlAbilityDB>();
            _weaponNames = new Dictionary<Guid, string>();
            _unAssignedWeapons = new List<Guid>();
            for (int fcInstanceIndex = 0; fcInstanceIndex < _shipFCDB.FireControlInsances.Count; fcInstanceIndex++)
            {
                var fireControlInstance = _shipFCDB.FireControlComponents[fcInstanceIndex].GetDataBlob<ComponentInstanceInfoDB>();
            }
            for (int weaponInstanceIndex = 0; weaponInstanceIndex < _shipFCDB.WeaponInstanceStates.Count; weaponInstanceIndex++)
            {
                var weaponInstanace = _shipFCDB.WeaponComponents[weaponInstanceIndex].GetDataBlob<ComponentInstanceInfoDB>();
                string wpnname = weaponInstanace.GetName();
                _weaponNames.Add(weaponInstanace.OwningEntity.Guid, wpnname);
                //_weapons.Add(wpnname);
                if (weaponInstanace.OwningEntity.GetDataBlob<WeaponInstanceStateDB>().FireControl == null)
                    _unAssignedWeapons.Add(weaponInstanace.OwningEntity.Guid);
            }
            foreach (var item in _state.FactionUIState.GetEntitiesForSystem(_orderingEntity.Manager))
            {
                if (item.HasDataBlob<NameDB>() && item.HasDataBlob<PositionDB>())
                {
                    string name = item.GetDataBlob<NameDB>().GetName(_state.Faction);
                    systemEntityNames.Add(item.Guid, name);
                }
            }

        }

        internal override void Display()
        {
            if (IsActive)
            {
                ImVec2 size = new ImVec2(200, 100);
                ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

                if (ImGui.Begin("Weapon Targeting", ref IsActive, _flags))
                {
                    
                    int selectable = 0;
                    bool selected = false;
                    ImGui.BeginGroup();
                    foreach (var fc in _shipFCDB.FireControlInsances)
                    {
                        

                        if (ImGui.Selectable("FC " + selectable, selected, ImGuiSelectableFlags.Default, _selectableBtnSize))
                        {
                            _selectedItemIndex = selectable;
                            _selectedFC = fc.OwningEntity.Guid;
                            _selectedFCAssignedWeapons = new List<Guid>();
                            foreach (var item in fc.AssignedWeapons)
                            {
                                _selectedFCAssignedWeapons.Add(item.Guid);
                            }

                        }
                        /*
                        ImGui.Text("Assigned Weapons: ");
                        foreach (var weapon in fc.AssignedWeapons)
                        {
                            bool isEnabled = weapon.GetDataBlob<ComponentInstanceInfoDB>().IsEnabled;
                            if (ImGui.Checkbox(weaponNames[weapon.Guid], ref isEnabled))
                            {
                                //give an enable/disable order. 
                            }
                        }*/
                        ImGui.Text("Target: ");
                        ImGui.SameLine();
                        if (fc.Target == null)
                            ImGui.Text("No Target");
                        else
                            ImGui.Text(fc.Target.GetDataBlob<NameDB>().GetName(_state.Faction));
                        selectable++;

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
                                }
                            }
                            if (ImGui.Button("Set Weapons"))
                            {
                                SetWeaponsFireControlOrder.CreateCommand(_state.Game, _state.CurrentSystemDateTime, _state.Faction.Guid, _orderingEntity.Guid, _selectedFC, _selectedFCAssignedWeapons);

                            }
                            ImGui.EndGroup();
                            //ImGui.EndChild();

                        }
                        ImGui.EndGroup();
                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        {
                            ImGui.Text("Set Target");
                            foreach (var item in systemEntityNames)
                            {
                                if (ImGui.Button(item.Value))
                                {
                                    SetTargetFireControlOrder.CreateCommand(_state.Game, _state.CurrentSystemDateTime, _state.Faction.Guid, _orderingEntity.Guid, _selectedFC, item.Key);
                                }
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
