using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        private Entity _orderEntity => _orderEntityState.Entity;
        SensorContact[] _allSensorContacts = new SensorContact[0];
        string[] _ownEntityNames = new string[0];
        EntityState[] _ownEntites = new EntityState[0];
        private bool _showOwnAsTarget;

        private Guid _factionID;

        //private int _dragDropIndex;
        private Guid _dragDropGuid;


        private int _selectedFCIndex = -1;
        private FireControlAbilityState[] _fcStates = new FireControlAbilityState[0];
        private Vector2[] _fcSizes = new Vector2[0];

        private Dictionary<Guid, WeaponState> _wpnDict = new Dictionary<Guid, WeaponState>();
        private WeaponState[] _allWeaponsStates = new WeaponState[0];

        private Dictionary<Guid, string> _weaponNames = new Dictionary<Guid, string>();

        //private WeaponState[] _unAssignedWeapons = new WeaponState[0];
        private OrdnanceDesign[] _allOrdnanceDesigns = new OrdnanceDesign[0];
        Dictionary<Guid, int> _storedOrdnance = new Dictionary<Guid, int>();
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
                            OpenFire(_fcStates[i].ComponentInstance.ID, SetOpenFireControlOrder.FireModes.CeaseFire);
                    }
                    else
                    {
                        if (ImGui.Button("Open Fire"))
                            OpenFire(_fcStates[i].ComponentInstance.ID, SetOpenFireControlOrder.FireModes.OpenFire);
                    }
                }

                foreach (var wpn in fc.ChildrenStates)
                {
                    ImGui.Selectable(_weaponNames[wpn.ID]);
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
                        ImGui.Selectable(_weaponNames[wpn.ID] + "##" + wpn.ComponentInstance.ID);
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
                                _dragDropGuid = ord.ID;
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


            if (instancesDB.TryGetStates<WeaponState>(out _allWeaponsStates))
            {
                foreach (var wpn in _allWeaponsStates)
                {
                    _wpnDict[wpn.ID] = wpn;
                }
            }
            /*
            List<WeaponState> unassigned= new List<WeaponState>();
                foreach (var wpn in _allWeaponsStates)
                {
                    _wpnDict[wpn.ID] = wpn;
                    if(wpn.ParentState == null)
                        unassigned.Add(wpn);
                }

                _unAssignedWeapons = unassigned.ToArray();
            }
            */

            var sysstate = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            var contacts = sysstate.SystemContacts;
            _allSensorContacts = contacts.GetAllContacts().ToArray();
            _ownEntites = sysstate.EntityStatesWithPosition.Values.ToArray();



        }

        public override void OnSystemTickChange(DateTime newdate)
        {


            _allOrdnanceDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns.Values.ToArray();
            var ctypes = new List<Guid>(); //there are likely to be not very many of these, proibly only one.
            foreach (var ordDes in _allOrdnanceDesigns)
            {
                if (!ctypes.Contains(ordDes.CargoTypeID))
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

            UpdateWpnNamesCashe();
        }
        
        void UpdateWpnNamesCashe()
        {

            for (int i = 0; i < _allWeaponsStates.Length; i++)
            {
                var wpn = _allWeaponsStates[i];

                var assOrdName = "";
                string assOrdCount = "(0)";
                if (wpn.AssignedOrdnanceDesign != null)
                {
                    assOrdName = wpn.AssignedOrdnanceDesign.Name;
                    assOrdCount = "(" + _storedOrdnance[wpn.AssignedOrdnanceDesign.ID] + ")";
                }

                _weaponNames[wpn.ID] = wpn.Name + "\t" + assOrdName + assOrdCount;
            }
        }
        
        void UnSetWeapon(WeaponState wpnState)
        {
            var fcState = wpnState.ParentState;
            var curWpnIDs = fcState.GetChildrenIDs();
            var newArry = new Guid[curWpnIDs.Length - 1];
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

        void SetWeapon(Guid wpnID, FireControlAbilityState fcState)
        {
            //var curWpns = fcState.AssignedWeapons;
            var curWpnIDs = fcState.GetChildrenIDs();
            var newArry = new Guid[curWpnIDs.Length + 1];
            for (int i = 0; i < curWpnIDs.Length; i++)
            {
                newArry[i] = curWpnIDs[i];
            }

            newArry[curWpnIDs.Length] = wpnID;
            SetWeapons(newArry, fcState.ID);
        }
        
        void SetWeapons(Guid[] wpnsAssignd, Guid firecontrolID)
        {
            SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, firecontrolID, wpnsAssignd);
        }

        void SetOrdnance(WeaponState wpn, Guid ordnanceAssigned)
        {
            SetOrdinanceToWpnOrder.CreateCommand(_uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, wpn.ID, ordnanceAssigned);
            UpdateWpnNamesCashe(); //refresh this or it wont show the change till after a systemtick. 
        }

        void SetTarget(FireControlAbilityState fcState, Guid targetID)
        {
            var fcGuid = fcState.ComponentInstance.ID;
            SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, fcGuid, targetID);
        }

        private void OpenFire(Guid fcID, SetOpenFireControlOrder.FireModes mode)
        {
            SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction, _orderEntity, fcID, mode);
        }
    }
}



