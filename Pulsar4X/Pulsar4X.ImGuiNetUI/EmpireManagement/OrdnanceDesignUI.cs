using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;

namespace Pulsar4X.SDL2UI
{
    public class OrdinanceDesignUI : PulsarGuiWindow
    {
        private Entity _faction;
        private FactionTechDB _factionTech;
        private byte[] _designName =  ImGuiSDL2CSHelper.BytesFromString("foo", 32);
        
        private OrdnanceDesign[] _currentDesigns;
        private string[] _currentDesignNames;
        private int _currentDesignsSelectedIndex = 0;

        private float _missileSize = 1;
        
        private List<ComponentDesign> _payloadTypes = new List<ComponentDesign>();
        private OrdnancePayloadAtb _selectedPayload;
        private string[] _payload;
        private int _payloadSelectedIndex = 0;
        private int _payloadCount = 1;
        
        List<ComponentDesign> _eleccPackTypes = new List<ComponentDesign>();
        private string[] _electronicsPackage;
        private int _electronicsSelectedIndex = 0;
        
        private float _fuelKG;
        private long _thrusterSizeKG;
        ComponentTemplateSD[] _engineTemplates = new ComponentTemplateSD[0];
        private string[] _engineTypeNames = new string[0];
        private ComponentDesigner _engineDesigner;
        private int _engineDesignTypeIndex = -1;
        
        //private int _fuelAmount = 1;

        private Dictionary<ComponentDesign, int>  _selectedComponentDesigns = new Dictionary<ComponentDesign, int>();
        private double _totalMass;
        private OrdinanceDesignUI()
        {
            HardRefresh();
        }

        public static OrdinanceDesignUI GetInstance()
        {
            OrdinanceDesignUI thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(OrdinanceDesignUI)))
            {
                thisitem = new OrdinanceDesignUI();
            }
            else
                thisitem = (OrdinanceDesignUI)_uiState.LoadedWindows[typeof(OrdinanceDesignUI)];
            
