using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Damage;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using SDL2;


namespace Pulsar4X.SDL2UI.Combat
{
    public class DamageViewer : PulsarGuiWindow
    {
        //private ComponentDesign _componentDesign;
        private Entity _selectedEntity;

        int _newmatIDCode;
        int _newmatHitPoints = 10;
        float _newmatHeat = 1f;
        float _newmatKinetic = 1f;
        private float _newmatDensity = 5000;
        int _newmatAmount = 50;


        private (int x, int y) _firePos = (0, 0);
        private (float x, float y) _fireVel = (0, 300000f);

        private float _projLen = 0.25f;
        private float _projMass = 0.25f;
        private float _projDensity = 4540f;

        private int _selectedComponentIndex = 0;


        private RawBmp _rawComponentImage;
        private IntPtr _componentSDLtexture;

        private int _damageEventIndex = 0;
        private List<RawBmp> _damageFrames;
        private int _showFrameNum = 0;
        private IntPtr _showDmgFrametx;

        private EntityDamageProfileDB _profile;
        private RawBmp _rawShipImage;
        private IntPtr _shipImgPtr;



        private DamageViewer()
        {

            DamageResist polyprop = new DamageResist(100, 255, 1175f);
            DamageResist aluminium = new DamageResist(150, 255, 2700f);
            DamageResist titanium = new DamageResist(200, 255, 4540f);
            DamageResist steelCarbon = new DamageResist(230, 255, 7860);
            DamageResist steelStainless = new DamageResist(255, 255, 7900);


            //_componentDesign.DamageResistance = aluminium;

        }

