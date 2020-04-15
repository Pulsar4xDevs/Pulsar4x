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
using Pulsar4X.Vectors;
using SDL2;


namespace Pulsar4X.SDL2UI.Combat
{
    public class DamageViewer : PulsarGuiWindow
    {
        private ComponentDesign _componentDesign;

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
        
        private DamageViewer()
        {
            
            DamageResist polyprop =  new DamageResist(100, 255, 1175f);
            DamageResist aluminium =  new DamageResist(150, 255, 2700f);
            DamageResist titanium =  new DamageResist(200, 255, 4540f );
            DamageResist steelCarbon =  new DamageResist(230, 255, 7860);
            DamageResist steelStainless =  new DamageResist(255, 255, 7900);
            
            
            _componentDesign.DamageResistance = aluminium;

        }

        public static DamageViewer GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DamageViewer)))
            {
                return new DamageViewer();
            }
            return (DamageViewer)_uiState.LoadedWindows[typeof(DamageViewer)];
        }

        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("DamageViewer Testing"))
                {
                    if (ImGui.CollapsingHeader("Create Ship Profile"))
                    {
                        if (ImGui.Button("CreateShip"))
                        {
                            /*

                            ComponentDesign thruster = new ComponentDesign();
                            thruster.ID = ID.NewGuid();
                            thruster.Connections = Connections.Front | Connections.Sides;
                            thruster.Volume_km3 = 10;
                            thruster.AspectRatio = 1;
                            thruster.DamageResistance = DamageTools.DamageResistsLookupTable[200];


                            ComponentDesign fuel = new ComponentDesign();
                            fuel.ID = ID.NewGuid();
                            fuel.Connections = Connections.Front | Connections.Sides | Connections.Back;
                            fuel.Volume_km3 = 100;
                            fuel.AspectRatio = 2f;
                            fuel.DamageResistance = DamageTools.DamageResistsLookupTable[100];

                            ComponentDesign lifeSuport = new ComponentDesign();
                            lifeSuport.ID = ID.NewGuid();
                            lifeSuport.Connections = Connections.Front | Connections.Sides | Connections.Back;
                            lifeSuport.Volume_km3 = 5;
                            lifeSuport.AspectRatio = 1;
                            lifeSuport.DamageResistance = DamageTools.DamageResistsLookupTable[150];


                            ComponentDesign cargo = new ComponentDesign();
                            cargo.ID = ID.NewGuid();
                            cargo.Connections = Connections.Front | Connections.Sides | Connections.Back;
                            cargo.Volume_km3 = 100;
                            cargo.AspectRatio = 0.5f;
                            cargo.DamageResistance = DamageTools.DamageResistsLookupTable[230];

                            List<(ComponentDesign component, int count)> componentTypes = new List<(ComponentDesign component, int count)>();

                            componentTypes.Add((thruster, 2));
                            componentTypes.Add((fuel, 1));
                            componentTypes.Add((lifeSuport, 3));
                            componentTypes.Add((cargo, 1));




                            _profile = ComponentPlacement.CreateShipMap(componentTypes, DamageTools.DamageResistsLookupTable[255]);
                            _rawShipImage = _profile.ShipDamageProfile;
                            _shipImgPtr = CreateSDLTexture(_profile.ShipDamageProfile);
                            _firePos.x = _rawShipImage.Width / 2;
                            */
                        }

                        if (_shipImgPtr != IntPtr.Zero)
                        {
                            int w = _rawShipImage.Width; // / 4;
                            int h = _rawShipImage.Height; // / 4;
                            ImGui.Image(_shipImgPtr, new System.Numerics.Vector2(w, h));


                            ImGui.InputInt("ComponentCluster", ref _selectedComponentIndex);
                            if (_selectedComponentIndex > 0)
                            {
                                if (ImGui.Button("ShiftLeft"))
                                {

                                    (Guid id, int count) item = _profile.PlacementOrder[_selectedComponentIndex];
                                    _profile.PlacementOrder.RemoveAt(_selectedComponentIndex);
                                    _profile.PlacementOrder.Insert(_selectedComponentIndex - 1, item);
                                    _rawShipImage = ComponentPlacement.CreateShipBmp(_profile);
                                    _shipImgPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _rawShipImage);
                                }
                            }

                            if (_selectedComponentIndex < _profile.PlacementOrder.Count - 1)
                            {
                                if (ImGui.Button("ShiftRight"))
                                {
                                    (Guid id, int count) item = _profile.PlacementOrder[_selectedComponentIndex];
                                    _profile.PlacementOrder.RemoveAt(_selectedComponentIndex);
                                    _profile.PlacementOrder.Insert(_selectedComponentIndex + 1, item);
                                    _rawShipImage = ComponentPlacement.CreateShipBmp(_profile);
                                    _shipImgPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _rawShipImage);
                                }
                            }

                        }
                    }
                }
                    
                /*
                if (ImGui.CollapsingHeader("Create Component Profile"))
                {
                    if (ImGui.InputFloat("Volume_km3", ref _componentProfile.Volume_km3))
                    {
                    }

                    if (ImGui.InputFloat("Aspect Ratio", ref _componentProfile.AspectRatio))
                    {
                    }

                    var mat = _componentProfile.Mats;
                    ImGui.Text("ByteCode: " + mat.IDCode);
                    ImGui.Text("HitPoints " + mat.HitPoints);
                    ImGui.Text("Density" + mat.Density);
                    //ImGui.Text("Heat: " + mat.Heat);
                    //ImGui.Text("Kinetic: " + mat.Kinetic);

                        

                    ImGui.InputInt("ByteCode: ", ref _newmatIDCode);
                    ImGui.InputInt("HitPoints ", ref _newmatHitPoints);
                    //ImGui.InputFloat("Heat: ", ref _newmatHeat);
                    //ImGui.InputFloat("Kinetic: ", ref _newmatKinetic);
                    ImGui.InputFloat("Density", ref _newmatDensity);

                    if (ImGui.Button("Replace Mat"))
                    {
                        _componentProfile.Mats = new DamageResist((byte)_newmatIDCode, _newmatHitPoints, _newmatDensity);


                    }
                
                    if (ImGui.Button("Create Bitmap"))
                    {
                        _rawComponentImage = DamageTools.CreateComponentByteArray(_componentProfile);
                        _componentSDLtexture = CreateSDLTexture(_rawComponentImage);
                    }
                }*/ 
                if (_shipImgPtr != IntPtr.Zero)
                {
                    if (ImGui.CollapsingHeader("Weapons"))
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
                                Velocity = new Vector2(_fireVel.x, _fireVel.y),
                                Mass = _projMass,
                                Density = _projDensity,
                                Length = _projLen
                            };
                            //_damageFrames = DamageTools.DealDamage(_rawShipImage, damageFrag);
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
