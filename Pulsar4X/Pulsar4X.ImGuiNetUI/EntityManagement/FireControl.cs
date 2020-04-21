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

        public class WeaponComponentInstance
        {
            public ComponentInstance WeaponInstance;
            public ComponentInstance FirecontrolInstance;
            public WeaponState CurrentWeaponState;
            public WeaponComponentInstance(ComponentInstance _WeaponInstance)
            {
                WeaponInstance = _WeaponInstance;
                CurrentWeaponState = _WeaponInstance.GetAbilityState<WeaponState>();
                FirecontrolInstance = null;
            }
        }



        List<WeaponComponentInstance> _missileLaunchers = new List<WeaponComponentInstance>();
        List<WeaponComponentInstance> _railGuns = new List<WeaponComponentInstance>();
        List<WeaponComponentInstance> _beamWpns = new List<WeaponComponentInstance>();
        
        
        List<WeaponComponentInstance> _allWeaponsinstances = new List<WeaponComponentInstance>();

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

        //The follwing varibles are to be used only for drag + drop logic.


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
                if (_orderEntity.GetDataBlob<CargoStorageDB>().StoredCargoTypes.ContainsKey(cargoType))
                {
                    var shipOrdnances = _orderEntity.GetDataBlob<CargoStorageDB>().StoredCargoTypes[cargoType].ItemsAndAmounts;

                    foreach (var ordType in shipOrdnances.Values)
                    {
                        _storedOrdnance[ordType.item.ID] = (int)ordType.amount;
                    }
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



                if (instancesDB.TryGetComponentsByAttribute<MissileLauncherAtb>(out var temp_missileLaunchers)) 
                {
                    for (int i = 0; i < temp_missileLaunchers.Count; i++)
                        _missileLaunchers.Add(new WeaponComponentInstance(temp_missileLaunchers[i]));
                    _allWeaponsinstances.AddRange(_missileLaunchers.ToList());
                }
                if (instancesDB.TryGetComponentsByAttribute<RailGunAtb>(out var temp_railGuns)) 
                {
                    foreach (ComponentInstance railgun in temp_railGuns)
                        _railGuns.Add(new WeaponComponentInstance(railgun));
                    _allWeaponsinstances.AddRange(_railGuns.ToList());
                }
                if (instancesDB.TryGetComponentsByAttribute<SimpleBeamWeaponAtbDB>(out var temp_beamWpns)) 
                {
                    foreach (ComponentInstance laser in temp_beamWpns)
                        _beamWpns.Add(new WeaponComponentInstance(laser));
                    _allWeaponsinstances.AddRange(_beamWpns.ToList());
                } 


                for (int i = 0; i < _allWeaponsinstances.Count; i++)
                {
                    _allWeaponsinstances[i].FirecontrolInstance = _allFireControl[0];
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

                if (_c2type == C2Type.SetTarget)
                    DisplayTargetColumn();
                if (_c2type == C2Type.SetWeapons)
                    DisplayWeaponColumn();
                if (_c2type == C2Type.SetOrdnance)
                    DisplayAmmoColumn();
                if (_c2type == C2Type.Nill)
                        return;
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

                foreach(WeaponComponentInstance weapon in _allWeaponsinstances) 
                {
                    if (weapon.FirecontrolInstance == _allFireControl[i])
                    {
                        _fcState[i].AssignedWeapons.Add(weapon.WeaponInstance);
                    }
                }


                
                for (int j = 0; j < _fcState[i].AssignedWeapons.Count; j++)
                {
                    var wpn = _fcState[i].AssignedWeapons[j];
                    
                    if (ImGui.SmallButton(wpn.Name))
                    {
                        
                    }

                    ImGui.SameLine();
                    
                    if (ImGui.SmallButton("X"))
                    {
                        _fcState[i].AssignedWeapons.RemoveAt(j);
                        Guid[] wnids = new Guid[_fcState[i].AssignedWeapons.Count];
                        for (int k = 0; k < _fcState[i].AssignedWeapons.Count; k++)
                        {
                            wnids[k] = _fcState[i].AssignedWeapons[k].ID;
                        }
                        SetWeapons(wnids);

                        foreach (WeaponComponentInstance weapon in _allWeaponsinstances)
                        {
                            if (weapon.WeaponInstance == wpn)
                            {
                                weapon.FirecontrolInstance = null;
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

        void DisplayTargetColumn()
        {
            BorderGroup.BeginBorder("Set Target:");
            ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

            for (int i = 0; i < _allSensorContacts.Length; i++)
            {
                var contact = _allSensorContacts[i];
                if (ImGui.SmallButton("Set ##sens" + i))
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
                    if (ImGui.SmallButton("Set##own" + i))
                    {
                        SetTarget(contact.Entity.Guid);
                    }
                    ImGui.SameLine();
                    ImGui.Text(contact.Name);
                }
            }
            BorderGroup.EndBoarder();
        }
        void DisplayWeaponColumn()
        {
            if (_missileLaunchers != null)
            {
                BorderGroup.BeginBorder("Missile Launchers:");
                for (int i = 0; i < _missileLaunchers.Count; i++)
                {
                    if (ImGui.SmallButton(_missileLaunchers[i].WeaponInstance.Name + "##" + i))
                    {

                        var wns = new List<ComponentInstance>(_fcState[i].AssignedWeapons);
                        Guid[] wnids = new Guid[wns.Count + 1];
                        for (int k = 0; k < wns.Count; k++)
                        {
                            wnids[k] = wns[k].ID;
                        }
                        wnids[wns.Count] = _missileLaunchers[i].WeaponInstance.ID;
                        SetWeapons(wnids);
                    }
                    ImGui.Indent();
                    for (int j = 0; j < _missileLaunchers[i].CurrentWeaponState.WeaponStats.Length; j++)
                    {
                        var stat = _missileLaunchers[i].CurrentWeaponState.WeaponStats[j];
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }

                    if (_missileLaunchers[i].CurrentWeaponState.AssignedOrdnanceDesign != null)
                    {
                        string ordname = _missileLaunchers[i].CurrentWeaponState.AssignedOrdnanceDesign.Name;
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
            }

            if (_railGuns != null)
            {
                BorderGroup.BeginBorder("Rail Guns:");
                for (int i = 0; i < _railGuns.Count; i++)
                {

                    if (ImGui.SmallButton(_railGuns[i].WeaponInstance.Name + "##" + i))
                    {
                        var wns = new List<ComponentInstance>(_fcState[i].AssignedWeapons);
                        Guid[] wnids = new Guid[wns.Count + 1];
                        for (int k = 0; k < wns.Count; k++)
                        {
                            wnids[k] = wns[k].ID;
                        }
                        wnids[wns.Count] = _railGuns[i].WeaponInstance.ID;
                        SetWeapons(wnids);
                    }
                    ImGui.Indent();
                    for (int j = 0; j < _railGuns[i].CurrentWeaponState.WeaponStats.Length; j++)
                    {
                        var stat = _railGuns[i].CurrentWeaponState.WeaponStats[j];
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }
                    ImGui.Unindent();

                }
                BorderGroup.EndBoarder();
                ImGui.NewLine();
            }

            if (_railGuns != null)
            {
                BorderGroup.BeginBorder("Beam Weapons:");
                for (int i = 0; i < _beamWpns.Count; i++)
                {
                    if (ImGui.SmallButton(_beamWpns[i].WeaponInstance.Name + "##" + i))
                    {
                        var wns = new List<ComponentInstance>(_fcState[i].AssignedWeapons);
                        Guid[] wnids = new Guid[wns.Count + 1];
                        for (int k = 0; k < wns.Count; k++)
                        {
                            wnids[k] = wns[k].ID;
                        }
                        wnids[wns.Count] = _beamWpns[i].WeaponInstance.ID;
                        SetWeapons(wnids);
                    }
                    ImGui.Indent();
                    for (int j = 0; j < _beamWpns[i].CurrentWeaponState.WeaponStats.Length; j++)
                    {
                        var stat = _beamWpns[i].CurrentWeaponState.WeaponStats[j];
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }
                    ImGui.Unindent();

                }
                BorderGroup.EndBoarder();
            }
        }

        void DisplayAmmoColumn()
        {
            BorderGroup.BeginBorder("Ordnance Availible:");
            ImGui.Checkbox("Show Only Cargo", ref _showOnlyCargoOrdnance);
            for (int i = 0; i < _allOrdnanceDesigns.Length; i++)
            {
                var ordDes = _allOrdnanceDesigns[i];

                if (_storedOrdnance.ContainsKey(ordDes.ID))
                {

                    //ImGui.SameLine();
                    //ImGui.Text(ordDes.Name);
                    BorderGroup.BeginBorder(ordDes.Name);

                    //ImGui.SameLine();
                    ImGui.Text("Qty in magaziene: " + _storedOrdnance[ordDes.ID]);
                    ImGui.Text("Mass: " + ordDes.WetMass);

                    double burnRate = ordDes.BurnRate;
                    double exaustVel = ordDes.ExaustVelocity;
                    double thrustNewtons = burnRate * exaustVel;
                    double burnTime = (ordDes.WetMass - ordDes.DryMass) / burnRate;
                    double dv = OrbitMath.TsiolkovskyRocketEquation(ordDes.WetMass, ordDes.DryMass, exaustVel);
                    ImGui.Text("Burn Time: " + burnTime + "s");
                    ImGui.Text("Thrust: " + Stringify.Thrust(thrustNewtons));
                    ImGui.Text("DeltaV: " + Stringify.Velocity(dv));

                    if (ImGui.SmallButton("Set"))
                    {
                        SetOrdnance(_missileLaunchers[_wpnIndex].WeaponInstance.ID, ordDes.ID);
                        _c2type = C2Type.SetWeapons;
                    }

                    BorderGroup.EndBoarder();
                }
                else if (!_showOnlyCargoOrdnance)
                {
                    if (ImGui.SmallButton("Set"))
                    {
                        SetOrdnance(_missileLaunchers[_wpnIndex].WeaponInstance.ID, ordDes.ID);
                        _c2type = C2Type.SetWeapons;
                    }
                    ImGui.SameLine();
                    ImGui.Text(ordDes.Name);
                }
            }
            BorderGroup.EndBoarder();
        }


    }
}