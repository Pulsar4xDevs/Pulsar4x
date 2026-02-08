using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Damage;
using ImGuiNET;
using Pulsar4X.Client;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.DataStructures;
using Pulsar4X.Orbital;
using Pulsar4X.Factions;
using Pulsar4X.Damage;
using Pulsar4X.Datablobs;
using Pulsar4X.Weapons;
using Pulsar4X.Galaxy;
using Pulsar4X.Ships;
using SDL3;

namespace Pulsar4X.Client.Combat
{
    public class DamageViewerWindow : PulsarGuiWindow
    {
        //private ComponentDesign _componentDesign;
        private Entity? _selectedEntity;

        int _newmatIDCode;
        int _newmatHitPoints = 10;
        float _newmatHeat = 1f;
        float _newmatKinetic = 1f;
        private float _newmatDensity = 5000;
        int _newmatAmount = 50;


        private float _projLen = 0.25f;
        private float _projMass = 0.25f;
        private float _projDensity = 4540f;

        private int _selectedComponentIndex = 0;


        private RawBmp _rawComponentImage;
        private IntPtr _componentSDLtexture;

        private int _damageEventIndex = 0;
        private List<RawBmp>? _damageFrames = null;
        private int _showFrameNum = 0;
        private IntPtr _showDmgFrametx;
        private EntityDamageProfileDB? _profile;
        private RawBmp _rawShipImage;
        private IntPtr _shipImgPtr;

        DamageMap _damageMap;
        IntPtr[] _damageMapPtr = new IntPtr[8];
        private IntPtr _hiResPtr = new IntPtr();
        private int _hiResSize = 128;
        DamageMap _projectileDamageMap;
        IntPtr _projectileDMapPtr;

        ComponentInstancesDB _componentInstances;

        private DamageViewerWindow()
        {

            DamageResistBlueprint polyprop = new DamageResistBlueprint(100, 255, 1175f);
            DamageResistBlueprint aluminium = new DamageResistBlueprint(150, 255, 2700f);
            DamageResistBlueprint titanium = new DamageResistBlueprint(200, 255, 4540f);
            DamageResistBlueprint steelCarbon = new DamageResistBlueprint(230, 255, 7860);
            DamageResistBlueprint steelStainless = new DamageResistBlueprint(255, 255, 7900);


            //_componentDesign.DamageResistance = aluminium;

        }