            return thisitem;
            
        }

        public void HardRefresh()
        {
            var designs = _uiState.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns;
            var componentDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns;
            _faction = _uiState.Faction;
            _factionTech = _uiState.Faction.GetDataBlob<FactionTechDB>();

            foreach (var des in componentDesigns)
            {

            }
            
            _currentDesigns = designs.Values.ToArray();
            _currentDesignNames = new string[_currentDesigns.Length];
            int i = 0;
            foreach (var mdesign in _currentDesigns)
            {
                _currentDesignNames[i] = mdesign.Name;
                i++;
            }
            


            _payloadTypes = new List<ComponentDesign>();
            _eleccPackTypes = new List<ComponentDesign>();
            foreach (ComponentDesign cdes in componentDesigns.Values)
            {
                if ((cdes.ComponentMountType & ComponentMountType.Missile) == ComponentMountType.Missile)
                {
                    if (cdes.AttributesByType.ContainsKey(typeof(OrdnancePayloadAtb)))
                    {
                        _payloadTypes.Add(cdes);
                    }
                    if (cdes.AttributesByType.ContainsKey(typeof(OrdnanceExplosivePayload)))
                    {
                        _payloadTypes.Add(cdes);
                    }
                    if (cdes.AttributesByType.ContainsKey(typeof(OrdnanceShapedPayload)))
                    {
                        _payloadTypes.Add(cdes);
                    }
                    if (cdes.AttributesByType.ContainsKey(typeof(OrdnanceLaserPayload)))
                    {
                        _payloadTypes.Add(cdes);
                    }
                    if (cdes.AttributesByType.ContainsKey(typeof(OrdnanceSubmunitionsPayload)))
                    {
                        _payloadTypes.Add(cdes);
                    }
                    if (cdes.AttributesByType.ContainsKey(typeof(SensorReceverAtbDB)))
                    {
                        _eleccPackTypes.Add(cdes);
                    }

                }
            }
            
            _payload = new string[_payloadTypes.Count];
            i = 0;
            foreach (var des in _payloadTypes)
            {
                _payload[i] = des.Name;
            }
            _electronicsPackage = new string[_eleccPackTypes.Count];
            i = 0;
            foreach (var des in _eleccPackTypes)
            {
                _electronicsPackage[i] = des.Name;
            }
            
            _selectedComponentDesigns[_payloadTypes[_payloadSelectedIndex]] = _payloadCount;
            _selectedComponentDesigns[_eleccPackTypes[_electronicsSelectedIndex]] = 1;
            
            
            var allDesignables = StaticRefLib.StaticData.ComponentTemplates.Values.ToArray();
            List<ComponentTemplateSD> engineTemplates = new List<ComponentTemplateSD>();
            foreach (var designable in allDesignables)
            {
                foreach (var atbSD in designable.ComponentAtbSDs)
                {
                    if( atbSD.AttributeType == typeof(NewtonionThrustAtb).ToString())
                    {
                        engineTemplates.Add(designable);
                    }
                }
            }

            _engineTemplates = engineTemplates.ToArray();
            _engineTypeNames = new string[_engineTemplates.Length];
            for (int j = 0; j < _engineTemplates.Length; j++)
            {
                _engineTypeNames[j] = engineTemplates[j].Name;
            }
            
            
            RefreshMass();
        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ordnance Design"))
            {
                ImGui.Combo("Current Designs", ref _payloadSelectedIndex, _currentDesignNames, _currentDesignNames.Length);

                ImGui.NewLine();
                BorderGroup.Begin("Payload:");
                if (ImGui.Combo("Payload type", ref _payloadSelectedIndex, _payload, _payload.Length))
                {
                    //_selectedPayload = _payloadTypes[_payloadSelectedIndex].GetAttribute<OrdnancePayloadAtb>();
                    _selectedComponentDesigns[_payloadTypes[_payloadSelectedIndex]] = _payloadCount;
                    RefreshMass();
                }
                if(ImGui.SliderInt("Payload Count", ref _payloadCount, 1, 100))
                {
                    _selectedComponentDesigns[_payloadTypes[_payloadSelectedIndex]] = _payloadCount;
                    RefreshMass();
                }
                var whmass = _payloadTypes[_payloadSelectedIndex].MassPerUnit * _payloadCount;
                ImGui.Text("Mass: " + Stringify.Mass(whmass));
                //ImGui.Text("Payload Trigger Type: " + _selectedPayload.Trigger);
                BorderGroup.End();
                ImGui.NewLine();
                
                BorderGroup.Begin("Electronics Suite:");
                if(ImGui.Combo("ElectronicsSuite", ref _electronicsSelectedIndex, _electronicsPackage, _electronicsPackage.Length))
                {
                    _selectedComponentDesigns[_eleccPackTypes[_electronicsSelectedIndex]] = 1;
                    RefreshMass();
                }
                var mass = _eleccPackTypes[_electronicsSelectedIndex].MassPerUnit;
                ImGui.Text("Mass: " + Stringify.Mass(mass));
                BorderGroup.End();
                
                ImGui.NewLine();
                
                BorderGroup.Begin("Engine:");

                if (ImGui.Combo("Engine Types", ref _engineDesignTypeIndex, _engineTypeNames, _engineTypeNames.Length))
                {
                    _engineDesigner = new ComponentDesigner(_engineTemplates[_engineDesignTypeIndex], _factionTech);
                    _engineDesigner.SetAttributes();
                }

                if(_engineDesigner != null)
                {
                    PartDesignUI.GuiDesignUI(_engineDesigner);
                    _thrusterSizeKG = _engineDesigner.MassValue;
                    RefreshMass();
                }
                
                if (ImGui.SliderFloat("Fuel", ref _fuelKG, 1, 1000))
                {
                    RefreshMass();
                }
                
                BorderGroup.End();
                
                ImGui.NewLine();
                ImGui.Text("Total Mass: " + Stringify.Mass(_totalMass));
                
                double burnRate = 0;
                double exaustVel = 0;

                if (_engineDesigner != null)
                {
                    var atb = _engineDesigner.GetAttribute<NewtonionThrustAtb>();
                    burnRate = atb.FuelBurnRate;
                    exaustVel = atb.ExhaustVelocity;
                }

                double thrustNewtons = burnRate * exaustVel;
                double burnTime = _fuelKG / burnRate;
                double dv = OrbitMath.TsiolkovskyRocketEquation(_totalMass, _totalMass - _fuelKG, exaustVel);
                ImGui.Text("Burn Time: " + burnTime + "s");
                ImGui.Text("Thrust: " + Stringify.Thrust(thrustNewtons));
                ImGui.Text("Acceleration (wet): " + Stringify.Velocity(thrustNewtons / _totalMass));
                ImGui.Text("Acceleration (dry): " + Stringify.Velocity(thrustNewtons / (_totalMass - _fuelKG)));
                ImGui.Text("DeltaV: " + Stringify.Velocity(dv));
                
                ImGui.InputText("Design Name", _designName, (uint)_designName.Length);
                NewDesignButton();
                
            }
        }

        void RefreshMass()
        {
            _totalMass = 0;
            foreach (var kvp in _selectedComponentDesigns)
            {
                _totalMass += kvp.Key.MassPerUnit * kvp.Value;
                
            }

            _totalMass += _fuelKG;
            _totalMass += _thrusterSizeKG;
        }

        internal void NewDesignButton()
        {
            if (ImGui.Button("Create Design") && _engineDesigner != null)
            {
                int version = 0;
                var strName = ImGuiSDL2CSHelper.StringFromBytes(_designName);
                foreach (var design in _currentDesigns)
                {
                    if (design.Name == strName)
                    {
                        if (design.DesignVersion >= version)
                            version = design.DesignVersion + 1;
                    }
                }
                

                List<(ComponentDesign, int)> misslcomponents = new List<(ComponentDesign, int)>();
                foreach (var kvp in _selectedComponentDesigns)
                {
                    misslcomponents.Add((kvp.Key, kvp.Value));
                }
                
                
                var atb = _engineDesigner.GetAttribute<NewtonionThrustAtb>();
                
                double burnRate = atb.FuelBurnRate;
                double exaustVel = atb.ExhaustVelocity;
                double thrustNewtons = burnRate * exaustVel;
                _engineDesigner.Name = "MissileEngine" + _engineDesigner.MassValue +","+ thrustNewtons;
                var engineDesign = _engineDesigner.CreateDesign(_faction);
                misslcomponents.Add((engineDesign, 1));
                OrdnanceDesign missileDesign = new OrdnanceDesign(_uiState.Faction.GetDataBlob<FactionInfoDB>(), strName, _fuelKG, misslcomponents);
                //missileDesign.DesignVersion = version;

            }
        }
    }
}