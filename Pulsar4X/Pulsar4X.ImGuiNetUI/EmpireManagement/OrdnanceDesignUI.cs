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
        
        List<ComponentDesign> _engineTypes = new List<ComponentDesign>();
        private string[] _engineDesigns;
        private int _engineSelectedIndex = 0;
        private int _engineCount = 1;

        private Dictionary<ComponentDesign, int>  _selectedComponentDesigns = new Dictionary<ComponentDesign, int>();
        private double _totalMass;
        private OrdinanceDesignUI()
        {
            HardRefresh();
        }

        public static OrdinanceDesignUI GetInstance()
        {
            OrdinanceDesignUI thisitem;
            if (!_state.LoadedWindows.ContainsKey(typeof(OrdinanceDesignUI)))
            {
                thisitem = new OrdinanceDesignUI();
            }
            else
                thisitem = (OrdinanceDesignUI)_state.LoadedWindows[typeof(OrdinanceDesignUI)];
            
            return thisitem;
            
        }

        public void HardRefresh()
        {
            var designs = _state.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns;
            var componentDesigns = _state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns;

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
            _engineTypes = new List<ComponentDesign>();
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

                    if (cdes.AttributesByType.ContainsKey(typeof(NewtonionThrustAtb)))
                    {
                        _engineTypes.Add(cdes);
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
            _engineDesigns = new string[_engineTypes.Count];
            i = 0;
            foreach (var des in _engineTypes)
            {
                _engineDesigns[i] = des.Name;
            }

            

        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ordnance Design"))
            {
                ImGui.Combo("Current Designs", ref _payloadSelectedIndex, _currentDesignNames, _currentDesignNames.Length);

                ImGui.NewLine();
                BorderGroup.BeginBorder("Payload:");
                if (ImGui.Combo("Payload type", ref _payloadSelectedIndex, _payload, _payload.Length))
                {
                    _selectedPayload = _payloadTypes[_payloadSelectedIndex].GetAttribute<OrdnancePayloadAtb>();
                    _selectedComponentDesigns[_payloadTypes[_payloadSelectedIndex]] = _payloadCount;
                    RefreshMass();
                }
                if(ImGui.SliderInt("Payload Count", ref _payloadCount, 1, 100))
                {
                    _selectedComponentDesigns[_payloadTypes[_payloadSelectedIndex]] = _payloadCount;
                    RefreshMass();
                }
                //ImGui.Text("Payload Trigger Type: " + _selectedPayload.Trigger);
                BorderGroup.EndBoarder();
                ImGui.NewLine();
                
                BorderGroup.BeginBorder("Electronics Suite:");
                if(ImGui.Combo("ElectronicsSuite", ref _electronicsSelectedIndex, _electronicsPackage, _electronicsPackage.Length))
                {
                    _selectedComponentDesigns[_eleccPackTypes[_electronicsSelectedIndex]] = 1;
                    RefreshMass();
                }
                ImGui.Text("Size: ");
                BorderGroup.EndBoarder();
                
                ImGui.NewLine();
                
                BorderGroup.BeginBorder("Engine:");
                
                if(ImGui.Combo("Engine Designs", ref _engineSelectedIndex, _engineDesigns, _engineDesigns.Length))
                {
                    _selectedComponentDesigns[_engineTypes[_engineSelectedIndex]] = _engineCount;
                    RefreshMass();
                }
                if(ImGui.SliderInt("Engine Count", ref _engineCount, 1, 256))
                {
                    _selectedComponentDesigns[_engineTypes[_engineSelectedIndex]] = _engineCount;
                    RefreshMass();
                }
                
                if(ImGui.SliderFloat("Fuel Amount in Kg", ref _fuelKG, 0, 100))
                {
                    RefreshMass();
                }
                BorderGroup.EndBoarder();
                
                ImGui.NewLine();
                ImGui.Text("Total Mass: " + _totalMass);
                var enginedesign = _engineTypes[_engineSelectedIndex];
                var atb = enginedesign.GetAttribute<NewtonionThrustAtb>();
            
                double burnRate = atb.FuelBurnRate * _engineCount;
                double exaustVel = atb.ExhaustVelocity;
                double thrustNewtons = burnRate * exaustVel;
                double burnTime = _fuelKG / burnRate;
                double dv = OrbitMath.TsiolkovskyRocketEquation(_totalMass, _totalMass - _fuelKG, exaustVel);
                ImGui.Text("Burn Time: " + burnTime + "s");
                ImGui.Text("Thrust: " + Stringify.Thrust(thrustNewtons));
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
                _totalMass += kvp.Key.Mass * kvp.Value;
            }

            _totalMass += _fuelKG;


        }

        internal void NewDesignButton()
        {
            if (ImGui.Button("Create Design"))
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
                OrdnanceDesign missileDesign = new OrdnanceDesign(_state.Faction.GetDataBlob<FactionInfoDB>(), strName, misslcomponents);
                //missileDesign.DesignVersion = version;

            }
        }
    }
}