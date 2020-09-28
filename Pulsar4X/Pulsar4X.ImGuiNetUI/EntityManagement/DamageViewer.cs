using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
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
        
        private List<RawBmp> _damageFrames;
        private int _showFrameNum = 0;
        private IntPtr _showDmgFrametx;

        private EntityDamageProfileDB _profile;
        private RawBmp _rawShipImage;
        private IntPtr _shipImgPtr;

        private int _selectedDamageEvent = 0;
        
        private DamageViewer()
        {
            
            DamageResist polyprop =  new DamageResist(100, 255, 1175f);
            DamageResist aluminium =  new DamageResist(150, 255, 2700f);
            DamageResist titanium =  new DamageResist(200, 255, 4540f );
            DamageResist steelCarbon =  new DamageResist(230, 255, 7860);
            DamageResist steelStainless =  new DamageResist(255, 255, 7900);

            
            //_componentDesign.DamageResistance = aluminium;

        }

        public static DamageViewer GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DamageViewer)))
            {
                var dv = new DamageViewer();
                if(_uiState.PrimaryEntity.Entity != null )
                {
                    dv.Init(_uiState.PrimaryEntity.Entity);
                }
                return dv;
            }
            else
            {
                var dv =(DamageViewer)_uiState.LoadedWindows[typeof(DamageViewer)];
                if(_uiState.PrimaryEntity != null && _uiState.PrimaryEntity.Entity != dv._selectedEntity)
                    dv.Init(_uiState.PrimaryEntity.Entity);
                return dv;
            }
            
        }

        private void Init(Entity damageableEntity)
        {
            var db = damageableEntity.GetDataBlob<EntityDamageProfileDB>();
            if(db != null)
            {
                _selectedEntity = damageableEntity;
                _profile = damageableEntity.GetDataBlob<EntityDamageProfileDB>();
                _rawShipImage = _profile.DamageProfile;
                _shipImgPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _rawShipImage);

                if (_profile.DamageSlides.Count > 0)
                {
                    _selectedDamageEvent = _profile.DamageSlides.Count - 1;


                    var _damageFrames = _profile.DamageSlides[_selectedDamageEvent];
                    foreach (RawBmp slide in _damageFrames)
                    {
                        
                    }
                    
                    
                }
                
                CanActive = true;
            }
            else
            {
                CanActive = false;
            }
        }

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
                            
                        ImGui.InputFloat("Mass", ref _projMass);
                        ImGui.InputFloat("Density", ref _projDensity);
                        ImGui.InputFloat("Length", ref _projLen);
                        
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
                    
                    }
                }

                if (_damageFrames != null && _damageFrames.Count > 0)
                {
                    if (ImGui.Button("NextFrame"))
                    {
                        _showFrameNum++;
                        if (_showFrameNum > _damageFrames.Count -1)
                            _showFrameNum = 0;
                        _showDmgFrametx = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _damageFrames[_showFrameNum]);
                    }
                    int h = _damageFrames[_showFrameNum].Height;
                    int w = _damageFrames[_showFrameNum].Width;
                    ImGui.Image(_showDmgFrametx, new System.Numerics.Vector2(w, h));
                }


            }

            ImGui.End();
        }

        }
    }
