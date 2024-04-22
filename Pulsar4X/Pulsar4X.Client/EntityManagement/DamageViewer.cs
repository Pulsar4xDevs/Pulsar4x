using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Damage;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;


namespace Pulsar4X.SDL2UI.Combat
{
    public class DamageViewer : PulsarGuiWindow
    {
        //private ComponentDesign _componentDesign;
        private Entity? _selectedEntity;

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
        private List<RawBmp>? _damageFrames = null;
        private int _showFrameNum = 0;
        private IntPtr _showDmgFrametx;

        private EntityDamageProfileDB? _profile;
        private RawBmp _rawShipImage;
        private IntPtr _shipImgPtr;



        private DamageViewer()
        {

            DamageResistBlueprint polyprop = new DamageResistBlueprint(100, 255, 1175f);
            DamageResistBlueprint aluminium = new DamageResistBlueprint(150, 255, 2700f);
            DamageResistBlueprint titanium = new DamageResistBlueprint(200, 255, 4540f);
            DamageResistBlueprint steelCarbon = new DamageResistBlueprint(230, 255, 7860);
            DamageResistBlueprint steelStainless = new DamageResistBlueprint(255, 255, 7900);


            //_componentDesign.DamageResistance = aluminium;

        }

        public static DamageViewer GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DamageViewer)))
            {
                var dv = new DamageViewer();
                if (_uiState.PrimaryEntity?.Entity != null)
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

                if (_profile.DamageEvents.Count > 0)
                {
                    _damageEventIndex = _profile.DamageEvents.Count - 1;
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
            _damageFrames = DamageTools.DealDamageSim(_profile, _profile.DamageEvents[_damageEventIndex]).damageFrames;
            _showFrameNum = 0;
            if(_damageFrames != null)
                _showDmgFrametx = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _damageFrames[_showFrameNum]);
        }

        static class Beam
        {
            public static double BeamFreq = 700;
            public static double MinFreq; //in meters
            public static double MaxFreq; //in meters
            
            public static double BeamEnergy = 1000;
            public static double MinEnergy = 10; //in meters
            public static double MaxEnergy = 10000; //in meters



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
                if(_factionInfoDB is null)
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

        private int _beamTypeIndex = 5;
        private double _momentum = 0;
        DamageFragment _damageFrag;
        private bool _typeIsBeam = true;
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

                        
                        //type of damage to test
                        ExsistingWeapons.Create(_selectedEntity.GetFactionOwner);
                        if (ImGui.Combo("Exsisting Design", ref ExsistingWeapons.SelectedWeaponIndex, ExsistingWeapons.WeaponNames, ExsistingWeapons.AvailableShipComponents.Count))
                        {
                            
                            //ExsistingWeapons.SelectedWeapon.
                        }
                        if (_profile != null && ImGui.Button("Beam"))
                        {
                            _typeIsBeam = true;
                        }
                        if (_profile != null && ImGui.Button("Longrod Projectile"))
                        {
                            _typeIsBeam = false;
                        }
                        
                        
                        
                        ImGui.NextColumn();

                        //tweaks to damage type
                        if (_typeIsBeam)
                        {
                            if (ImGuiExt.SliderDouble("Energy", ref Beam.BeamEnergy, Beam.MinEnergy, Beam.MaxEnergy))
                            {

                            }

                            if (ImGuiExt.SliderDouble("Freqency", ref Beam.BeamFreq, Beam.MinFreq, Beam.MaxFreq))
                            {
                                _momentum = (float)(UniversalConstants.Science.PlankConstant * Beam.BeamFreq);
                            }
                        }
                        else
                        {
                            
                        }

                        ImGui.NextColumn();



                        ImGui.NextColumn();

                        ImGui.InputFloat("Mass", ref _projMass);
                        ImGui.InputFloat("Density", ref _projDensity);
                        ImGui.InputFloat("Length", ref _projLen);


                        ImGui.Columns(0);

                        if (ImGui.Button("Fire"))
                        {
                            _damageFrag = new DamageFragment()
                            {
                                Position = _firePos,
                                Velocity = new Orbital.Vector2(_fireVel.x, _fireVel.y),
                                Mass = _projMass,
                                Density = _projDensity,
                                Length = _projLen
                            };
                            _damageFrames = DamageTools.DealDamageSim(_profile, _damageFrag).damageFrames;
                            _rawShipImage = _damageFrames.Last();
                        }


                    }
                }

                if(_profile != null && _profile.DamageEvents.Count > 0)
                {
                    if (ImGui.SliderInt("Damage Events", ref _damageEventIndex, 1, _profile.DamageEvents.Count - 1))
                    {
                        SetDamageEventFrames();
                    }
                }

                if(_profile != null && _profile.DamageEvents.Count > 0 && _damageFrames == null)
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
