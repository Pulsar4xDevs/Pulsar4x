using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
using Pulsar4X.ECSLib.ComponentFeatureSets.RailGun;
using Pulsar4X.SDL2UI;

using System.Runtime.InteropServices;

namespace Pulsar4X.ImGuiNetUI
{
    public class FireControl : PulsarGuiWindow
    {
        private EntityState _orderEntityState;
        private Entity _orderEntity { get { return _orderEntityState.Entity; } }

        public class WeaponComponentInstance
        {
            public int LocalID;
            public ComponentInstance WeaponInstance;
            public WeaponState CurrentWeaponState;
            public bool HasFirecontrol { get { return FirecontrolInstance != null; } }
            public Guid ID { get { return WeaponInstance.ID; } }
            public ComponentInstance FirecontrolInstance
            {
                get{ return CurrentWeaponState.Master; }

                set{  CurrentWeaponState.Master = FirecontrolInstance; }
            }
            public WeaponComponentInstance(ComponentInstance _WeaponInstance, int ID = 0) 
            {
                WeaponInstance = _WeaponInstance;
                CurrentWeaponState = _WeaponInstance.GetAbilityState<WeaponState>();
                FirecontrolInstance = null;
                LocalID = ID;
            }
        }
        public class FirecontrolComponentInstance
        {
            public int LocalID;
            public Guid Owner;
            public string Name { get { return FirecontrolInstance.Name; } }
            public Guid ID { get { return FirecontrolInstance.ID; } }
            public string TargetName
            {
                get 
                {
                    if (Owner == null || TargetnameDB == null)
                        return "No target";
                    else
                        return TargetnameDB.GetName(Owner);     
                }
            }
            public NameDB TargetnameDB;
            public ComponentInstance FirecontrolInstance;
            public FireControlAbilityState FirecontrolState;
            
            public FirecontrolComponentInstance(ComponentInstance _FirecontrolInstance, Guid _owner , int ID = 0)
            {
                FirecontrolInstance = _FirecontrolInstance;
                FirecontrolState = _FirecontrolInstance.GetAbilityState<FireControlAbilityState>();
                LocalID = ID;

            }
        }

        bool dragdrop = false;

        List<int> testarray = new List<int>();


        WeaponComponentInstance NewWeapon(ComponentInstance _WeaponInstance)
        {
            return new WeaponComponentInstance(_WeaponInstance, weaponIDmax++);
        }
        int weaponIDmax = 0;


        Int32[] weaponarray = new Int32[0];


        void NewFirecontrol(ComponentInstance _FirecontrolInstance)
        {
            _allFirecontrols.Add( new FirecontrolComponentInstance(_FirecontrolInstance, _orderEntity.FactionOwner , firecontrolIDmax++));
        }
        int firecontrolIDmax = 0;

        List<WeaponComponentInstance> _missileLaunchers = new List<WeaponComponentInstance>();
        List<WeaponComponentInstance> _railGuns = new List<WeaponComponentInstance>();
        List<WeaponComponentInstance> _beamWpns = new List<WeaponComponentInstance>();
        
        List<WeaponComponentInstance> _allWeapons = new List<WeaponComponentInstance>();
        List<FirecontrolComponentInstance> _allFirecontrols = new List<FirecontrolComponentInstance>();

        OrdnanceDesign[] _allOrdnanceDesigns = new OrdnanceDesign[0];
        Dictionary<Guid, int> _storedOrdnance = new Dictionary<Guid, int>();
        private bool _showOnlyCargoOrdnance = true;

        SensorContact[] _allSensorContacts = new SensorContact[0];
        string[] _ownEntityNames = new string[0];
        EntityState[] _ownEntites = new EntityState[0];
        
       


