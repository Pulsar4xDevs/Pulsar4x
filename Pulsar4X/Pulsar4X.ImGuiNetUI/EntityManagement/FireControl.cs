using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.ImGuiNetUI
{
    public class FireControl : PulsarGuiWindow
    {
        
        string[] _allWeaponNames = new string[0];
        ComponentDesign[] _allWeaponDesigns = new ComponentDesign[0];
        ComponentInstance[] _allWeaponInstances = new ComponentInstance[0];
        
        SensorContact[] _allSensorContacts = new SensorContact[0];
        
        ComponentInstance[] _allFireControl = new ComponentInstance[0];
        FireControlAbilityState[] _fcState = new FireControlAbilityState[0];
        int[][] _assignedWeapons = new int[0][];

        string[] _fcTarget = new string[0];
        
        private FireControl()
        {
            
        }



        public static FireControl GetInstance()
        {
            FireControl thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FireControl)))
            {
                thisitem = new FireControl();
            }
            else
                thisitem = (FireControl)_uiState.LoadedWindows[typeof(FireControl)];
            
            return thisitem;
        }

        public override void OnSystemTickChange(DateTime newdate)
        {
            
        }
        
        void HardRefresh(EntityState orderEntity)
        {
            if(orderEntity.DataBlobs.ContainsKey(typeof(FireControlAbilityDB)))
            {
                var instancesDB = orderEntity.Entity.GetDataBlob<ComponentInstancesDB>();
            
                if( instancesDB.TryGetComponentsByAttribute<BeamFireControlAtbDB>(out var fcinstances))
                {
                    _allFireControl = new ComponentInstance[fcinstances.Count];
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
            }
        
        }

        void RefreshOnUpdate()
        {
        }

        internal override void Display()
        {
            if (!IsActive)
                return;
            if (ImGui.Begin("Fire Control"))
            {
                DisplayFC();
            }
        }

        void DisplayFC()
        {
            for (int i = 0; i < _allFireControl.Length; i++)
            {
                BorderGroup.BeginBorder(_allFireControl[i].Name);
                
                ImGui.Text("Target: " + _fcTarget[i]);
                for (int j = 0; j < _fcState[i].AssignedWeapons.Count; j++)
                {
                    var wpn = _fcState[i].AssignedWeapons[j];
                    ImGui.Text(wpn.Name);
                }
                BorderGroup.EndBoarder();
                
            }
            
        }
    }
}