using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Extensions;
using Pulsar4X.Names;
using Pulsar4X.Sensors;
using Pulsar4X.Movement;

namespace Pulsar4X.Client
{
    public class SensorDraw : PulsarGuiWindow
    {
        private EntityState? _selectedEntitySate;
        private Entity? _selectedEntity => _selectedEntitySate?.Entity;
        private Entity[]? _potentialTargetEntities;
        private string[]? _potentialTargetNames;
        private int _targetIndex = -1;
        private Entity? _targetEntity;
        private SensorProfileDB? _targetSensorProfile;
        private SensorReturnValues[]? _targetDetectionQuality;

        private  Dictionary<EMWaveForm, double> _attenuatedWaveForms = new ();

        private SystemState? _selectedStarSysState;
        private StarSystem? _selectedStarSys => _selectedStarSysState?.StarSystem;

        private SensorReceiverAtb[]? _selectedReceverAtb;
        private SensorReceiverAbility[]? _selectedReceverInstanceAbility;


        private ImDrawListPtr _draw_list;

        private WaveDrawData? _receverDat;
        
        private WaveDrawData? _reflectDat;
        private WaveDrawData? _emmittrDat;
        private WaveDrawData? _detectedDat;


        private double rlowestWave = 0;
        private double rhighestWave = 0;
        private double rlowestMag = 0;
        private double rhighestMag = 0;
        private double slowestWave = 0;
        private double shighestWave = 0;
        private double slowestMag = 0;
        private double shighestMag = 0;
        private (double, double) _recDatWave;
        private (double, double) _recDatMag;
        private (double, double) _sigDatWave;
        private (double, double) _sigDatMag;
        
        private System.Numerics.Vector2 _scalingFactor = new System.Numerics.Vector2(0.1f, 0.1f);
        private System.Numerics.Vector2 _translation = new System.Numerics.Vector2(0,0);
        
