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
        private SystemState? _selectedStarSysState;

        private SensorAbilityDB _sensorAbilityDB;

        
        #region targetVariables
        private Entity[]? _potentialTargetEntities;
        private string[]? _potentialTargetNames;
        private int _targetIndex = -1;

        private SensorProfileDB? _emitSensorProfile;
        private SensorReturnValues[]? _targetDetectionQuality;

        List<EMData> _emitted = new ();
        private (int type, int index) _highlightIndex = (-1, -1);
        List<EMData> _reflected = new ();
        double _distance = 1.0;
        double _attenuationFactor = 1.0;
        
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
        uint _highlightColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 1.0f, 0.3f));
        uint _receverColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 1.0f));
        
        private System.Numerics.Vector2 _canvasPos;
        private System.Numerics.Vector2 _canvasSize;
        private System.Numerics.Vector2 _canvasEndPos;
        private System.Numerics.Vector2 _translation;
        private System.Numerics.Vector2 _scalingFactor;
        private System.Numerics.Vector2 wavP0;
        private System.Numerics.Vector2 wavP1;
        private System.Numerics.Vector2 wavP2;

        private bool _logScale = true;
        private bool _atRange = true;
        #endregion
        
        
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
            
            if (instance._emitSensorProfile == null || instance._emitSensorProfile.OwningEntity != instance._selectedEntity)
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

            if (Window.Begin("Sensor Display: " + _selectedEntitySate.Name, ref IsActive))
            {
                
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);
                if (_potentialTargetNames != null
                    && _potentialTargetEntities != null
                    && ImGui.Combo("Sensors", ref _targetIndex, _potentialTargetNames, _potentialTargetNames.Length))
                {
                    _sensorAbilityDB = _potentialTargetEntities[_targetIndex].GetDataBlob<SensorAbilityDB>();
                    SetBoundsData();
                }
                if (ImGui.Button("Refresh ReflectionProfile"))
                {
                    SetBoundsData();
                }
                
                
                if (_sensorAbilityDB != null)
                {
                    if (ImGui.Button("Scan"))
                    {
                        var ss = new SensorScan();
                        ss.TriggerProcess(_selectedEntity, _selectedStarSysState.StarSystem.StarSysDateTime);
                    }
                }

                if (ImGui.Checkbox("LogScale", ref _logScale))
                {
                    SetBoundsData();
                }

                if (_sensorAbilityDB != null)
                {
                    BorderGroup.Begin("Sensors");
                    for (int i = 0; i < _sensorAbilityDB.InstanceAtributes.Count; i++)
                    {
                        SensorReceiverAtb? recever = _sensorAbilityDB.InstanceAtributes[i];
                        float low = (float)recever.RecevingWaveformCapabilty.WavelengthMin_nm;
                        float mid = (float)recever.RecevingWaveformCapabilty.WavelengthAverage_nm;
                        float high = (float)recever.RecevingWaveformCapabilty.WavelengthMax_nm;

                        float mag1 = (float)recever.WorstSensitivity_kW;
                        float mag2 = (float)recever.BestSensitivity_kW;
                        ImGui.Text(_sensorAbilityDB.InstanceStates[i].Name);
                        if (ImGui.IsItemHovered())
                        {
                            _highlightIndex = (0, i);
                        }
                        ImGui.Indent();
                        ImGui.Text(Stringify.Power(recever.BestSensitivity_kW));
                        ImGui.Unindent();
                    }
                    BorderGroup.End();
                }
                
                if (_emitSensorProfile != null)
                {
                    var emitted = _emitSensorProfile.EmittedEMSpectra;
                    var reflected = _emitSensorProfile.ReflectedEMSpectra;
                    if (ImGui.Checkbox("At Range", ref _atRange))
                    {
                        SetBoundsData();
                    }
                    ImGui.Text("Range: " + Stringify.Distance(_distance));

                    BorderGroup.Begin("Reflected");
                    for (int i = 0; i < reflected.Count; i++)
                    {
                        EMData emdat = reflected[i];
                        ImGui.Text(emdat.GetName);
                        if (ImGui.IsItemHovered())
                        {
                            _highlightIndex = (1, i);
                        }
                        ImGui.Indent();
                        ImGui.Text(Stringify.Power(emdat.Magnitude * _attenuationFactor));
                        ImGui.Unindent();
                    }

                    BorderGroup.End();
                    
                    BorderGroup.Begin("Emitted");
                    for (int i = 0; i < emitted.Count; i++)
                    {
                        EMData em = emitted[i];
                        ImGui.Text(em.GetName);
                        if (ImGui.IsItemHovered())
                        {
                            _highlightIndex = (2, i);
                        }
                        ImGui.Indent();
                        ImGui.Text(Stringify.Power(em.Magnitude * _attenuationFactor));
                        ImGui.Unindent();
                        
                    }
                    BorderGroup.End();
                }
                
                ImGui.NextColumn();
                if(_emitSensorProfile != null)
                    Draw();
                _highlightIndex = (-1, -1);
                
                Window.End();
            }
        }

        void Setup()
        {
            if(_selectedEntity == null)
                return;
            if (_selectedEntity.TryGetDataBlob<SensorProfileDB>(out var sensorData))
            {
                _emitSensorProfile = sensorData;
                SetBoundsData();
            }
            
            //gather potential targets data
            var tgts = _selectedStarSysState.StarSystem.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
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

        void SetBoundsData()
        {
            if (_atRange &&_sensorAbilityDB != null)
            {
                _distance = MoveMath.GetDistanceBetween(_sensorAbilityDB.OwningEntity, _emitSensorProfile.OwningEntity);
                _attenuationFactor = SensorTools.AttenuationFactor(_distance);
            }
            else
            {
                _distance = 0;
                _attenuationFactor = 1.0;
            }
            
            
            _lowestWave = double.MaxValue;
            _highestWave = double.MinValue;
            _lowestMagnitude = double.MaxValue;
            _highestMagnitude = double.MinValue;
            
            if(_emitSensorProfile != null)
            {
                SensorProfileTools.UpdateReflectionProfile(_emitSensorProfile, _uiState.PrimarySystemDateTime);
                _emitted = _emitSensorProfile.EmittedEMSpectra;
                _reflected = _emitSensorProfile.ReflectedEMSpectra;
                foreach (var emitter in _emitted.Concat(_reflected))
                {
                    _lowestWave = Math.Min(_lowestWave, emitter.WaveForm.WavelengthMin_nm);
                    _highestWave = Math.Max(_highestWave, emitter.WaveForm.WavelengthMax_nm);
                    _lowestMagnitude = Math.Min(_lowestMagnitude, emitter.Magnitude * _attenuationFactor);
                    _highestMagnitude = Math.Max(_highestMagnitude, emitter.Magnitude * _attenuationFactor);
                }
            }

            if (_sensorAbilityDB != null)
            {
                foreach (var atb in _sensorAbilityDB.InstanceAtributes)
                {
                    _lowestWave = Math.Min(_lowestWave, atb.RecevingWaveformCapabilty.WavelengthMin_nm);
                    _highestWave = Math.Max(_highestWave, atb.RecevingWaveformCapabilty.WavelengthMax_nm);
                    _lowestMagnitude = Math.Min(_lowestMagnitude, atb.BestSensitivity_kW);
                    //_highestMagnitude = Math.Max(_highestMagnitude, atb.WorstSensitivity_kW);
                }
                
            }

            if (_logScale)
            {
                _lowestMagnitude = Math.Log10(_lowestMagnitude);
                _highestMagnitude = Math.Log10(_highestMagnitude);
            }
        }

        
        void Draw()
        {
            var draw_list = ImGui.GetWindowDrawList();
            // ImDrawList API uses screen coordinates!
            _canvasPos = ImGui.GetCursorScreenPos();
            _canvasSize = ImGui.GetContentRegionAvail();
            _canvasEndPos = _canvasPos + _canvasSize;
            
            //calculate X scale for canvas size.
            _scalingFactor.X = _canvasSize.X / (float)(_highestWave - _lowestWave);
            
            _translation.X = (float)(_canvasPos.X - _lowestWave * _scalingFactor.X);
            
            //calculate Y scale for canvas size (75%)
            _scalingFactor.Y = (float)(_canvasSize.Y * 0.75f / (_highestMagnitude - Math.Min(0, _lowestMagnitude)));
            //translate Y to proper screen position, and for the lowest point we want to draw.
            _translation.Y = (float)(_canvasEndPos.Y);
            
            
            double midVal = 1;
            
            //draw canvas boarder.
            draw_list.AddRect(_canvasPos, _canvasEndPos, _canvasBorderColour);

            for (int i = 0; i < _reflected.Count; i++)
            {
                EMData em = _reflected[i];
                if (_logScale)
                    midVal = (Math.Log10(em.Magnitude * _attenuationFactor) - _lowestMagnitude) * _scalingFactor.Y;
                else
                    midVal = em.Magnitude * _attenuationFactor * _scalingFactor.Y;

                wavP0.X = (float)(_translation.X + em.WaveForm.WavelengthMin_nm * _scalingFactor.X);
                wavP0.Y = _translation.Y;

                wavP1.X = (float)(_translation.X + em.WaveForm.WavelengthAverage_nm * _scalingFactor.X);
                wavP1.Y = (float)(_translation.Y - midVal);

                wavP2.X = (float)(_translation.X + em.WaveForm.WavelengthMax_nm * _scalingFactor.X);
                wavP2.Y = _translation.Y;

                draw_list.AddTriangle(wavP0, wavP1, wavP2, _reflectedColour);
                if(_highlightIndex == (1, i))
                    draw_list.AddTriangleFilled(wavP0, wavP1, wavP2, _highlightColour);
            }

            for (int i = 0; i < _emitted.Count; i++)
            {
                EMData em = _emitted[i];
                if (_logScale)
                    midVal = (Math.Log10(em.Magnitude * _attenuationFactor) - _lowestMagnitude) * _scalingFactor.Y;
                else
                    midVal = em.Magnitude * _attenuationFactor * _scalingFactor.Y;

                wavP0.X = (float)(_translation.X + em.WaveForm.WavelengthMin_nm * _scalingFactor.X);
                wavP0.Y = _translation.Y;

                wavP1.X = (float)(_translation.X + em.WaveForm.WavelengthAverage_nm * _scalingFactor.X);
                wavP1.Y = (float)(_translation.Y - midVal);

                wavP2.X = (float)(_translation.X + em.WaveForm.WavelengthMax_nm * _scalingFactor.X);
                wavP2.Y = _translation.Y;
                
                
                draw_list.AddTriangle(wavP0, wavP1, wavP2, _emittedColour);
                if(_highlightIndex == (2, i))
                    draw_list.AddTriangleFilled(wavP0, wavP1, wavP2, _highlightColour);
            }

            if(_sensorAbilityDB != null)
            {
                for (int i = 0; i < _sensorAbilityDB.InstanceAtributes.Count; i++)
                {
                    SensorReceiverAtb? em = _sensorAbilityDB.InstanceAtributes[i];
                    if (_logScale)
                        midVal = (Math.Log10(em.BestSensitivity_kW) - _lowestMagnitude) * _scalingFactor.Y;
                    else
                        midVal = em.BestSensitivity_kW * _scalingFactor.Y;

                    wavP0.X = (float)(_translation.X + em.RecevingWaveformCapabilty.WavelengthMin_nm * _scalingFactor.X);
                    wavP0.Y = _canvasPos.Y;

                    wavP1.X = (float)(_translation.X + em.RecevingWaveformCapabilty.WavelengthAverage_nm * _scalingFactor.X);
                    wavP1.Y = (float)(_translation.Y - midVal);

                    wavP2.X = (float)(_translation.X + em.RecevingWaveformCapabilty.WavelengthMax_nm * _scalingFactor.X);
                    wavP2.Y = _canvasPos.Y;
                    draw_list.AddTriangle(wavP0, wavP1, wavP2, _receverColour);
                    if(_highlightIndex == (0, i))
                        draw_list.AddTriangleFilled(wavP0, wavP1, wavP2, _highlightColour);
                }
            }
            
        }
    }
}