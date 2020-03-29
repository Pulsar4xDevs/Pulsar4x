using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class SensorDraw : PulsarGuiWindow
    {
        private EntityState _selectedEntitySate;
        private Entity _selectedEntity => _selectedEntitySate.Entity;
        private Entity[] _potentialTargetEntities;
        private string[] _potentialTargetNames;
        private int _targetIndex = -1;
        private Entity _targetEntity;
        private SensorProfileDB _targetSensorProfile;
        
        private SystemState _selectedStarSysState;
        private StarSystem _selectedStarSys => _selectedStarSysState.StarSystem;

        private SensorReceverAtbDB[] _selectedReceverAtb;
        private Vector2[] _recever;
        private Vector2[] _reflected;
        private Vector2[] _emmitted;
        private double lowestWave = 0;
        private double highestWave = 0;
        private double lowestMag = 0;
        private double highestMag = 0;
        private double scalingFactor = 0.1;
        private SensorDraw() 
        {
              
        }
        internal static SensorDraw GetInstance()
        {
            SensorDraw instance;
            if (!_state.LoadedWindows.ContainsKey(typeof(SensorDraw)))
                instance = new SensorDraw();
            else
            {
                instance = (SensorDraw)_state.LoadedWindows[typeof(SensorDraw)];
                if(_state.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _state.LastClickedEntity;
            }
            if(instance._selectedEntitySate != null)
            {
                if (_state.LastClickedEntity?.Entity != null && instance._selectedEntity != _state.LastClickedEntity.Entity)
                    instance._selectedEntitySate = _state.LastClickedEntity;
            }
            else
            {
                if(_state.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _state.LastClickedEntity;
            }
            
            instance._selectedStarSysState = _state.StarSystemStates[_state.SelectedStarSysGuid];
            return instance;
        }
        
        
        
        
        
        
        internal override void Display()
        {
            if (IsActive && _selectedEntitySate != null && ImGui.Begin("Sensor Display", ref IsActive))
            {
                
                if (_selectedEntity.HasDataBlob<SensorAbilityDB>())
                {
                    

                        if(_selectedReceverAtb == null || ImGui.Button("refresh") )
                        {
                            if (_selectedEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceverAtbDB>(out var recevers))
                            {
                                _selectedReceverAtb = new SensorReceverAtbDB[recevers.Count];
                                _recever = new Vector2[recevers.Count * 3];
                                int i = 0;
                                foreach (var recever in recevers)
                                {
                                    _selectedReceverAtb[i] = recever.Design.GetAttribute<SensorReceverAtbDB>();
                                    
                                    
                                    float x0_lowWave = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMin_nm;
                                    float x1_midWave = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthAverage_nm;
                                    float x2_highWave = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMax_nm;

                                    float y0_2_ = (float)_selectedReceverAtb[i].WorstSensitivity_kW;
                                    float y1_magnitude = (float)_selectedReceverAtb[i].BestSensitivity_kW;
                                    
                                    Vector2 p0 = new Vector2(x0_lowWave, y0_2_);
                                    Vector2 p1 = new Vector2(x1_midWave, y1_magnitude);
                                    Vector2 p2 =  new Vector2(x2_highWave, y0_2_);

                                    _recever[i] = p0;
                                    _recever[i + 1] = p1;
                                    _recever[i + 2] = p2;

                                    if (x0_lowWave < lowestWave)
                                        lowestWave = x0_lowWave;
                                    if (x2_highWave > highestWave)
                                        highestWave = x2_highWave;
                                    if (y0_2_ > lowestMag)
                                        lowestMag = y0_2_;
                                    if (y1_magnitude > highestMag)
                                        highestMag = y1_magnitude;
                                    i++;
                                }

                                var tgts = _selectedStarSys.GetAllEntitiesWithDataBlob<SensorProfileDB>();
                                _potentialTargetNames = new string[tgts.Count];
                                _potentialTargetEntities = tgts.ToArray();
                                i = 0;
                                foreach (var target in tgts)
                                {
                                    string name = target.GetDataBlob<NameDB>().GetName(_state.Faction);
                                    _potentialTargetNames[i] = name;
                                    i++;
                                }
                                
                            }
                        }
                        
                        if(ImGui.Combo("Targets", ref _targetIndex, _potentialTargetNames, _potentialTargetNames.Length))
                        {
                            _targetEntity = _potentialTargetEntities[_targetIndex];
                            SetTargetData();
                        }
                
                        
                        ImGui.Text("lowest_x: " + lowestWave);
                        ImGui.Text("highest_x: " + highestWave);
                        ImGui.Text("lowest_y" + lowestMag);
                        ImGui.Text("highest_y" + highestMag);
                        if(_targetSensorProfile != null)
                            ImGui.Text("target cross section" + _targetSensorProfile.TargetCrossSection_msq);
                        
                        
                        ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
                        uint borderColour = ImGui.ColorConvertFloat4ToU32(new Vector4(50, 50, 50, 255));
                        uint receverColour = ImGui.ColorConvertFloat4ToU32(new Vector4(50, 200, 50, 255));
                        uint reflectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(200, 200, 50, 255));
                        uint emittedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(200, 40, 50, 255));
                        Vector2 canvas_pos = ImGui.GetCursorScreenPos();            // ImDrawList API uses screen coordinates!
                        Vector2 canvas_size = ImGui.GetContentRegionAvail();
                        Vector2 canvas_endPos = canvas_pos + canvas_size;
                        

                        
                        draw_list.AddRect(canvas_pos, canvas_endPos, borderColour );

                        for (int i = 0; i < _recever.Length / 3; i++)
                        {

                            float x0 = (float)(canvas_pos.X + _recever[i].X * scalingFactor);
                            float y0 = (float)(canvas_pos.Y + _recever[i].Y * scalingFactor);
                            float x1 = (float)(canvas_pos.X + _recever[i+1].X * scalingFactor);
                            float y1 = (float)(canvas_pos.Y + _recever[i+1].Y * scalingFactor);
                            float x2 = (float)(canvas_pos.X + _recever[i+2].X * scalingFactor);
                            float y2 = (float)(canvas_pos.Y + _recever[i+2].Y * scalingFactor);
                            
                            draw_list.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), receverColour);
                            draw_list.AddLine(new Vector2(x1, y1), new Vector2(x2, y2), receverColour);
                        }
 
                        if(_reflected != null)
                        {
                            for (int i = 0; i < _reflected.Length / 3; i++)
                            {

                                float x0 = (float)(canvas_pos.X + _reflected[i].X * scalingFactor);
                                float y0 = (float)(canvas_pos.Y + _reflected[i].Y * scalingFactor);
                                float x1 = (float)(canvas_pos.X + _reflected[i + 1].X * scalingFactor);
                                float y1 = (float)(canvas_pos.Y + _reflected[i + 1].Y * scalingFactor);
                                float x2 = (float)(canvas_pos.X + _reflected[i + 2].X * scalingFactor);
                                float y2 = (float)(canvas_pos.Y + _reflected[i + 2].Y * scalingFactor);

                                draw_list.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), reflectedColour);
                                draw_list.AddLine(new Vector2(x1, y1), new Vector2(x2, y2), reflectedColour);
                            }
                        }
                        
                        if(_emmitted != null)
                        {
                            for (int i = 0; i < _emmitted.Length / 3; i++)
                            {

                                float x0 = (float)(canvas_pos.X + _emmitted[i].X * scalingFactor);
                                float y0 = (float)(canvas_pos.Y + _emmitted[i].Y * scalingFactor);
                                float x1 = (float)(canvas_pos.X + _emmitted[i + 1].X * scalingFactor);
                                float y1 = (float)(canvas_pos.Y + _emmitted[i + 1].Y * scalingFactor);
                                float x2 = (float)(canvas_pos.X + _emmitted[i + 2].X * scalingFactor);
                                float y2 = (float)(canvas_pos.Y + _emmitted[i + 2].Y * scalingFactor);

                                draw_list.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), emittedColour);
                                draw_list.AddLine(new Vector2(x1, y1), new Vector2(x2, y2), emittedColour);
                            }
                        }

                }

                void SetTargetData()
                {
                    _targetSensorProfile = _targetEntity.GetDataBlob<SensorProfileDB>();
                    if (_targetSensorProfile.ReflectedEMSpectra.Count == 0)
                        SetReflectedEMProfile.SetEntityProfile(_targetEntity, _state.PrimarySystemDateTime);
                    var emitted = _targetSensorProfile.EmittedEMSpectra;
                    var reflected = _targetSensorProfile.ReflectedEMSpectra;

                    _reflected = new Vector2[reflected.Count * 3];
                    _emmitted = new Vector2[emitted.Count * 3];
                    int i = 0;
                    foreach (var waveformkvp in reflected)
                    {
                        float low = (float)waveformkvp.Key.WavelengthMin_nm;
                        float mid = (float)waveformkvp.Key.WavelengthAverage_nm;
                        float high = (float)waveformkvp.Key.WavelengthMax_nm;
                        float magnatude = (float)waveformkvp.Value;
                        if (float.IsInfinity(magnatude))
                            magnatude = float.MaxValue;
                        
                        Vector2 p0 = new Vector2(low, 0);
                        Vector2 p1 = new Vector2(mid, magnatude);
                        Vector2 p2 = new Vector2(high, 0);

                        _reflected[i] = p0;
                        _reflected[i + 1] = p1;
                        _reflected[i + 2] = p2;



                        if (low < lowestWave)
                            lowestWave = low;
                        if (high > highestWave)
                            highestWave = high;
                        if (magnatude > highestMag)
                            highestMag = magnatude;
                        
                    }
                    
                    i = 0;
                    foreach (var waveformkvp in emitted)
                    {
                        float low = (float)waveformkvp.Key.WavelengthMin_nm;
                        float mid = (float)waveformkvp.Key.WavelengthAverage_nm;
                        float high = (float)waveformkvp.Key.WavelengthMax_nm;
                        float magnatude = (float)waveformkvp.Value;
                        if (float.IsInfinity(magnatude))
                            magnatude = float.MaxValue;
                        
                        Vector2 p0 = new Vector2(low, 0);
                        Vector2 p1 = new Vector2(mid, magnatude);
                        Vector2 p2 =  new Vector2(high, 0);

                        _emmitted[i] = p0;
                        _emmitted[i + 1] = p1;
                        _emmitted[i + 2] = p2;
                        
                        if (low < lowestWave)
                            lowestWave = low;
                        if (high > highestWave)
                            highestWave = high;
                        if (magnatude > highestMag)
                            highestMag = magnatude;
                    }
                    
                }


            }
        }
    }
}