        uint _borderColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        uint _receverColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 1.0f, 0.5f, 1.0f));
        uint _receverFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 1.0f, 0.5f, 0.9f));
        uint _reflectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 1.0f));
        uint _reflectedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 0.75f));
        uint _emittedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.25f, 1.0f));
        uint _emittedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.1f, 0.1f));
        uint _detectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 1.0f, 0.1f));
        
        private SensorDraw()
        {
            _draw_list = ImGui.GetWindowDrawList();
        }
        internal static SensorDraw GetInstance()
        {
            SensorDraw instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SensorDraw)))
                instance = new SensorDraw();
            else
            {
                instance = (SensorDraw)_uiState.LoadedWindows[typeof(SensorDraw)];
                if(_uiState.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _uiState.LastClickedEntity;
            }
            if(instance._selectedEntitySate != null)
            {
                if (_uiState.LastClickedEntity?.Entity != null && instance._selectedEntity != _uiState.LastClickedEntity.Entity)
                    instance._selectedEntitySate = _uiState.LastClickedEntity;
            }
            else
            {
                if(_uiState.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _uiState.LastClickedEntity;
            }

            if (_uiState.IsGameLoaded && !string.IsNullOrEmpty(_uiState.SelectedStarSystemId))
                instance._selectedStarSysState = _uiState.StarSystemStates[_uiState.SelectedStarSystemId];
            else
                instance._selectedStarSysState = null;
            return instance;
        }






        internal override void Display()
        {
            if(!IsActive || _selectedEntitySate == null || _selectedEntity == null)
                return;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(1500, 800));
            
            if (Window.Begin("Sensor Display: " + _selectedEntitySate.Name, ref IsActive))
            {
                if (_selectedEntity.HasDataBlob<SensorAbilityDB>())
                {
                    if (_selectedReceverAtb == null || ImGui.Button("refresh"))
                    {
                        SetSensorData();
                        SetTargetData();
                    }

                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 300);

                    if (_potentialTargetNames != null
                        && _potentialTargetEntities != null
                        && ImGui.Combo("Targets", ref _targetIndex, _potentialTargetNames, _potentialTargetNames.Length))
                    {
                        _targetEntity = _potentialTargetEntities[_targetIndex];
                        SetTargetData();
                    }


                    ImGui.Text("rlowest_x: " + rlowestWave);
                    ImGui.Text("rhighest_x: " + rhighestWave);
                    ImGui.Text("rlowest_y: " + rlowestMag);
                    ImGui.Text("rhighest_y: " + rhighestMag);
                    if (_targetSensorProfile != null)
                        ImGui.Text("target cross section: " + _targetSensorProfile.TargetCrossSection_msq);
                    

                    ImGui.BeginChild("stuff");

                    BorderGroup.Begin("Recevers:", _borderColour);
                    DisplayWavInfo(_receverDat);
                    BorderGroup.End();

                    if (_reflectDat != null)
                    {
                        BorderGroup.Begin("Reflectors:", _borderColour);
                        DisplayWavInfo(_reflectDat);
                        BorderGroup.End();

                    }

                    if (_emmittrDat != null)
                    {
                        BorderGroup.Begin("Emmiters:", _borderColour);
                        DisplayWavInfo(_emmittrDat);
                        BorderGroup.End();

                    }

                    if (_detectedDat != null)
                    {
                        BorderGroup.Begin("Detected:", _borderColour);
                        DisplayWavInfo(_detectedDat);
                        BorderGroup.End();
                    }

                    ImGui.EndChild();

                    ImGui.NextColumn();
                    Draw();

                }
                Window.End();

            }

            void Draw()
            {
                _draw_list = ImGui.GetWindowDrawList();
                // ImDrawList API uses screen coordinates!
                System.Numerics.Vector2 canvas_pos = ImGui.GetCursorScreenPos();
                System.Numerics.Vector2 canvas_size = ImGui.GetContentRegionAvail();
                System.Numerics.Vector2 canvas_endPos = canvas_pos + canvas_size;
                System.Numerics.Vector2 waveBounds = new System.Numerics.Vector2((float)(rhighestWave - rlowestWave), (float)(rhighestMag - rlowestMag));

                _scalingFactor.X = 1 / (waveBounds.X / canvas_size.X);
                _scalingFactor.Y = 1 / (waveBounds.Y / canvas_size.Y);

                _translation.X = (float)(canvas_pos.X - rlowestWave * _scalingFactor.X);
                _translation.Y = (float)(canvas_pos.Y - rlowestMag * _scalingFactor.Y);

                _draw_list.AddRect(canvas_pos, canvas_endPos, _borderColour);

                ImGui.Text("Scale:");
                ImGui.Text("X: " + _scalingFactor.X + " Y: " + _scalingFactor.Y);

                System.Numerics.Vector2 p0 = _translation + new System.Numerics.Vector2((float)rlowestWave, (float)rlowestMag) * _scalingFactor;
                System.Numerics.Vector2 p1 = _translation + new System.Numerics.Vector2((float)rhighestWave, (float)rhighestMag) * _scalingFactor;
                ImGui.Text("Box From: " + p0);
                ImGui.Text("Box To:   " + p1);

                DrawWav(_receverDat, _receverFill);

                if (_reflectDat != null)
                    DrawWav(_reflectDat, _reflectedFill);
                if (_emmittrDat != null)
                    DrawWav(_emmittrDat, _emittedFill);
                if (_detectedDat != null)
                    DrawWav(_detectedDat, _detectedColour);
            }

            void DrawWav(WaveDrawData? wavesArry, uint colour)
            {
                if(wavesArry == null) return;
                for (int i = 0; i < wavesArry.Count; i++)
                {
                    System.Numerics.Vector2 p0 = _translation + wavesArry.Points[i].p0 * _scalingFactor;
                    System.Numerics.Vector2 p1 = _translation + wavesArry.Points[i].p1 * _scalingFactor;
                    System.Numerics.Vector2 p2 = _translation + wavesArry.Points[i].p2 * _scalingFactor;
                    if (wavesArry.IsWaveDrawn[i].drawSrc)
                    {

                        //_draw_list.AddLine(p0, p1, colour);
                        //_draw_list.AddLine(p1, p2, colour);
                        _draw_list.AddTriangleFilled(p0, p1, p2, colour);
                    }

                    if (wavesArry.HasAtn && wavesArry.IsWaveDrawn[i].drawAtn)
                    {
                        System.Numerics.Vector2 p3 = _translation + wavesArry.Points[i].p3 * _scalingFactor;
                        _draw_list.AddTriangleFilled(p0, p3, p2, colour);
                    }

                }

            }

            void DisplayWavInfo(WaveDrawData? wavesArry)
            {
                if(wavesArry == null) return;

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

                    ImGui.Text("MinWav: " + Stringify.DistanceSmall(wavesArry.Points[i].p0 .X));
                    ImGui.SameLine();
                    ImGui.Text("Magnitude: " + Stringify.Power(wavesArry.Points[i].p0.Y));

                    ImGui.Text("AvgWav: " + Stringify.DistanceSmall(wavesArry.Points[i].p1.X));

                    if(wavesArry.HasAtn)
                    {
                        ImGui.SameLine();
                        ImGui.Text(" Magnitude peak/attenuated:");
                        ImGui.Text("   "+Stringify.Power(wavesArry.Points[i].p1.Y) + "/" + Stringify.Power(wavesArry.Points[i].p3.Y));
                    }
                    else
                    {
                        ImGui.SameLine();
                        ImGui.Text(" Magnitude peak:");
                        ImGui.Text("   "+Stringify.Power(wavesArry.Points[i].p1.Y));
                    }

                    ImGui.Text("MaxWav: " + Stringify.DistanceSmall(wavesArry.Points[i].p2.X));
                    ImGui.SameLine();
                    ImGui.Text("Magnitude: " + Stringify.Power(wavesArry.Points[i].p2.Y));
                }
            }

            void ResetBounds()
            {
                if(_receverDat == null) return;
                 rlowestWave = float.PositiveInfinity;
                 rlowestMag = float.PositiveInfinity;
                 rhighestMag = float.NegativeInfinity;
                 rhighestWave = float.NegativeInfinity;

                for (int i = 0; i < _receverDat.Count; i++)
                {
                    if(_receverDat.IsWaveDrawn[i].drawSrc)
                    {
                        float low = _receverDat.Points[i].p0.X;
                        float high = _receverDat.Points[i].p2.X;
                        float mag1 = _receverDat.Points[i].p0.Y; //recever Worst sensitivity/highest value
                        float mag2 = _receverDat.Points[i].p1.Y; //recever Best sensitivity
                        if (low < rlowestWave)
                            rlowestWave = low;
                        if (high > rhighestWave)
                            rhighestWave = high;
                        if (mag1 > rhighestMag)
                            rhighestMag = mag1;
                        if (mag2 < rlowestMag)
                            rlowestMag = mag2;
                    }
                }
                _recDatWave = (rlowestWave, rhighestWave);
                _recDatMag = (rlowestMag, rhighestMag);

                if(_reflectDat != null)
                    ResetTargetBounds(_reflectDat);
                if(_emmittrDat != null)
                    ResetTargetBounds(_emmittrDat);
                if(_detectedDat != null)
                    ResetTargetBounds(_detectedDat);
            }

            void ResetTargetBounds(WaveDrawData dat)
            {
                 slowestWave = float.PositiveInfinity;
                 slowestMag = float.PositiveInfinity;
                 shighestMag = float.NegativeInfinity;
                 shighestWave = float.NegativeInfinity;
                
                for (int i = 0; i < dat.Count; i++)
                {
                    if(dat.IsWaveDrawn[i].drawSrc || dat.IsWaveDrawn[i].drawAtn)
                    {
                        float low = dat.Points[i].p0.X;
                        float high = dat.Points[i].p2.X;
                        float mag1 = dat.Points[i].p0.Y; //xmit lowest value prob 0
                        float mag2 = dat.Points[i].p1.Y; //xmit highest value
                        float mag3 = dat.Points[i].p3.Y; //xmit 2nd highest value

                        
                        
                        if (low < slowestWave)
                            slowestWave = low;
                        if (high > shighestWave)
                            shighestWave = high;
                        if (mag1 > shighestMag)
                            shighestMag = mag1;
                        if (mag2 < slowestMag)
                            slowestMag = mag2;
                        

                        if(dat.IsWaveDrawn[i].drawSrc)
                        {
                            if (mag2 > shighestMag)
                                shighestMag = mag2;
                        }
                        if(dat.IsWaveDrawn[i].drawAtn)
                        {
                            if (mag3 > shighestMag)
                                shighestMag = mag3;
                        }
                    }
                }
                _sigDatWave = (slowestWave, shighestWave);
                _sigDatMag = (slowestMag, shighestMag);
            }



            void SetSensorData()
            {
                if(_selectedStarSys == null) return;

                if (_selectedEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceiverAtb>(out var recevers))
                {
                    _receverDat = new WaveDrawData();
                    _receverDat.HasAtn = false;
                    var points = _receverDat.Points = new (System.Numerics.Vector2 p0, System.Numerics.Vector2 p1, System.Numerics.Vector2 p2, System.Numerics.Vector2 p3)[recevers.Count];
                    _receverDat.IsWaveDrawn = new (bool drawSrc, bool drawAtn)[recevers.Count];

                    _selectedReceverAtb = new SensorReceiverAtb[recevers.Count];
                    _selectedReceverInstanceAbility = new SensorReceiverAbility[recevers.Count];
                    int i = 0;
                    foreach (var recever in recevers)
                    {
                        _selectedReceverAtb[i] = recever.Design.GetAttribute<SensorReceiverAtb>();
                        _selectedReceverInstanceAbility[i] = recever.GetAbilityState<SensorReceiverAbility>();

                        float low = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMin_nm;
                        float mid = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthAverage_nm;
                        float high = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMax_nm;

                        float mag1 = (float)_selectedReceverAtb[i].WorstSensitivity_kW;
                        float mag2 = (float)_selectedReceverAtb[i].BestSensitivity_kW;

                        points[i].p0 = new System.Numerics.Vector2(low, mag1);
                        points[i].p1 = new System.Numerics.Vector2(mid, mag2);
                        points[i].p2 =  new System.Numerics.Vector2(high, mag1);
                        i++;
                    }

                    var tgts = _selectedStarSys.GetAllEntitiesWithDataBlob<SensorProfileDB>();
                    _potentialTargetNames = new string[tgts.Count];
                    _potentialTargetEntities = tgts.ToArray();
                    i = 0;
                    foreach (var target in tgts)
                    {
                        string name = target.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                        _potentialTargetNames[i] = name;
                        i++;
                    }

                    for (int j = 0; j < _selectedReceverInstanceAbility.Length; j++)
                    {
                        //SetTargetData();
                        //var foo = _selectedReceverInstanceAbility[i].CurrentContacts;
                        //foreach (SensorProcessorTools.SensorReturnValues val in foo.Values)
                        //{
                            //val.SignalStrength_kW
                        //}

                    }

                }
            }

            void SetTargetData()
            {
                if(_selectedReceverAtb == null) return;
                if(_targetEntity == null) return;
                 _targetSensorProfile = _targetEntity.GetDataBlob<SensorProfileDB>();
                SensorProfileTools.SetReflectionProfile(_targetSensorProfile, _uiState.PrimarySystemDateTime);
                var emitted = _targetSensorProfile.EmittedEMSpectra;
                var reflected = _targetSensorProfile.ReflectedEMSpectra;

                var posSelected = _selectedEntity.GetDataBlob<PositionDB>();

                var range = _selectedEntity.GetDataBlob<PositionDB>().GetDistanceTo_m(_targetEntity.GetDataBlob<PositionDB>());

                _reflectDat = MakeTargetWavDat(reflected, range, _reflectedFill);
                
                //_emmittrDat = MakeTargetWavDat(emitted, range, _emittedFill);
                _attenuatedWaveForms =  SensorTools.AttenuatedForDistance(_targetSensorProfile, range);
                //_detectedDat = _selectedReceverAtb[0].

                _targetDetectionQuality = new SensorReturnValues[_selectedReceverAtb.Length];
                for (int i = 0; i < _selectedReceverAtb.Length; i++)
                {
                    _targetDetectionQuality[i] = SensorTools.DetectonQuality(_selectedReceverAtb[i], _attenuatedWaveForms);
                }

                _detectedDat = MakeTargetWavDat(_attenuatedWaveForms, range, _detectedColour);


            }

            WaveDrawData MakeTargetWavDat(Dictionary<EMWaveForm, double> wavsDict, double range, uint colour)
            {
                var wavDat = new WaveDrawData();
                wavDat.HasAtn = true;
                var datPts = wavDat.Points = new (System.Numerics.Vector2 p0, System.Numerics.Vector2 p1, System.Numerics.Vector2 p2, System.Numerics.Vector2 p3)[wavsDict.Count];
                wavDat.IsWaveDrawn = new (bool drawSrc, bool drawAtn)[wavsDict.Count];
                wavDat._receverColours = new uint[wavsDict.Count];
                int i = 0;
                foreach (var waveformkvp in wavsDict)
                {
                    float low = (float)waveformkvp.Key.WavelengthMin_nm;
                    float mid = (float)waveformkvp.Key.WavelengthAverage_nm;
                    float high = (float)waveformkvp.Key.WavelengthMax_nm;
                    float magnatude = (float)waveformkvp.Value;
                    float atnmag = (float)SensorTools.AttenuationCalc(magnatude, range);
                    if (float.IsInfinity(magnatude))
                        magnatude = float.MaxValue;

                    datPts[i].p0 = new System.Numerics.Vector2(low, 0);
                    datPts[i].p1 = new System.Numerics.Vector2(mid, magnatude);
                    datPts[i].p2 = new System.Numerics.Vector2(high, 0);
                    datPts[i].p3 = new System.Numerics.Vector2(mid, atnmag);
                    wavDat._receverColours[i] = colour;
                    i++;
                }

                return wavDat;
            }

        }

        public override void OnGameTickChange(DateTime newDate)
        {

        }

        public override void OnSystemTickChange(DateTime newDate)
        {

        }
    }

    public class WaveDrawData
    {
        public int Count { get { return Points.Length; } }
        public bool HasAtn = false;
        public (System.Numerics.Vector2 p0, System.Numerics.Vector2 p1, System.Numerics.Vector2 p2, System.Numerics.Vector2 p3)[] Points = new (System.Numerics.Vector2, System.Numerics.Vector2, System.Numerics.Vector2, System.Numerics.Vector2)[0];
        public (bool drawSrc, bool drawAtn)[] IsWaveDrawn = new (bool, bool)[0];
        public uint[]? _receverColours;
        
    }


    public class SensorData : PulsarGuiWindow
    {
        private EntityState? _selectedEntitySate;
        private Entity? _selectedEntity => _selectedEntitySate?.Entity;
        private SystemState? _selectedStarSysState;

        private SensorAbilityDB _abilityDB;

        
        #region targetVariables
        private Entity[]? _potentialTargetEntities;
        private string[]? _potentialTargetNames;
        private int _targetIndex = -1;
        //private Entity? _targetEntity;
        private SensorProfileDB? _targetSensorProfile;
        private SensorReturnValues[]? _targetDetectionQuality;
        //Dictionary<EMWaveForm, double> _emitted = new ();
        List<EMData> _emitted = new ();
        Dictionary<EMWaveForm, double> _reflected = new ();
        #endregion

        #region drawData
        private double _lowestWave = 0;
        private double _highestWave = 0;
        private float _xscale = 1.0f;
        
        private double _highestMagnitude = 0;
        private double _lowestMagnitude = 0;
        private float _yscale = 1.0f;
        
        uint _canvasBorderColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        uint _reflectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
        uint _emittedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 1.0f, 1.0f));

        private System.Numerics.Vector2 _canvasPos;
        private System.Numerics.Vector2 _canvasSize;
        private System.Numerics.Vector2 _canvasEndPos;
        private System.Numerics.Vector2 _translation;
        private System.Numerics.Vector2 _scalingFactor;
        private System.Numerics.Vector2 wavP0;
        private System.Numerics.Vector2 wavP1;
        private System.Numerics.Vector2 wavP2;
        
        #endregion
        
        
        internal static SensorData GetInstance()
        {
            SensorData instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SensorData)))
                instance = new SensorData();
            else
            {
                instance = (SensorData)_uiState.LoadedWindows[typeof(SensorData)];
                if(_uiState.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _uiState.LastClickedEntity;
            }
            if(instance._selectedEntitySate != null)
            {
                if (_uiState.LastClickedEntity?.Entity != null && instance._selectedEntity != _uiState.LastClickedEntity.Entity)
                    instance._selectedEntitySate = _uiState.LastClickedEntity;
            }
            else
            {
                if(_uiState.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _uiState.LastClickedEntity;
            }

            if (_uiState.IsGameLoaded && !string.IsNullOrEmpty(_uiState.SelectedStarSystemId))
                instance._selectedStarSysState = _uiState.StarSystemStates[_uiState.SelectedStarSystemId];
            else
                instance._selectedStarSysState = null;
            
            if (instance._abilityDB == null || instance._abilityDB.OwningEntity != instance._selectedEntity)
            {
                instance.Setup();
            }
            return instance;
        }
        internal override void Display()
        {
            if(!IsActive || _selectedEntitySate == null || _selectedEntity == null)
                return;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(1500, 800));

            if (Window.Begin("Sensor Display2: " + _selectedEntitySate.Name, ref IsActive))
            {
                
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);
                if (_potentialTargetNames != null
                    && _potentialTargetEntities != null
                    && ImGui.Combo("Targets", ref _targetIndex, _potentialTargetNames, _potentialTargetNames.Length))
                {
                    SetTargetData(_potentialTargetEntities[_targetIndex]);
                }
                if (ImGui.Button("Refresh ReflectionProfile"))
                {
                    SensorProfileTools.SetReflectionProfile(_targetSensorProfile, _uiState.PrimarySystemDateTime);
                }
                
                
                if (_abilityDB != null)
                {
                    if (ImGui.Button("Scan"))
                    {
                        var ss = new SensorScan();
                        ss.TriggerProcess(_selectedEntity, _selectedStarSysState.StarSystem.StarSysDateTime);
                    }
                    
                    /*
                    foreach (var data in _abilityDB.CurrentContacts)
                    {
                        string tgtname = data.entity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                        var targetSensorProfile = data.entity.GetDataBlob<SensorProfileDB>();
                        ImGui.Text(tgtname);
                        ImGui.Text("Strength:");
                        ImGui.SameLine();
                        ImGui.Text(Stringify.Power(data.Item2.SignalStrength_kW));
                        ImGui.Text("Quality:");
                        ImGui.SameLine();
                        ImGui.Text(data.returnValues.SignalQuality.Percent.ToString());
                        
                        var emitted = targetSensorProfile.EmittedEMSpectra;
                        var reflected = targetSensorProfile.ReflectedEMSpectra;
                        
                        /*
                        var range = _selectedEntity.GetDataBlob<PositionDB>().GetDistanceTo_m(data.entity.GetDataBlob<PositionDB>());
                        var attenuatedWaveForms =  SensorTools.AttenuatedForDistance(targetSensorProfile, range);
                        foreach (var wf in attenuatedWaveForms)
                        {
                            ImGui.Text("Signal Strength: " + Stringify.Power(wf.Value));
                        }
                        */
                    //}
                
                }

                if (_targetSensorProfile != null)
                {
                    var emitted = _targetSensorProfile.EmittedEMSpectra;
                    var reflected = _targetSensorProfile.ReflectedEMSpectra;

                    ImGui.Text("Emitted");
                    foreach (var em in emitted)
                    {
                        ImGui.Text(em.GetName);
                        ImGui.Text(Stringify.Power( em.Magnitude));
                    }
                }
                
                ImGui.NextColumn();
                if(_targetSensorProfile != null)
                    Draw();
                
                
                Window.End();
            }
        }

        void Setup()
        {
            if(_selectedEntity == null)
                return;
            if (_selectedEntity.TryGetDataBlob<SensorAbilityDB>(out var sensorData))
            {
                _abilityDB = sensorData;
            }
            
            //gather potential targets data
            var tgts = _selectedStarSysState.StarSystem.GetAllEntitiesWithDataBlob<SensorProfileDB>();
            _potentialTargetNames = new string[tgts.Count];
            _potentialTargetEntities = tgts.ToArray();
            var i = 0;
            foreach (var target in tgts)
            {
                string name = target.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                _potentialTargetNames[i] = name;
                i++;
            }
        }

        void SetTargetData(Entity targetEntity)
        {
            _targetSensorProfile = targetEntity.GetDataBlob<SensorProfileDB>();
            _emitted = _targetSensorProfile.EmittedEMSpectra;
            _reflected = _targetSensorProfile.ReflectedEMSpectra;
            
            _lowestWave = double.MaxValue;
            _highestWave = double.MinValue;
            _lowestMagnitude = double.MaxValue;
            _highestMagnitude = double.MinValue;
            
            foreach (var emitter in _emitted)
            {
                _lowestWave = Math.Min(_lowestWave, emitter.WaveForm.WavelengthMin_nm);
                _highestWave = Math.Max(_highestWave, emitter.WaveForm.WavelengthMax_nm);
                _lowestMagnitude = Math.Min(_lowestMagnitude, emitter.Magnitude);
                _highestMagnitude = Math.Max(_highestMagnitude, emitter.Magnitude);
            }
            foreach (var reflect in _reflected)
            {
                _lowestWave = Math.Min(_lowestWave, reflect.Key.WavelengthMin_nm);
                _highestWave = Math.Max(_highestWave, reflect.Key.WavelengthMax_nm);
                _lowestMagnitude = Math.Min(_lowestMagnitude, reflect.Value);
                _highestMagnitude = Math.Max(_highestMagnitude, reflect.Value);
            }
            
            
        }


        void Draw()
        {
            var draw_list = ImGui.GetWindowDrawList();
            // ImDrawList API uses screen coordinates!
            _canvasPos = ImGui.GetCursorScreenPos();
            _canvasSize = ImGui.GetContentRegionAvail();
            _canvasEndPos = _canvasPos + _canvasSize;
            
            //calculate scale for canvas size.
            _scalingFactor.X = _canvasSize.X / (float)(_highestWave - _lowestWave);
            _scalingFactor.Y = _canvasSize.Y / (float)_highestMagnitude; 
            
            _translation.X = (float)(_canvasPos.X - _lowestWave * _scalingFactor.X);
            _translation.Y = _canvasEndPos.Y - 2;
            
            //draw canvas boarder.
            draw_list.AddRect(_canvasPos, _canvasEndPos, _canvasBorderColour);
            foreach (var em in _reflected)
            { 
                wavP0.X =  (float)(_translation.X + em.Key.WavelengthMin_nm * _scalingFactor.X);
                wavP0.Y = _translation.Y;
                
                wavP1.X =  (float)(_translation.X + em.Key.WavelengthAverage_nm * _scalingFactor.X);
                wavP1.Y = (float)(_translation.Y - em.Value * _scalingFactor.Y);
                
                wavP2.X =  (float)(_translation.X + em.Key.WavelengthMax_nm * _scalingFactor.X);
                wavP2.Y = _translation.Y;
                draw_list = ImGui.GetWindowDrawList();
                draw_list.AddTriangle(wavP0, wavP1, wavP2, _reflectedColour );
            }
            foreach (var em in _emitted)
            { 
                wavP0.X =  (float)(_translation.X + em.WaveForm.WavelengthMin_nm * _scalingFactor.X);
                wavP0.Y = _translation.Y;
                
                wavP1.X =  (float)(_translation.X + em.WaveForm.WavelengthAverage_nm * _scalingFactor.X);
                wavP1.Y =  (float)(_translation.Y - em.Magnitude * _scalingFactor.Y);
                
                wavP2.X =  (float)(_translation.X + em.WaveForm.WavelengthMax_nm * _scalingFactor.X);
                wavP2.Y = _translation.Y;
                draw_list = ImGui.GetWindowDrawList();
                draw_list.AddTriangle(wavP0, wavP1, wavP2, _emittedColour );
            }
            

        }
            
    }
}