/*



public class FireControl2 : PulsarGuiWindow
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
            get
            {
                if (CurrentWeaponState.ParentState == null)
                    return null;
                return CurrentWeaponState.ParentState.ComponentInstance;
            }
        }
        public WeaponComponentInstance(ComponentInstance _WeaponInstance, int ID = 0) 
        {
            WeaponInstance = _WeaponInstance;
            CurrentWeaponState = _WeaponInstance.GetAbilityState<WeaponState>();
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
        
        public FirecontrolComponentInstance(ComponentInstance _FirecontrolInstance, int ID = 0)
        {
            FirecontrolInstance = _FirecontrolInstance;
            FirecontrolState = _FirecontrolInstance.GetAbilityState<FireControlAbilityState>();
            LocalID = ID;
            Owner = FirecontrolInstance.ParentEntity.FactionOwner;

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
        _allFirecontrols.Add( new FirecontrolComponentInstance(_FirecontrolInstance, firecontrolIDmax++));
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


    private FireControl2()
    {
        _flags = ImGuiWindowFlags.None;
    }



    public static FireControl2 GetInstance(EntityState orderEntity)
    {
        FireControl2 thisitem;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(FireControl2)))
        {
            thisitem = new FireControl2();
            thisitem.HardRefresh(orderEntity);
            thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
        }
        else
        {
            thisitem = (FireControl2)_uiState.LoadedWindows[typeof(FireControl2)];
            if(thisitem._orderEntityState != orderEntity)
            {
                thisitem.HardRefresh(orderEntity);
                thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
            }
        }
        


        return thisitem;
    }

    



    public void UpdateTargets()
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
        SoftRefresh();

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
    void SoftRefresh()
    {
        RefreshTargets();
        UpdateTargets();
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
                SoftRefresh();
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
            
        }
        
        var sysstate = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
        var contacts = sysstate.SystemContacts;
        _allSensorContacts = contacts.GetAllContacts().ToArray();
        _ownEntites = sysstate.EntityStatesWithPosition.Values.ToArray();
        


    }

    void OnFrameRefresh() { }

    
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
                SoftRefresh();
            }
        }
        if (_c2type == C2Type.SetWeapons)
        {
            ImGui.Text("Select Weapns for: " + _selectedfirecontrol.Name);
            if(ImGui.SmallButton("Targeting Mode"))
            {
                _c2type = C2Type.SetTarget;
            }
        }

        
        foreach(FirecontrolComponentInstance firecontrol in _allFirecontrols)
        {


            if (_selectedfirecontrol.ID == firecontrol.ID)
            {
                BorderGroup.Begin(firecontrol.Name + " (Selected)");
            }
            else
            {
                BorderGroup.Begin(firecontrol.Name);
                
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
                    SetWeapon(_allWeapons[selectedwep].ID, firecontrol.FirecontrolState);
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
                    {
                        UnSetWeapon(weapon.ID, firecontrol.FirecontrolState);
                    }
                }
            }

            BorderGroup.End();
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
        BorderGroup.Begin("Set Target:");
        ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

        foreach (SensorContact contact in _allSensorContacts)
        {
            if (ImGui.SmallButton("Set ##sens" + contact.ActualEntityGuid))
                SetTarget(contact.ActualEntityGuid);
            
            ImGui.SameLine();
            ImGui.Text(contact.Name);
        }

        if (_showOwnAsTarget)
        {
            foreach (EntityState contact in _ownEntites)
            {
                if (ImGui.SmallButton("Set##own" + contact.Entity.Guid))
                    SetTarget(contact.Entity.Guid);
                
                ImGui.SameLine();
                ImGui.Text(contact.Name);
            }
        }
        BorderGroup.End();
    }
    void DisplayWeaponColumn()
    {
        if (_missileLaunchers != null)
        {
            BorderGroup.Begin("Missile Launchers:");
            foreach (WeaponComponentInstance missilelauncher in _missileLaunchers)
            {
                ComponentInstance missilelauncherinstance = missilelauncher.WeaponInstance;
                WeaponState missilelauncherstate = missilelauncher.CurrentWeaponState;
                if (ImGui.SmallButton(missilelauncherinstance.Name + "##" + missilelauncherinstance.ID))
                    SetWeapon(missilelauncher.ID, _selectedfirecontrol.FirecontrolState);
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
            BorderGroup.End();
            ImGui.NewLine();
        }

        if (_railGuns != null)
        {
            BorderGroup.Begin("Rail Guns:");
            foreach (WeaponComponentInstance railgun in _railGuns)
            {
                ComponentInstance railguninstance = railgun.WeaponInstance;
                WeaponState railgunstate = railgun.CurrentWeaponState;
                

                if (ImGui.SmallButton(railguninstance.Name + "##" + railguninstance.ID))
                    SetWeapon(railgun.ID, _selectedfirecontrol.FirecontrolState);
                ImGui.Indent();
                foreach (var stat in railgunstate.WeaponStats)
                    ImGui.Text(stat.name + Stringify.Value(stat.value, stat.valueType));
                ImGui.Unindent();

            }
            BorderGroup.End();
            ImGui.NewLine();
        }

        if (_beamWpns != null)
        {
            BorderGroup.Begin("Beam Weapons:");
            foreach (WeaponComponentInstance laser in _beamWpns)
            {
                ComponentInstance laserinstance = laser.WeaponInstance;
                WeaponState laserstate = laser.CurrentWeaponState;

                if (ImGui.SmallButton(laserinstance.Name + "##" + laserinstance.ID))
                    SetWeapon(laser.ID, _selectedfirecontrol.FirecontrolState);
                ImGui.Indent();
                foreach (var stat in laserstate.WeaponStats)
                {
                    string str = stat.name + Stringify.Value(stat.value, stat.valueType);
                    ImGui.Text(str);
                }
                ImGui.Unindent();

            }
            BorderGroup.End();
        }
    }

    void DisplayAmmoColumn()
    {
        BorderGroup.Begin("Ordnance Availible:");
        ImGui.Checkbox("Show Only Cargo", ref _showOnlyCargoOrdnance);
        foreach (OrdnanceDesign ordDes in _allOrdnanceDesigns)
        {
            if (_storedOrdnance.ContainsKey(ordDes.ID))
            {

                //ImGui.SameLine();
                //ImGui.Text(ordDes.Name);
                BorderGroup.Begin(ordDes.Name);

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

                BorderGroup.End();
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
        BorderGroup.End();
    }


    void UnSetWeapon(Guid wpnID, FireControlAbilityState fcState)
    {
        var curWpnIDs = fcState.GetChildrenIDs();
        
        var newArry = new Guid[curWpnIDs.Length];
        int j = 0;
        for (int i = 0; i < curWpnIDs.Length; i++)
        {
            if (curWpnIDs[i] != wpnID)
            {
                newArry[j] = curWpnIDs[i];
                j++;
            }
        }
        SetWeapons(newArry, fcState.ComponentInstance.ID);
    }

    void SetWeapon(Guid wpnID, FireControlAbilityState fcState)
    {
        var curWpnIDs = fcState.GetChildrenIDs();
        var newArry = new Guid[curWpnIDs.Length + 1];
        for (int i = 0; i < curWpnIDs.Length; i++)
        { 
            newArry[i] = curWpnIDs[i];
        }
        newArry[curWpnIDs.Length] = wpnID;
        SetWeapons(newArry, fcState.ComponentInstance.ID);
    }
    
    
    void SetWeapons(Guid[] wpnsAssignd, Guid firecontrolID)
    {
        SetWeaponsFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, firecontrolID, wpnsAssignd);
    }

    void SetOrdnance(Guid wpnID, Guid ordnanceAssigned)
    {
        SetOrdinanceToWpnOrder.CreateCommand(_uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, wpnID, ordnanceAssigned);
    }

    void SetTarget(Guid targetID)
    {
        SetTargetFireControlOrder.CreateCommand(_uiState.Game, _uiState.PrimarySystemDateTime, _uiState.Faction.Guid, _orderEntity.Guid, _selectedfirecontrol.FirecontrolInstance.ID, targetID);
        UpdateTargets();
    }

    private void OpenFire(Guid fcID, SetOpenFireControlOrder.FireModes mode)
    {
        SetOpenFireControlOrder.CreateCmd(_uiState.Game, _uiState.Faction, _orderEntity, fcID, mode);
    }

}
*/