        public static DamageViewer GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DamageViewer)))
            {
                var dv = new DamageViewer();
                if (_uiState.PrimaryEntity.Entity != null)
                {
                    dv.Init(_uiState.PrimaryEntity.Entity);
                }

                return dv;
            }
            else
            {
                var dv = (DamageViewer)_uiState.LoadedWindows[typeof(DamageViewer)];
                if (_uiState.PrimaryEntity != null && _uiState.PrimaryEntity.Entity != dv._selectedEntity)
                    dv.Init(_uiState.PrimaryEntity.Entity);
                return dv;
            }

        }

        private void Init(Entity damageableEntity)
        {
            var db = damageableEntity.GetDataBlob<EntityDamageProfileDB>();
            if (db != null)
            {
                _selectedEntity = damageableEntity;
                _profile = damageableEntity.GetDataBlob<EntityDamageProfileDB>();
                _rawShipImage = _profile.DamageProfile;
                _shipImgPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _rawShipImage);

                if (_profile.DamageSlides.Count > 0)
                {
                    _damageEventIndex = _profile.DamageSlides.Count - 1;
                    SetDamageEventFrames();
                }

                CanActive = true;
            }
            else
            {
                CanActive = false;
            }
        }

        void SetDamageEventFrames()
        {
            _damageFrames = _profile.DamageSlides[_damageEventIndex];
            _showFrameNum = 0;
            _showDmgFrametx = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _damageFrames[_showFrameNum]);
        }

        static class Beam
        {
            public static double BeamFreq = 700;
            public static string[] BeamTypeNames = new string[]
            {
                "Gama-Ray", 
                "X-Ray", 
                "UV", 
                "Visable", 
                "Near IR", 
                "IR", 
                "MicroWave", 
                "Radio"
            };
            private static int _beamTypeIndex = 5;

            public static int BeamTypeIndex
            {
                get
                {
                    return _beamTypeIndex;
                }
                set
                {
                    _beamTypeIndex = value;
                    SetRange();

                }
            }

            public static double MinFreq; //in meters
            public static double MaxFreq; //in meters

            static void SetRange()
            {
                switch (_beamTypeIndex)
                {
                    case 0: //gama
                        MinFreq = 1e-12; //1pm
                        MaxFreq = 1e-11; //10pm
                        break;
                    case 1: //xray
                        MinFreq = 1e-11; //1pm
                        MaxFreq = 1e-8; //10pm
                        break;                    
                    case 2://uv
                        MinFreq = 1e-8; //10pm
                        MaxFreq = 4e-7; //400nm
                        break;                    
                    case 3://visable
                        MinFreq = 4e-7; //400nm
                        MaxFreq = 7e-7; //700nm
                        break;                    
                    case 4://near IR
                        MinFreq = 7e-7; //700nm
                        MaxFreq = 1e-5; //10um
                        break;
                    case 5:// IR
                        MinFreq = 1e-5; //10um
                        MaxFreq = 0.001; //1mm
                        break;
                    case 6:// MicroWave
                        MinFreq = 0.001; //1mm
                        MaxFreq = 1; //1m
                        break;
                    case 7:// Radio
                        MinFreq = 1; //1m
                        MaxFreq = 100000; //100000m Extremely low freqency
                        break;
                }
            }

        }

        private int _beamTypeIndex = 5;
        private double _momentum = 0;
        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("DamageViewer Testing"))
                {

                    if (_shipImgPtr != IntPtr.Zero)
                    {
                        int w = _rawShipImage.Width; // / 4;
                        int h = _rawShipImage.Height; // / 4;
                        ImGui.Image(_shipImgPtr, new System.Numerics.Vector2(w, h));
                        
                    }
                    
                }
                
                if (_shipImgPtr != IntPtr.Zero)
                {
                    if (ImGui.CollapsingHeader("Fire a Weapon to check damage result"))
                    {
                        ImGui.InputInt("PositionX", ref _firePos.x);
                        ImGui.InputInt("PositionY", ref _firePos.y);

                        ImGui.InputFloat("VelocityX", ref _fireVel.x);
                        ImGui.InputFloat("VelocityY", ref _fireVel.y);
                            

                        ImGui.Columns(2);
                        
                        if (ImGui.Button("Fire Beam"))
                        {
                            var damageFrag = new DamageFragment()
                            {
                                Position = _firePos,
                                Velocity = new Orbital.Vector2(_fireVel.x, _fireVel.y),
                                Mass = _projMass,
                                Density = _projDensity,
                                Length = _projLen,
                                Momentum = (float)_momentum,

                            };
                            _damageFrames = DamageTools.DealDamage(_profile, damageFrag);
                            _rawShipImage = _damageFrames.Last();
                        }
                        ImGui.NextColumn();

                        if(ImGui.Combo("Beam Type", ref _beamTypeIndex, Beam.BeamTypeNames, Beam.BeamTypeNames.Length))
                        {
                            Beam.BeamTypeIndex = _beamTypeIndex;
                        }

                        if (ImGuiExt.SliderDouble("Freqency", ref Beam.BeamFreq, Beam.MinFreq, Beam.MaxFreq))
                        {
                            _momentum = (float)(UniversalConstants.Science.PlankConstant * Beam.BeamFreq);
                        }


                        ImGui.NextColumn();
                        if (ImGui.Button("Fire Longrod Projectile"))
                        {
                            var damageFrag = new DamageFragment()
                            {
                                Position = _firePos,
                                Velocity = new Orbital.Vector2(_fireVel.x, _fireVel.y),
                                Mass = _projMass,
                                Density = _projDensity,
                                Length = _projLen
                            };
                            _damageFrames = DamageTools.DealDamage(_profile, damageFrag);
                            _rawShipImage = _damageFrames.Last();
                        }
                        
                        ImGui.NextColumn();
                        
                        ImGui.InputFloat("Mass", ref _projMass);
                        ImGui.InputFloat("Density", ref _projDensity);
                        ImGui.InputFloat("Length", ref _projLen);
                        
                        
                        ImGui.Columns(0);

                    
                    }
                }

                for (int i = 0; i < _profile.DamageSlides.Count; i++)
                {
                    
                }
                
                if(_profile.DamageSlides.Count > 1)
                {
                    if (ImGui.SliderInt("Damage Events", ref _damageEventIndex, 0, _profile.DamageSlides.Count))
                    {
                        SetDamageEventFrames();
                    }
                }
                
                if(_profile.DamageSlides.Count > 0 && _damageFrames == null)
                {
                    _damageEventIndex = 0;
                    SetDamageEventFrames();

                }
                
                if (_damageFrames != null && _damageFrames.Count > 0)
                {
                    if (ImGui.Button("PrevFrame"))
                    {
                        _showFrameNum--;
                        if (_showFrameNum < 0)
                            _showFrameNum = _damageFrames.Count -1;
                        _showDmgFrametx = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _damageFrames[_showFrameNum]);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("NextFrame"))
                    {
                        _showFrameNum++;
                        if (_showFrameNum > _damageFrames.Count -1)
                            _showFrameNum = 0;
                        _showDmgFrametx = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _damageFrames[_showFrameNum]);
                    }
                    ImGui.Text(_showFrameNum +1  + " of " + _damageFrames.Count);
                    int h = _damageFrames[_showFrameNum].Height;
                    int w = _damageFrames[_showFrameNum].Width;
                    ImGui.Image(_showDmgFrametx, new System.Numerics.Vector2(w, h));
                }


            }

            ImGui.End();
        }

        }
    }