        FirecontrolComponentInstance _selectedfirecontrol;

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

        
        void AssignWep(int WeaponID, int FirecontrolID)
        {
            if (FirecontrolID < 0)
                _allWeapons[WeaponID].CurrentWeaponState.Master = null;
            else
                _allWeapons[WeaponID].CurrentWeaponState.Master = _allFirecontrols[FirecontrolID].FirecontrolInstance;
            SetFirecontrolWeps();
        }
        void AssignWep(int WeaponID)
        {
            _allWeapons[WeaponID].CurrentWeaponState.Master = _allFirecontrols[0].FirecontrolInstance;
            SetFirecontrolWeps();
        }


        public void HardSetTargets()
        {
            foreach (FirecontrolComponentInstance firecontrol in _allFirecontrols)
            {

                if (firecontrol.FirecontrolState.Target != null)
                    firecontrol.TargetnameDB = firecontrol.FirecontrolState.Target.GetDataBlob<NameDB>();
                else
                    firecontrol.TargetnameDB = null;
            }
        }

        public void RefreshTargets()
        {
            var sysstate = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            var contacts = sysstate.SystemContacts;
            _allSensorContacts = contacts.GetAllContacts().ToArray();
            _ownEntites = sysstate.EntityStatesWithPosition.Values.ToArray();
        }

