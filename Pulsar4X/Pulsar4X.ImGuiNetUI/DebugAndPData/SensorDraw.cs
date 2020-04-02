using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class SensorDraw : PulsarGuiWindow
    {

        public class WaveDrawData
        {
            public int Count { get { return Points.Length; } }
            public bool HasAtn = false;
            public (Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)[] Points = new (Vector2, Vector2, Vector2, Vector2)[0];
            public (bool drawSrc,bool drawAtn)[] IsWaveDrawn = new (bool, bool)[0];
            public uint[] _receverColours;
        }
        
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

        private ImDrawListPtr _draw_list;
        
        private WaveDrawData _receverDat;
        private WaveDrawData _reflectDat;
        private WaveDrawData _emmittrDat;
        
        private double lowestWave = 0;
        private double highestWave = 0;
        private double lowestMag = 0;
        private double highestMag = 0;
        private Vector2 _scalingFactor = new Vector2(0.1f, 0.1f);
        private Vector2 _translation = new Vector2(0,0);
        private SensorDraw() 
        {
            _draw_list = ImGui.GetWindowDrawList();
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
            //
            if(!IsActive || _selectedEntitySate == null)
                return;
            ImGui.SetNextWindowSize(new Vector2(1500, 800));
            if (ImGui.Begin("Sensor Display", ref IsActive))
            {
                
                if (_selectedEntity.HasDataBlob<SensorAbilityDB>())
                {
                    if(_selectedReceverAtb == null || ImGui.Button("refresh") )
                    {
                        SetSensorData();     
                    }
                    
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 300);
                    
                    if(ImGui.Combo("Targets", ref _targetIndex, _potentialTargetNames, _potentialTargetNames.Length))
                    {
                        _targetEntity = _potentialTargetEntities[_targetIndex];
                        SetTargetData();
                    }
            
                    
                    ImGui.Text("lowest_x: " + lowestWave);
                    ImGui.Text("highest_x: " + highestWave);
                    ImGui.Text("lowest_y: " + lowestMag);
                    ImGui.Text("highest_y: " + highestMag);
                    if(_targetSensorProfile != null)
                        ImGui.Text("target cross section: " + _targetSensorProfile.TargetCrossSection_msq);
                    
                    
                    
                    uint borderColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    
                    uint receverColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 1.0f, 0.5f, 1.0f));
                    uint receverFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 1.0f, 0.5f, 0.75f));
                    
                    uint reflectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 1.0f));
                    uint reflectedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 0.75f));
                    
                    uint emittedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.25f, 1.0f));
                    uint emittedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.25f, 0.5f));

                    ImGui.BeginChild("stuff");

                    ImGui.Text("Recevers: ");
                    ImGui.PushID("recevers");
                    DisplayWavInfo(_receverDat);
                    ImGui.PopID();
                    if(_reflectDat != null)
                    {
                        ImGui.PushID("reflectors");
                        ImGui.Text("Reflectors:");
                        DisplayWavInfo(_reflectDat);
                        ImGui.PopID();
                    }
                    if(_emmittrDat != null)
                    {
                        ImGui.PushID("emmiters");
                        ImGui.Text("Emmiters:");
                        DisplayWavInfo(_emmittrDat);
                        ImGui.PopID();
                    }
                    ImGui.EndChild();
                    
                    ImGui.NextColumn();
                    
                    // ImDrawList API uses screen coordinates!
                    Vector2 canvas_pos = ImGui.GetCursorScreenPos();
                    Vector2 canvas_size = ImGui.GetContentRegionAvail();
                    Vector2 canvas_endPos = canvas_pos + canvas_size;
                    Vector2 waveBounds = new Vector2((float)(highestWave - lowestWave),(float)( highestMag - lowestMag));

                    _scalingFactor.X = 1 / (waveBounds.X / canvas_size.X);
                    _scalingFactor.Y = 1 / (waveBounds.Y / canvas_size.Y);

                    _translation.X = (float)(canvas_pos.X - lowestWave * _scalingFactor.X);
                    _translation.Y = (float)(canvas_pos.Y - lowestMag * _scalingFactor.Y);  
     
                    _draw_list.AddRect(canvas_pos, canvas_endPos, borderColour );
                    
                    ImGui.Text("Scale:");
                    ImGui.Text("X: " + _scalingFactor.X + " Y: " + _scalingFactor.Y);
                    
                    Vector2 p0 = _translation + new Vector2((float)lowestWave, (float)lowestMag) * _scalingFactor;
                    Vector2 p1 = _translation + new Vector2((float)highestWave, (float)highestMag) * _scalingFactor;
                    ImGui.Text("Box From: " + p0);
                    ImGui.Text("Box To:   " + p1);

                    DrawWav(_receverDat, receverColour);

                    if(_reflectDat != null)
                        DrawWav(_reflectDat, reflectedColour);
                    if(_emmittrDat != null)
                        DrawWav(_emmittrDat, emittedColour);

                }

    
                
                
                void DrawWav(WaveDrawData wavesArry, uint colour)
                {
                    for (int i = 0; i < wavesArry.Count; i++)
                    {
                        Vector2 p0 = _translation + wavesArry.Points[i].p0 * _scalingFactor;
                        Vector2 p1 = _translation + wavesArry.Points[i].p1 * _scalingFactor;
                        Vector2 p2 = _translation + wavesArry.Points[i].p2 * _scalingFactor;
                        if (wavesArry.IsWaveDrawn[i].drawSrc)
                        {

                            //_draw_list.AddLine(p0, p1, colour);
                            //_draw_list.AddLine(p1, p2, colour);
                            _draw_list.AddTriangleFilled(p0, p1, p2, colour);
                        }

                        if (wavesArry.HasAtn && wavesArry.IsWaveDrawn[i].drawAtn)
                        {
                            Vector2 p3 = _translation + wavesArry.Points[i].p3 * _scalingFactor;
                            _draw_list.AddTriangleFilled(p0, p3, p2, colour);
                        }

                    }
                    
                }

                void DisplayWavInfo(WaveDrawData wavesArry)
                {
                    for (int i = 0; i < wavesArry.Count; i++)
                    {
                        if(ImGui.Checkbox("Show Wave##drawbool" + i, ref wavesArry.IsWaveDrawn[i].drawSrc))
                            ResetBounds();

                        if(wavesArry.HasAtn)
                        { 
                            ImGui.SameLine();
                            if(ImGui.Checkbox("Show Attenuated Wave##drawbool" + i, ref wavesArry.IsWaveDrawn[i].drawAtn))
                                ResetBounds();
                        }

                        ImGui.Text("MinWav: " + wavesArry.Points[i].p0 .X);
                        ImGui.SameLine();
                        ImGui.Text("Magnitude: " + wavesArry.Points[i].p0.Y);

                        ImGui.Text("AvgWav: " + wavesArry.Points[i].p1.X);
                        ImGui.Text("Magnitude: " + wavesArry.Points[i].p1.Y);
                        if(wavesArry.HasAtn)
                            ImGui.Text("Attenuated Magnitude: " + wavesArry.Points[i].p3.Y);

                        ImGui.Text("MaxWav: " + wavesArry.Points[i].p2.X);
                        ImGui.SameLine();
                        ImGui.Text("Magnitude: " + wavesArry.Points[i].p2.Y);
                    }
                }
                
               

                void ResetBounds()
                {
                    lowestWave = float.PositiveInfinity;
                    lowestMag = float.PositiveInfinity;
                    highestMag = float.NegativeInfinity;
                    highestWave = float.NegativeInfinity;

                    var dat = _receverDat;
                    for (int i = 0; i < dat.Count; i++)
                    {
                        if(dat.IsWaveDrawn[i].drawSrc)
                        {
                            float low = dat.Points[i].p0.X;
                            float high = dat.Points[i].p2.X;
                            float mag1 = dat.Points[i].p0.Y; //recever highest value
                            float mag2 = dat.Points[i].p1.Y; //recever lowest value
                            if (low < lowestWave)
                                lowestWave = low;
                            if (high > highestWave)
                                highestWave = high;
                            if (mag1 > highestMag)
                                highestMag = mag1;
                            if (mag2 < lowestMag)
                                lowestMag = mag2;
                        }
                    }

                    if(_reflectDat != null)
                        ResetTargetBounds(_reflectDat);
                    if(_emmittrDat != null)
                        ResetTargetBounds(_emmittrDat);
                }

                void ResetTargetBounds(WaveDrawData dat)
                {
                    for (int i = 0; i < dat.Count; i++)
                    {
                        if(dat.IsWaveDrawn[i].drawSrc || dat.IsWaveDrawn[i].drawAtn)
                        {
                            float low = dat.Points[i].p0.X;
                            float high = dat.Points[i].p2.X;
                            float mag1 = dat.Points[i].p0.Y; //xmit lowest value prob 0
                            float mag2 = dat.Points[i].p1.Y; //xmit highest value
                            float mag3 = dat.Points[i].p3.Y; //xmit 2nd highest value
                            
                            if (low < lowestWave)
                                lowestWave = low;
                            if (high > highestWave)
                                highestWave = high;
                            
                            if (mag1 < lowestMag) //will likely be 0
                                lowestMag = mag1;
                            
                            if(dat.IsWaveDrawn[i].drawSrc)
                            {
                                if (mag2 > highestMag)
                                    highestMag = mag2;
                            }
                            else if(dat.IsWaveDrawn[i].drawAtn)
                            {
                                if (mag3 > highestMag)
                                    highestMag = mag3;
                            }
                        }
                    }
                }



                void SetSensorData()
                {                            
                    if (_selectedEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceverAtbDB>(out var recevers))
                    {
                        _receverDat = new WaveDrawData();
                        _receverDat.HasAtn = false;
                        var points = _receverDat.Points = new (Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)[recevers.Count];
                        _receverDat.IsWaveDrawn = new (bool drawSrc, bool drawAtn)[recevers.Count];
                        
                        _selectedReceverAtb = new SensorReceverAtbDB[recevers.Count];
                        
                        int i = 0;
                        foreach (var recever in recevers)
                        {
                            _selectedReceverAtb[i] = recever.Design.GetAttribute<SensorReceverAtbDB>();
                            
                            
                            float low = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMin_nm;
                            float mid = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthAverage_nm;
                            float high = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMax_nm;

                            float mag1 = (float)_selectedReceverAtb[i].WorstSensitivity_kW;
                            float mag2 = (float)_selectedReceverAtb[i].BestSensitivity_kW;
                            
                            points[i].p0 = new Vector2(low, mag1);
                            points[i].p1 = new Vector2(mid, mag2);
                            points[i].p2 =  new Vector2(high, mag1);
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

                void SetTargetData()
                {

                    _targetSensorProfile = _targetEntity.GetDataBlob<SensorProfileDB>();
                    SetReflectedEMProfile.SetEntityProfile(_targetEntity, _state.PrimarySystemDateTime);
                    var emitted = _targetSensorProfile.EmittedEMSpectra;
                    var reflected = _targetSensorProfile.ReflectedEMSpectra;
                    var range = PositionDB.GetDistanceBetween_m(_selectedEntity.GetDataBlob<PositionDB>(), _targetEntity.GetDataBlob<PositionDB>());

                    _reflectDat = MakeTargetWavDat(reflected, range);
                    _emmittrDat = MakeTargetWavDat(emitted, range);

                }

                WaveDrawData MakeTargetWavDat(Dictionary<EMWaveForm, double> wavsDict, double range)
                {
                    var wavDat = new WaveDrawData();
                    wavDat.HasAtn = true;
                    var datPts = wavDat.Points = new (Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)[wavsDict.Count];
                    wavDat.IsWaveDrawn = new (bool drawSrc, bool drawAtn)[wavsDict.Count];

                    int i = 0;
                    foreach (var waveformkvp in wavsDict)
                    {
                        float low = (float)waveformkvp.Key.WavelengthMin_nm;
                        float mid = (float)waveformkvp.Key.WavelengthAverage_nm;
                        float high = (float)waveformkvp.Key.WavelengthMax_nm;
                        float magnatude = (float)waveformkvp.Value;
                        float atnmag = (float)SensorProcessorTools.AttenuationCalc(magnatude, range);
                        if (float.IsInfinity(magnatude))
                            magnatude = float.MaxValue;
                        
                        datPts[i].p0 = new Vector2(low, 0);
                        datPts[i].p1 = new Vector2(mid, magnatude);
                        datPts[i].p2 = new Vector2(high, 0);
                        datPts[i].p3 = new Vector2(mid, atnmag);
                        i++;
                    }

                    return wavDat;
                }
            }
        }
    }
}