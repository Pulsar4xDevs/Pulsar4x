using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
using Pulsar4X.ECSLib.ComponentFeatureSets.RailGun;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.ImGuiNetUI
{
    public class FireControl : PulsarGuiWindow
    {
        private EntityState _orderEntityState;
        private Entity _orderEntity { get { return _orderEntityState.Entity; } }
        
        //string[] _allWeaponNames = new string[0];
        //ComponentDesign[] _allWeaponDesigns = new ComponentDesign[0];
        //ComponentInstance[] _allWeaponInstances = new ComponentInstance[0];
        ComponentInstance[] _missileLaunchers = new ComponentInstance[0];
        WeaponState[] _mlstates = new WeaponState[0];
        ComponentInstance[] _railGuns = new ComponentInstance[0];
        WeaponState[] _rgstates = new WeaponState[0];
        WeaponState[] _beamstates = new WeaponState[0];
        ComponentInstance[] _beamWpns = new ComponentInstance[0];
        List<WeaponState> _allWeaponsstates = new List<WeaponState>();
        List<ComponentInstance> _allWeaponsinstances = new List<ComponentInstance>();

        OrdnanceDesign[] _allOrdnanceDesigns = new OrdnanceDesign[0];
        Dictionary<Guid, int> _storedOrdnance = new Dictionary<Guid, int>();
        private bool _showOnlyCargoOrdnance = true;

        SensorContact[] _allSensorContacts = new SensorContact[0];
        string[] _ownEntityNames = new string[0];
        EntityState[] _ownEntites = new EntityState[0];
        
        
        ComponentInstance[] _allFireControl = new ComponentInstance[0];
        FireControlAbilityState[] _fcState = new FireControlAbilityState[0];
        int[][] _assignedWeapons = new int[0][];

        string[] _fcTarget = new string[0];
        int _selectedfirecon = 0;



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
                if(thisitem._orderEntityState != orderEntity)
                {
                    thisitem.HardRefresh(orderEntity);
                    thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
                }
            }
            
            return thisitem;
        }
        

        public override void OnSystemTickChange(DateTime newdate)
        {
            
            for (int i = 0; i < _allFireControl.Length; i++)
            {
                _fcTarget[i] = "None";
                if (_allFireControl[i].TryGetAbilityState(out FireControlAbilityState fcstate))
                {
                    _fcState[i] = fcstate;
                    if (fcstate.Target != null)
                    {
                        _fcTarget[i] = fcstate.Target.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwner);
                    }
                }
            }

            _allOrdnanceDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns.Values.ToArray();
            var ctypes = new List<Guid>(); //there are likely to be not very many of these, proibly only one.
            foreach (var ordDes in _allOrdnanceDesigns)
            {
                if(!ctypes.Contains(ordDes.CargoTypeID))
                    ctypes.Add(ordDes.CargoTypeID);
            }

            
            foreach (var cargoType in ctypes)
            {
                var shipOrdnances = _orderEntity.GetDataBlob<CargoStorageDB>().StoredCargoTypes[cargoType].ItemsAndAmounts;
                
                foreach (var ordType in shipOrdnances.Values)
                {
                    _storedOrdnance[ordType.item.ID]= (int)ordType.amount;
                }
            }
        }
        
        void HardRefresh(EntityState orderEntity)
        {
            _orderEntityState = orderEntity;
            if(orderEntity.DataBlobs.ContainsKey(typeof(FireControlAbilityDB)))
            {
                var instancesDB = orderEntity.Entity.GetDataBlob<ComponentInstancesDB>();
            
                if( instancesDB.TryGetComponentsByAttribute<BeamFireControlAtbDB>(out var fcinstances))
                {
                    _allFireControl = new ComponentInstance[fcinstances.Count];
                    _fcTarget = new string[fcinstances.Count]; 
                    _fcState = new FireControlAbilityState[fcinstances.Count];
                    for (int i = 0; i < fcinstances.Count; i++)
                    {
                        _allFireControl[i] = fcinstances[i];
                        _fcTarget[i] = "None";
                        if (fcinstances[i].TryGetAbilityState(out FireControlAbilityState fcstate))
                        {
                            _fcState[i] = fcstate;
                            if (fcstate.Target != null)
                            {
                                _fcTarget[i] = fcstate.Target.GetDataBlob<NameDB>().GetName(orderEntity.Entity.FactionOwner);
                            }
                        }
                    }
                }

                
                if (instancesDB.TryGetComponentsByAttribute<MissileLauncherAtb>(out var mlinstances))
                {
                    _missileLaunchers = mlinstances.ToArray();
                    _mlstates = new WeaponState[mlinstances.Count];
                    for (int i = 0; i < mlinstances.Count; i++)
                    {
                        _mlstates[i] = mlinstances[i].GetAbilityState<WeaponState>();
                    }
                    
                }
                if (instancesDB.TryGetComponentsByAttribute<RailGunAtb>(out var railGuns))
                {
                    _railGuns = railGuns.ToArray();
                    _rgstates = new WeaponState[railGuns.Count];
                    for (int i = 0; i < railGuns.Count; i++)
                    {
                        _rgstates[i] = railGuns[i].GetAbilityState<WeaponState>();
                    }
                }

                if (instancesDB.TryGetComponentsByAttribute<SimpleBeamWeaponAtbDB>(out var beams))
                {
                    _beamWpns = beams.ToArray();
                    _beamstates = new WeaponState[beams.Count];
                    for (int i = 0; i < beams.Count; i++)
                    {
                        _beamstates[i] = beams[i].GetAbilityState<WeaponState>();
                    }
                }

                _allWeaponsstates.AddRange(_mlstates.ToList());
                _allWeaponsstates.AddRange(_rgstates.ToList());
                _allWeaponsstates.AddRange(_beamstates.ToList());
                _allWeaponsinstances.AddRange(_missileLaunchers.ToList());
                _allWeaponsinstances.AddRange(_railGuns.ToList());
                _allWeaponsinstances.AddRange(_beamWpns.ToList());


                for (int i = 0; i < _allWeaponsstates.Count; i++)
                {
                    _allWeaponsstates[i].WeaponComponentInstance = _allWeaponsinstances[i];
                    _allWeaponsstates[i].FireControl = _allFireControl[0];
                }
            }
            
            var sysstate = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];

            var contacts = sysstate.SystemContacts;
            _allSensorContacts = contacts.GetAllContacts().ToArray();
            _ownEntites = sysstate.EntityStatesWithPosition.Values.ToArray();
            
        }

        void OnFrameRefresh()
        {
            for (int i = 0; i < _allFireControl.Length; i++)
            {
                string tgt = "None";
                if (_allFireControl[i].TryGetAbilityState(out FireControlAbilityState fcstate))
                {
                    _fcState[i] = fcstate;
                    if (fcstate.Target != null)
                    {
                        tgt = fcstate.Target.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwner);
                    }
                }
                _fcTarget[i] = tgt;
                
            }
        }

        void SetWeapons(Guid[] wpnsAssignd)
        {
            SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, _allFireControl[_fcIndex].ID, wpnsAssignd);
        }

        void SetOrdnance(Guid wpnID, Guid ordnanceAssigned)
        {
            SetOrdinanceToWpnOrder.CreateCommand(_uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, wpnID, ordnanceAssigned);
        }

        void SetTarget(Guid targetID)
        {
            SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, _allFireControl[_fcIndex].ID, targetID);
        }
        
        private void OpenFire(Guid fcID, SetOpenFireControlOrder.FireModes mode)
        {
            SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction, _orderEntity, fcID, mode);
        }

        internal override void Display()
        {
            if (!IsActive)
                return;
            OnFrameRefresh();
            if (ImGui.Begin("Fire Control", ref IsActive, _flags))
            {
                ImGui.Columns(2);
                ImGui.GetColumnWidth(300);
                DisplayFC();
                ImGui.NextColumn();
                Display2ndColomn();
            }
        }

        void DisplayFC()
        {
            if (_c2type == C2Type.SetTarget)
            {
                ImGui.Text("Select Target for: " + _allFireControl[_selectedfirecon].Name);
                if (ImGui.SmallButton("Weapon Assignment Mode"))
                {
                    _fcIndex = _selectedfirecon;
                    _c2type = C2Type.SetWeapons;
                }
            }
            if (_c2type == C2Type.SetWeapons)
            {
                ImGui.Text("Select Weapns for: " + _allFireControl[_selectedfirecon].Name);
                if (ImGui.SmallButton("Targeting Mode"))
                {
                    _fcIndex = _selectedfirecon;
                    _c2type = C2Type.SetTarget;
                }
            }



            for (int i = 0; i < _allFireControl.Length; i++)
            {
                if (_selectedfirecon == i)
                {
                    BorderGroup.BeginBorder(_allFireControl[i].Name + " (Selected)");
                }
                else
                {
                    BorderGroup.BeginBorder(_allFireControl[i].Name);
                    if (ImGui.SmallButton("Select"))
                    {
                        _selectedfirecon = i;
                    }
                }

                ImGui.Text("Target: " + _fcTarget[i]);
                if (_fcState[i].IsEngaging)
                {
                    if (ImGui.Button("Cease Fire"))
                        OpenFire(_allFireControl[i].ID, SetOpenFireControlOrder.FireModes.CeaseFire);
                }
                else
                {
                    if (ImGui.Button("Open Fire"))
                        OpenFire(_allFireControl[i].ID, SetOpenFireControlOrder.FireModes.OpenFire);
                }

                _fcState[i].AssignedWeapons = new List<ComponentInstance>();

                foreach(WeaponState weapon in _allWeaponsstates) 
                {
                    if (weapon.FireControl == _allFireControl[i])
                    {
                        _fcState[i].AssignedWeapons.Add(weapon.WeaponComponentInstance);
                    }
                }


                
                for (int j = 0; j < _fcState[i].AssignedWeapons.Count; j++)
                {
                    var wpn = _fcState[i].AssignedWeapons[j];
                    if (ImGui.SmallButton(wpn.Name))
                    {
                        

                        _fcState[i].AssignedWeapons.RemoveAt(j);
                        Guid[] wnids = new Guid[_fcState[i].AssignedWeapons.Count];
                        for (int k = 0; k < _fcState[i].AssignedWeapons.Count; k++)
                        {
                            wnids[k] = _fcState[i].AssignedWeapons[k].ID;
                        }
                        SetWeapons(wnids);

                        foreach (WeaponState weapon in _allWeaponsstates)
                        {
                            if (weapon.WeaponComponentInstance == wpn)
                            {
                                weapon.FireControl = null;
                            }
                          
                        }
                    }

                }


                BorderGroup.EndBoarder();
                
            }
            
        }

        enum C2Type
        {
            Nill,
            SetTarget,
            SetWeapons,
            SetOrdnance,
        }
        private int _fcIndex;
        private int _wpnIndex;
        private C2Type _c2type = C2Type.SetTarget;
        private bool _showOwnAsTarget;
        void Display2ndColomn()
        {
            if (_c2type == C2Type.Nill)
                return;
            if (_c2type == C2Type.SetTarget)
            {
                BorderGroup.BeginBorder("Set Target:");
                ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

                for (int i = 0; i < _allSensorContacts.Length; i++)
                {
                    var contact = _allSensorContacts[i];
                    if (ImGui.SmallButton("Set ##sens" + i ))
                    {
                        SetTarget(contact.ActualEntityGuid);
                    }

                    ImGui.SameLine();
                    ImGui.Text(contact.Name);
                }

                if (_showOwnAsTarget)
                {
                    for (int i = 0; i < _ownEntites.Length; i++)
                    {
                        var contact = _ownEntites[i];
                        if(ImGui.SmallButton("Set##own" + i ))
                        {
                            SetTarget(contact.Entity.Guid);
                        }
                        ImGui.SameLine();
                        ImGui.Text(contact.Name);
                    }
                }
                BorderGroup.EndBoarder();
            }

            if (_c2type == C2Type.SetWeapons)
            {
                BorderGroup.BeginBorder("Missile Launchers:");
                for (int i = 0; i < _missileLaunchers.Length; i++)
                {
                    if( ImGui.SmallButton(_missileLaunchers[i].Name + "##" + i))
                    {

                        var wns = new List<ComponentInstance>( _fcState[i].AssignedWeapons);
                        Guid[] wnids = new Guid[wns.Count + 1];
                        for (int k = 0; k < wns.Count; k++)
                        {
                            wnids[k] = wns[k].ID;
                        }
                        wnids[wns.Count] = _missileLaunchers[i].ID;
                        SetWeapons(wnids);
                    }
                    ImGui.Indent();
                    for (int j = 0; j < _mlstates[i].WeaponStats.Length; j++)
                    {
                        var stat = _mlstates[i].WeaponStats[j];
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }

                    if (_mlstates[i].AssignedOrdnanceDesign != null)
                    {
                        string ordname = _mlstates[i].AssignedOrdnanceDesign.Name;
                        ImGui.Text("Assigned Ordnance: " + ordname);
                    }

                    if (ImGui.Button("Select Ordnance"))
                    {
                        _wpnIndex = i;
                        _c2type = C2Type.SetOrdnance;
                    }
                    ImGui.Unindent();
                    
                }
                BorderGroup.EndBoarder();
                ImGui.NewLine();
                BorderGroup.BeginBorder("Rail Guns:");
                for (int i = 0; i < _railGuns.Length; i++)
                {
                    
                    if( ImGui.SmallButton(_railGuns[i].Name + "##" + i))
                    {
                        var wns = new List<ComponentInstance>( _fcState[i].AssignedWeapons);
                        Guid[] wnids = new Guid[wns.Count + 1];
                        for (int k = 0; k < wns.Count; k++)
                        {
                            wnids[k] = wns[k].ID;
                        }
                        wnids[wns.Count] = _railGuns[i].ID;
                        SetWeapons(wnids);
                    }
                    ImGui.Indent();
                    for (int j = 0; j < _rgstates[i].WeaponStats.Length; j++)
                    {
                        var stat = _rgstates[i].WeaponStats[j];
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }
                    ImGui.Unindent();
                    
                }
                BorderGroup.EndBoarder();
                ImGui.NewLine();
                BorderGroup.BeginBorder("Beam Weapons:");
                for (int i = 0; i < _beamWpns.Length; i++)
                {
                    if( ImGui.SmallButton(_beamWpns[i].Name + "##" + i))
                    {
                        var wns = new List<ComponentInstance>( _fcState[i].AssignedWeapons);
                        Guid[] wnids = new Guid[wns.Count + 1];
                        for (int k = 0; k < wns.Count; k++)
                        {
                            wnids[k] = wns[k].ID;
                        }
                        wnids[wns.Count] = _beamWpns[i].ID;
                        SetWeapons(wnids);
                    }
                    ImGui.Indent();
                    for (int j = 0; j < _beamstates[i].WeaponStats.Length; j++)
                    {
                        var stat = _beamstates[i].WeaponStats[j];
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }
                    ImGui.Unindent();
                    
                }
                BorderGroup.EndBoarder();
            }

            if (_c2type == C2Type.SetOrdnance)
            {
                BorderGroup.BeginBorder("Ordnance Availible:");
                ImGui.Checkbox("Show Only Cargo", ref _showOnlyCargoOrdnance);
                for (int i = 0; i < _allOrdnanceDesigns.Length; i++)
                {
                    var ordDes = _allOrdnanceDesigns[i];
                    
                    if (_storedOrdnance.ContainsKey(ordDes.ID))
                    {
                        if (ImGui.SmallButton("Set"))
                        {
                            SetOrdnance(_missileLaunchers[_wpnIndex].ID, ordDes.ID);
                            _c2type = C2Type.SetWeapons;
                        }
                        ImGui.SameLine();
                        ImGui.Text(ordDes.Name);
                        ImGui.SameLine();
                        ImGui.Text("(" + _storedOrdnance[ordDes.ID] + ")");
                    }
                    else if (!_showOnlyCargoOrdnance)
                    {
                        if (ImGui.SmallButton("Set"))
                        {
                            SetOrdnance(_missileLaunchers[_wpnIndex].ID, ordDes.ID);
                            _c2type = C2Type.SetWeapons;
                        }
                        ImGui.SameLine();
                        ImGui.Text(ordDes.Name);
                    }
                }
            }
        }
    }
}