        public override void OnSystemTickChange(DateTime newdate)
        {
            RefreshTargets();
            HardSetTargets();

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
                        _storedOrdnance[ordType.item.ID] = (int)ordType.amount;
                }

            }
        }
        
        void HardRefresh(EntityState orderEntity)
        {
            RefreshTargets();
            _orderEntityState = orderEntity;
            if(orderEntity.DataBlobs.ContainsKey(typeof(FireControlAbilityDB)))
            {
                var instancesDB = orderEntity.Entity.GetDataBlob<ComponentInstancesDB>();
            
                if( instancesDB.TryGetComponentsByAttribute<BeamFireControlAtbDB>(out var fcinstances))
                {
                    firecontrolIDmax = 0;
                    _allFirecontrols = new List<FirecontrolComponentInstance>();
                    foreach (ComponentInstance Beamfirecontrol in fcinstances) 
                        NewFirecontrol(Beamfirecontrol);
                    HardSetTargets();
                }

                _selectedfirecontrol = _allFirecontrols[0];
                
                weaponIDmax = 0;
                _allWeapons = new List<WeaponComponentInstance>();

                if (instancesDB.TryGetComponentsByAttribute<MissileLauncherAtb>(out var temp_missileLaunchers)) 
                {
                    foreach (ComponentInstance missile in temp_missileLaunchers)
                        _missileLaunchers.Add(NewWeapon(missile));
                    _allWeapons.AddRange(_missileLaunchers);
                }
                if (instancesDB.TryGetComponentsByAttribute<RailGunAtb>(out var temp_railGuns)) 
                {
                    foreach (ComponentInstance railgun in temp_railGuns)
                        _railGuns.Add(NewWeapon(railgun));
                    _allWeapons.AddRange(_railGuns);
                }
                if (instancesDB.TryGetComponentsByAttribute<SimpleBeamWeaponAtbDB>(out var temp_beamWpns)) 
                {
                    foreach (ComponentInstance laser in temp_beamWpns)
                        _beamWpns.Add(NewWeapon(laser));
                    _allWeapons.AddRange(_beamWpns);
                }


                foreach (WeaponComponentInstance weapon in _allWeapons) 
                    AssignWep(weapon.LocalID);
            }
            
            var sysstate = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            var contacts = sysstate.SystemContacts;
            _allSensorContacts = contacts.GetAllContacts().ToArray();
            _ownEntites = sysstate.EntityStatesWithPosition.Values.ToArray();
            SetFirecontrolWeps();


        }

        void OnFrameRefresh() { }

        private void SetFirecontrolWeps()
        {
            foreach(FirecontrolComponentInstance firecontrol in _allFirecontrols)
            {
                List<Guid> AssignedWeps = new List<Guid>();
                foreach(WeaponComponentInstance weapon in _allWeapons)
                {
                    
                    if (weapon.FirecontrolInstance != null && weapon.ID == firecontrol.ID)
                        AssignedWeps.Add(weapon.ID);

                }
                SetWeapons(AssignedWeps.ToArray(), firecontrol.ID);
            }
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
            
            int selectedwep = 0;

            if (_c2type == C2Type.SetTarget)
            {
                ImGui.Text("Select Target for: " + _selectedfirecontrol.Name);
                if(ImGui.SmallButton("Weapon Assignment Mode"))
                {
                    _c2type = C2Type.SetWeapons;
                }
            }
            if (_c2type == C2Type.SetWeapons)
            {
                ImGui.Text("Select Weapns for: " + _selectedfirecontrol.Name);
                if(ImGui.SmallButton("Targeting Mode"))
                {
                    RefreshTargets();
                    _c2type = C2Type.SetTarget;
                }
            }

            
            foreach(FirecontrolComponentInstance firecontrol in _allFirecontrols)
            {


                if (_selectedfirecontrol.ID == firecontrol.ID)
                {
                    BorderGroup.BeginBorder(firecontrol.Name + " (Selected)");
                }
                else
                {
                    BorderGroup.BeginBorder(firecontrol.Name);
                    
                    if (ImGui.SmallButton("Select"))
                    {
                        _selectedfirecontrol = firecontrol;
                    }
                }

                ImGui.Text("Target: " + firecontrol.TargetName);
                if(dragdrop)
                {
                    ImGui.Button("Drop Here");
                    if (ImGui.BeginDragDropTarget())
                    {
                        AssignWep(selectedwep, firecontrol.LocalID);
                        dragdrop = false;
                        ImGui.EndDragDropTarget();
                    }
                }
                else
                {
                    if (firecontrol.FirecontrolState.IsEngaging)
                    {
                        if (ImGui.Button("Cease Fire"))
                            OpenFire(firecontrol.FirecontrolInstance.ID, SetOpenFireControlOrder.FireModes.CeaseFire);
                    }
                    else
                    {
                        if (ImGui.Button("Open Fire"))
                            OpenFire(firecontrol.FirecontrolInstance.ID, SetOpenFireControlOrder.FireModes.OpenFire);
                    }
                }

                FireControlAbilityState firecontrolstate = firecontrol.FirecontrolState;

                firecontrol.FirecontrolState.AssignedWeapons = new List<ComponentInstance>();

                foreach(WeaponComponentInstance weapon in _allWeapons) 
                {
                    if (weapon.FirecontrolInstance == null)
                        continue;

                    if (weapon.FirecontrolInstance.ID == firecontrol.ID)
                    {
                        
                        if (ImGui.SmallButton(weapon.WeaponInstance.Name))
                        {  
                        }
                        if (ImGui.BeginDragDropSource()) 
                        {
                            ImGui.Text(weapon.WeaponInstance.Name);
                            unsafe 
                            {
                                int number = weapon.LocalID;
                                selectedwep = weapon.LocalID;
                                int* tesnum = &number;
                                ImGui.SetDragDropPayload(weapon.WeaponInstance.Name, new IntPtr(tesnum), sizeof(int)) ;
                                dragdrop = true;
                            }
                            
                            ImGui.EndDragDropSource();
                        }

                        ImGui.SameLine();

                        if (ImGui.SmallButton("X"))
                            AssignWep(weapon.LocalID, -1);

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
        private Guid _wpnGuid;
        private C2Type _c2type = C2Type.SetTarget;
        private bool _showOwnAsTarget;

        void DisplayTargetColumn()
        {
            BorderGroup.BeginBorder("Set Target:");
            ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

            foreach (SensorContact contact in _allSensorContacts)
            {
                if (ImGui.SmallButton("Set ##sens" + contact.ActualEntityGuid))
                    SetRefreshedTarget(contact.ActualEntityGuid);
                ImGui.SameLine();
                ImGui.Text(contact.Name);
            }

            if (_showOwnAsTarget)
            {
                foreach (EntityState contact in _ownEntites)
                {
                    if (ImGui.SmallButton("Set##own" + contact.Entity.Guid))
                        SetRefreshedTarget(contact.Entity.Guid);
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
                foreach (WeaponComponentInstance missilelauncher in _missileLaunchers)
                {
                    ComponentInstance missilelauncherinstance = missilelauncher.WeaponInstance;
                    WeaponState missilelauncherstate = missilelauncher.CurrentWeaponState;
                    if (ImGui.SmallButton(missilelauncherinstance.Name + "##" + missilelauncherinstance.ID))
                        AssignWep(missilelauncher.LocalID, _selectedfirecontrol.LocalID);
                    ImGui.Indent();
                    foreach (var stat in missilelauncherstate.WeaponStats)
                    {
                        string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                        ImGui.Text(str);
                    }

                    if (missilelauncherstate.AssignedOrdnanceDesign != null)
                    {
                        string ordname = missilelauncherstate.AssignedOrdnanceDesign.Name;
                        ImGui.Text("Assigned Ordnance: " + ordname);
                    }

                    if (ImGui.Button("Select Ordnance"))
                    {
                        _wpnGuid = missilelauncherinstance.ID;
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
                foreach (WeaponComponentInstance railgun in _railGuns)
                {
                    ComponentInstance railguninstance = railgun.WeaponInstance;
                    WeaponState railgunstate = railgun.CurrentWeaponState;
                    

                    if (ImGui.SmallButton(railguninstance.Name + "##" + railguninstance.ID))
                        AssignWep(railgun.LocalID, _selectedfirecontrol.LocalID);
                    ImGui.Indent();
                    foreach (var stat in railgunstate.WeaponStats)
                        ImGui.Text(stat.name + Stringify.Value(stat.value, stat.valueType));
                    ImGui.Unindent();

                }
                BorderGroup.EndBoarder();
                ImGui.NewLine();
            }

            if (_beamWpns != null)
            {
                BorderGroup.BeginBorder("Beam Weapons:");
                foreach (WeaponComponentInstance laser in _beamWpns)
                {
                    ComponentInstance laserinstance = laser.WeaponInstance;
                    WeaponState laserstate = laser.CurrentWeaponState;

                    if (ImGui.SmallButton(laserinstance.Name + "##" + laserinstance.ID))
                        AssignWep(laser.LocalID, _selectedfirecontrol.LocalID);
                    ImGui.Indent();
                    foreach (var stat in laserstate.WeaponStats)
                    {
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
            foreach (OrdnanceDesign ordDes in _allOrdnanceDesigns)
            {
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
                        SetOrdnance(_wpnGuid, ordDes.ID);
                        _c2type = C2Type.SetWeapons;
                    }

                    BorderGroup.EndBoarder();
                }
                else if (!_showOnlyCargoOrdnance)
                {
                    if (ImGui.SmallButton("Set"))
                    {
                        SetOrdnance(_wpnGuid, ordDes.ID);
                        _c2type = C2Type.SetWeapons;
                    }
                    ImGui.SameLine();
                    ImGui.Text(ordDes.Name);
                }
            }
            BorderGroup.EndBoarder();
        }


        void SetWeapons(Guid[] wpnsAssignd) { SetWeapons(wpnsAssignd, _allFirecontrols[0].ID); }
        void SetWeapons(Guid[] wpnsAssignd, Guid FirecontrolID) { SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, FirecontrolID, wpnsAssignd); }
        void SetOrdnance(Guid wpnID, Guid ordnanceAssigned) { SetOrdinanceToWpnOrder.CreateCommand(_uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, wpnID, ordnanceAssigned); }
        void SetRefreshedTarget(Guid targetID) { RefreshTargets(); SetTarget(targetID); }
        void SetTarget(Guid targetID) { SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, _selectedfirecontrol.FirecontrolInstance.ID, targetID); }
        private void OpenFire(Guid fcID, SetOpenFireControlOrder.FireModes mode) { SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction, _orderEntity, fcID, mode);}

    }

}