        public static DamageViewerWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DamageViewerWindow)))
            {
                var dv = new DamageViewerWindow();
                if (_uiState.LastClickedEntity?.Entity != null)
                {
                    dv.Init(_uiState.LastClickedEntity.Entity);
                }

                return dv;
            }
            else
            {
                var dv = (DamageViewerWindow)_uiState.LoadedWindows[typeof(DamageViewerWindow)];
                if (_uiState.PrimaryEntity != null && _uiState.LastClickedEntity.Entity != dv._selectedEntity)
                    dv.Init(_uiState.LastClickedEntity.Entity);
                return dv;
            }

        }

        private void Init(Entity damageableEntity)
        {
            _selectedEntity = damageableEntity;

            if(damageableEntity.TryGetDataBlob<ShipInfoDB>(out var shdb))
            {
                var design = shdb.Design;
                _damageMap = new DamageMap(damageableEntity, design);
            }
            if(damageableEntity.TryGetDataBlob<SystemBodyInfoDB>(out var sbdb))
            {
                _damageMap = new DamageMap(damageableEntity, sbdb);
            }
            DamageMapRendering.CreateSDLTextures(_uiState.ViewPort.Renderer, _damageMap, ref _damageMapPtr);

            _dmWidth = (int)(_damageMap.Width * _dmSizeScaler);
            _dmHeight = (int)(_damageMap.Height * _dmSizeScaler);
            _dmProjectileSliderBot = _dmWidth;
            _dmProjectileSliderLhs = (int)(_dmHeight * 0.75);
            _dmProjectileSliderTop = 0;
            _dmProjectileSliderRhs = (int)(_dmHeight * 0.25);
            if(damageableEntity.TryGetDataBlob<EntityDamageProfileDB>(out var _profile))
            {
                _rawShipImage = _profile.DamageProfile;
                if (_profile.DamageEvents.Count > 0)
                {
                    _damageEventIndex = _profile.DamageEvents.Count - 1;
                    SetDamageEventFrames();
                }
                Textures.CreateTexture(_uiState.ViewPort.Renderer, _rawShipImage, ref _shipImgPtr, SDL.PixelFormat.ARGB8888);
            }
            if(damageableEntity.TryGetDataBlob<ComponentInstancesDB>(out var _componentInstances))
            {}
            CanActive = true;
            /*
            else
            {
                CanActive = false;
            }*/
        }

        void SetDamageEventFrames()
        {
            if (_profile == null) return;

            _damageFrames = DamageTools.DealDamageSim(_profile, _profile.DamageEvents[_damageEventIndex]).damageFrames;
            _showFrameNum = 0;
            if (_damageFrames != null)
                Textures.CreateTexture(_uiState.ViewPort.Renderer, _damageFrames[_showFrameNum], ref _showDmgFrametx, SDL.PixelFormat.ARGB8888);
        }


        static class ExsistingWeapons
        {
            private static FactionInfoDB? _factionInfoDB;
            private static List<ComponentDesign> _allShipComponents;
            public static int SelectedWeaponIndex = 0;
            public static List<ComponentDesign> AvailableShipComponents;
            public static string[] WeaponNames;

            public static ComponentDesign SelectedWeapon
            {
                get { return AvailableShipComponents[SelectedWeaponIndex]; }
            }

            public static void Create(Entity faction)
            {
                if (_factionInfoDB is null)
                {
                    _factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
                    RefreshComponentDesigns();
                }
            }

            static void RefreshComponentDesigns()
            {
                _allShipComponents = _factionInfoDB.ComponentDesigns.Values.ToList();
                _allShipComponents.Sort((a, b) => a.Name.CompareTo(b.Name));

                var templatesByGroup = _allShipComponents.GroupBy(t => t.ComponentType);
                var groupNames = templatesByGroup.Select(g => g.Key).ToList();
                //var sortedTempGroupNames = groupNames.OrderBy(name => name).ToArray();
                //_sortedComponentNames = new string[sortedTempGroupNames.Length + 1];
                //_sortedComponentNames[0] = "All";
                //Array.Copy(sortedTempGroupNames, 0, _sortedComponentNames, 1, sortedTempGroupNames.Length);

                AvailableShipComponents = _allShipComponents.Where(t => t.ComponentType.Equals("Weapon")).ToList();
                WeaponNames = new string[AvailableShipComponents.Count];
                for (int index = 0; index < AvailableShipComponents.Count; index++)
                {
                    ComponentDesign component = AvailableShipComponents[index];
                    WeaponNames[index] = component.Name;
                }
            }
        }

        //private int _beamTypeIndex = 5;
        private double _momentum = 0;
        DamageFragment _damageFrag;
        private bool _typeIsBeam = true;
        private int _dmProjectileSpeed = 5000;
        private int _dmProjectileSliderTop = 0;
        private int _dmProjectileSliderBot = 0;
        private int _dmProjectileSliderLhs = 0;
        private int _dmProjectileSliderRhs = 0;
        private float _dmSizeScaler = 1.0f;
        private int _dmWidth;
        private int _dmHeight;

        private System.Numerics.Vector2 _ImageStart = new(0, 0);
        private bool _showCompIDMap = false;
        private bool _showVMap = false;
        private bool _showPresMap = false;
        private bool _showPMap = false;
        private bool _showTemp = false;
        private bool _showPState = false;
        private bool _showPhMap = false;
        private bool _showCompisite = true;
        private bool _runSimLoop = false;
        private int _projectileTypes;
        private int _beamRange = 10000;
        private float _beamlifetime = 30;
        private int _beamEnergy = 10000;
        private int _beamFreq = 700;
        internal override void Display()
        {
            if (IsActive)
            {
                if (Window.Begin("DamageViewer Testing") && _damageMap != null)
                {
                    var availableSize = ImGui.GetContentRegionAvail();
                    float aspectRatio = (float)_damageMap.Width / _damageMap.Height;
                    float scaleX = availableSize.X / _damageMap.Width;
                    float scaleY = availableSize.Y / _damageMap.Height;
                    _dmSizeScaler = Math.Min(scaleX, scaleY);
                    _dmWidth = (int)(_damageMap.Width * _dmSizeScaler);
                    _dmHeight = (int)(_damageMap.Height * _dmSizeScaler);
                    _dmProjectileSliderBot = _dmWidth;
                    _dmProjectileSliderLhs = (int)(_dmHeight * 0.75);
                    _dmProjectileSliderTop = 0;
                    _dmProjectileSliderRhs = (int)(_dmHeight * 0.25);



                    if (_shipImgPtr != IntPtr.Zero && ImGui.CollapsingHeader("Old Damage View"))
                    {
                        int w = _rawShipImage.Width; // / 4;
                        int h = _rawShipImage.Height; // / 4;
                        ImGui.Image(_shipImgPtr.ToTextureRef(), new System.Numerics.Vector2(w, h));

                    }


                    if (_damageMapPtr[0] != IntPtr.Zero&& ImGui.CollapsingHeader("New Damage Map"))
                    {
                        var vsliderSize = new System.Numerics.Vector2(18, _dmHeight);
                        //var hsliderSize = new System.Numerics.Vector2(18, w);
                        //ImGuiSliderFlags.
                        ImGui.SetNextItemWidth(_dmWidth);
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + vsliderSize.X + 8);
                        if (ImGui.SliderInt("###top", ref _dmProjectileSliderTop, 0, _dmWidth))
                        {
                            if (_dmProjectileSliderLhs > _dmHeight * 0.5)
                                _dmProjectileSliderLhs = _dmHeight;
                            else
                                _dmProjectileSliderLhs = 0;
                        }

                        if (ImGui.VSliderInt("###lhs", vsliderSize, ref _dmProjectileSliderLhs, _dmHeight, 0))
                        {
                            if(_dmProjectileSliderTop > _dmWidth * 0.5)
                                _dmProjectileSliderTop = _dmWidth;
                            else
                                _dmProjectileSliderTop = 0;
                        }
                        ImGui.SameLine();
                        _ImageStart = ImGui.GetCursorScreenPos();
                        var cpos = ImGui.GetCursorPos();
                        if(_showCompIDMap)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[0].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }
                        if(_showPresMap)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[1].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }
                        if(_showVMap)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[2].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }
                        if(_showPMap)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[3].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }
                        if(_showTemp)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[4].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }
                        if(_showPState)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[5].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }
                        if(_showPhMap && _damageMapPtr[6] != IntPtr.Zero)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[6].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }

                        if (_showCompisite && _damageMapPtr[7] != IntPtr.Zero)
                        {
                            ImGui.SetCursorPos(cpos);
                            ImGui.Image(_damageMapPtr[7].ToTextureRef(), new System.Numerics.Vector2(_dmWidth, _dmHeight));
                        }



                        ImGui.SameLine();
                        if (ImGui.VSliderInt("###rhs", vsliderSize, ref _dmProjectileSliderRhs, _dmHeight, 0))
                        {
                            if(_dmProjectileSliderBot > _dmWidth * 0.5)
                                _dmProjectileSliderBot = _dmWidth;
                            else
                                _dmProjectileSliderBot = 0;
                        }
                        ImGui.SetNextItemWidth(_dmWidth);
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + vsliderSize.X + 8);
                        if (ImGui.SliderInt("###bot", ref _dmProjectileSliderBot, 0, _dmWidth))
                        {
                            if (_dmProjectileSliderRhs > _dmHeight * 0.5)
                                _dmProjectileSliderRhs = _dmHeight;
                            else
                                _dmProjectileSliderRhs = 0;
                        }

                        ImGui.Checkbox("comIDMap", ref _showCompIDMap);
                        ImGui.SameLine();
                        ImGui.Checkbox("PresMap", ref _showPresMap);
                        ImGui.SameLine();
                        ImGui.Checkbox("Vmap", ref _showVMap);
                        ImGui.SameLine();
                        ImGui.Checkbox("Pmap", ref _showPMap);
                        ImGui.SameLine();
                        ImGui.Checkbox("TempMap", ref _showTemp);
                        ImGui.SameLine();
                        ImGui.Checkbox("PhaseStateMap", ref _showPState);
                        ImGui.SameLine();
                        ImGui.Checkbox("PhotonMap", ref _showPhMap);
                        ImGui.SameLine();
                        ImGui.Checkbox("compisite", ref _showCompisite);
                    }

                    if (_hiResPtr != IntPtr.Zero)
                    {
                        ImGui.Image(_hiResPtr.ToTextureRef(), new System.Numerics.Vector2(_hiResSize, _hiResSize));
                    }

                    if (_shipImgPtr != IntPtr.Zero)
                    {
                        if (ImGui.CollapsingHeader("Fire a Weapon to check damage result"))
                        {
                            var drawList = ImGui.GetWindowDrawList();
                            System.Numerics.Vector2 offsetStart = new System.Numerics.Vector2(_dmProjectileSliderTop, _dmProjectileSliderLhs);
                            System.Numerics.Vector2 offsetEnd = new System.Numerics.Vector2(_dmProjectileSliderBot, _dmProjectileSliderRhs);
                            System.Numerics.Vector2 lineStart = _ImageStart + offsetStart; // Adjust these offsets as needed
                            System.Numerics.Vector2 lineEnd = _ImageStart + offsetEnd;  // Adjust these offsets as needed
                            var color = ImGui.GetColorU32(0xFFFFFFFF);
                            // Draw the line relative to the image position
                            drawList.AddLine(lineStart, lineEnd, color, 1f);


                            ImGui.Columns(2);


                            //type of damage to test
                            /*
                            ExsistingWeapons.Create(_selectedEntity.GetFactionOwner);
                            if (ImGui.Combo("Exsisting Design", ref ExsistingWeapons.SelectedWeaponIndex, ExsistingWeapons.WeaponNames, ExsistingWeapons.AvailableShipComponents.Count))
                            {

                                //ExsistingWeapons.SelectedWeapon.
                            }*/

                            if (ImGui.RadioButton("Particle", ref _projectileTypes, 0))
                            {

                            }

                            if (ImGui.RadioButton("laser", ref _projectileTypes, 1))
                            {

                            }


                            ImGui.NextColumn();

                            //tweaks to damage type
                            if (_projectileTypes == 0)
                            {
                                //ImGui.InputFloat("Mass", ref _projMass);
                                //ImGui.InputFloat("Density", ref _projDensity);
                                //ImGui.InputFloat("Length", ref _projLen);
                                if (ImGui.SliderInt("Speed", ref _dmProjectileSpeed, 100, 100000))
                                {

                                }

                            }
                            else
                            {
                                if (ImGui.SliderInt("Energy", ref _beamEnergy, 1000, 100000))
                                {

                                }
                                if (ImGui.SliderInt("Freqency", ref _beamFreq, 500, 10000))
                                {
                                    //_momentum = (float)(UniversalConstants.Science.PlankConstant * Beam.BeamFreq);
                                }
                                if(ImGui.SliderInt("Range Km", ref _beamRange, 1, 100000))
                                {}
                                if(ImGui.SliderFloat("Length in seconds", ref _beamlifetime, 1, 60))
                                {}
                            }

                            ImGui.Columns(1);





                            if (ImGui.Button("Fire"))
                            {
                                SetDMVectors();

                                _damageMap.MergeAndResize(_projectileDamageMap);
                                DamageMapRendering.CreateSDLTextures(_uiState.ViewPort.Renderer, _damageMap, ref _damageMapPtr);
                                var fpart = KineticMath.GetFastestPart(_damageMap);
                                DamageMapRendering.CreateTextureForFastestParticleRegion(_uiState.ViewPort.Renderer, _damageMap, fpart, ref _hiResPtr, _hiResSize);



                                /*
                                _damageFrag = new DamageFragment()
                                {
                                    Position = _firePos,
                                    Velocity = new Orbital.Vector2(_fireVel.x, _fireVel.y),
                                    Mass = _projMass,
                                    Density = _projDensity,
                                    Length = _projLen
                                };
                                _damageFrames = DamageTools.DealDamageSim(_profile, _damageFrag).damageFrames;
                                _rawShipImage = _damageFrames.Last();*/
                            }

                            if (ImGui.Button("RunSimLoop"))
                            {
                                _runSimLoop =! _runSimLoop;
                            }
                            if (ImGui.Button("StepSim"))
                            {
                                _runSimLoop = false;
                                DamagePhysicsSim.PhysicsLoop(_damageMap);
                                DamageMapRendering.CreateSDLTextures(_uiState.ViewPort.Renderer, _damageMap, ref _damageMapPtr);
                                var fpart = KineticMath.GetFastestPart(_damageMap);
                                DamageMapRendering.CreateTextureForFastestParticleRegion(_uiState.ViewPort.Renderer, _damageMap, fpart, ref _hiResPtr, _hiResSize);
                            }

                            if (_runSimLoop)
                            {
                                DamagePhysicsSim.PhysicsLoop(_damageMap);
                                DamageMapRendering.CreateSDLTextures(_uiState.ViewPort.Renderer, _damageMap, ref _damageMapPtr);
                                var fpart = KineticMath.GetFastestPart(_damageMap);
                                DamageMapRendering.CreateTextureForFastestParticleRegion(_uiState.ViewPort.Renderer, _damageMap, fpart, ref _hiResPtr, _hiResSize);
                            }
                            ImGui.Text(Stringify.Energy(_damageMap.TotalEnergy));
                            ImGui.Text(_damageMap.RunTime.ToString());
                            if(_componentInstances != null)
                            {
                                if(ImGui.Button("updateComponetnts"))
                                    DamagePhysicsSim.UpdateComponetHealth(_damageMap, _componentInstances);

                                foreach (var kvp in _componentInstances.ComponentsByDesign)
                                {
                                    foreach (var component in kvp.Value)
                                    {
                                        ImGui.Text(component.Name);
                                        ImGui.SameLine();
                                        ImGui.Text((component.HealthPercent * 100).ToString());
                                    }
                                }
                            }
                        }
                    }

                    if (_hiResPtr != IntPtr.Zero)
                    {




                    }

                    if (_profile != null && _profile.DamageEvents.Count > 0)
                    {
                        if (ImGui.SliderInt("Damage Events", ref _damageEventIndex, 1, _profile.DamageEvents.Count - 1))
                        {
                            SetDamageEventFrames();
                        }
                    }

                    if (_profile != null && _profile.DamageEvents.Count > 0 && _damageFrames == null)
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
                                _showFrameNum = _damageFrames.Count - 1;
                            Textures.CreateTexture(_uiState.ViewPort.Renderer, _damageFrames[_showFrameNum], ref _showDmgFrametx, SDL.PixelFormat.ARGB8888);
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("NextFrame"))
                        {
                            _showFrameNum++;
                            if (_showFrameNum > _damageFrames.Count - 1)
                                _showFrameNum = 0;
                            Textures.CreateTexture(_uiState.ViewPort.Renderer, _damageFrames[_showFrameNum], ref _showDmgFrametx, SDL.PixelFormat.ARGB8888);
                        }

                        ImGui.Text(_showFrameNum + 1 + " of " + _damageFrames.Count);
                        int h = _damageFrames[_showFrameNum].Height;
                        int w = _damageFrames[_showFrameNum].Width;
                        ImGui.Image(_showDmgFrametx.ToTextureRef(), new System.Numerics.Vector2(w, h));
                    }
                }
                Window.End();
            }
        }

        void SetDMVectors()
        {
            System.Numerics.Vector2 size = new (3,3);
            System.Numerics.Vector2 dmProjStart = new(_dmProjectileSliderTop, _dmProjectileSliderLhs);
            dmProjStart /= _dmSizeScaler;
            dmProjStart -= size;
            System.Numerics.Vector2 _dmProjEnd = new(_dmProjectileSliderBot, _dmProjectileSliderRhs);
            _dmProjEnd /= _dmSizeScaler;
            System.Numerics.Vector2 velocity = System.Numerics.Vector2.Normalize(_dmProjEnd - dmProjStart);
            if(_projectileTypes == 0)
            {
                velocity *= _dmProjectileSpeed;
                ParticleMaterial dmMat = new ParticleMaterial()
                {
                    TensileStrength = 110,
                    Elasticity = 0.5f,
                    ThermalCapacity = 900,
                    ThermalConductivity = 237,
                    MeltingZeroPoint = 933.47f,
                    TriplePoint = new PhasePoint(0.00001f, 933.47f),
                    CriticalPoint = new PhasePoint(1150, 7500),
                    Density = 7874
                };
                _projectileDamageMap = new DamageMap((int)dmProjStart.X, (int)dmProjStart.Y, velocity, (int)size.X, (int)size.Y, dmMat);
            }
            else if (_projectileTypes == 1)
            {
                velocity *= 2.998e+8f; //speed of light
                Vector3 vel3 = new Vector3(velocity.X, velocity.Y, 0);
                Vector3 lPos = vel3 * _beamRange;
                BeamInfoDB bidb = new BeamInfoDB(000, _selectedEntity, true, 300)
                {
                    VelocityVector = vel3,
                    LaunchPosition = lPos,
                    Energy = _beamEnergy,
                    Frequency = _beamFreq,
                };
                _projectileDamageMap = new DamageMap((int)dmProjStart.X, (int)dmProjStart.Y, bidb, _beamlifetime);
            }


            //_projectileDMapPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _damageMap.compIDMap, _damageMap.Width, _damageMap.Height);
        }
    }
}
