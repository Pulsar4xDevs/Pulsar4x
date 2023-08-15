using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib.ComponentFeatureSets;
using Pulsar4X.Orbital;
using SDL2;
using Vector3 = Pulsar4X.Orbital.Vector3;
using static Pulsar4X.SDL2UI.ImGuiExt;
using Vector2 = Pulsar4X.Orbital.Vector2;

namespace Pulsar4X.SDL2UI
{
    public class EntitySpawnWindow : PulsarGuiWindow
    {
        private List<ShipDesign> _exsistingClasses = new List<ShipDesign>();
        private string[] _entitytypes = new string[]{ "Ship", "Planet", "Colony" };
        private int _entityindex;
        byte[] _nameInputBuffer = ImGuiSDL2CSHelper.BytesFromString("", 16);
        private string[] _bodyTypes;
        private int _bodyTypeIndex = 1;
        Random _rng = new Random();
        //private Vector3 _graphicPos = new Vector3();
        private EntitySpawnGraphic _icon;
        
        private EntityNameSelector _sysBodies;
        private EntityNameSelector _factionEntites;
        private EntityNameSelector _factionOwnerEntites;
        private EntityNameSelector _speciesEntites;
        private KeplerElements _ke;
        private StateVectors _sv;
        private double _objMass = 1000;
        private double _parentMass = 1e10;
        private double _sgp;
        private KeIcon _keIcon;
        private Icon _soiIcon;
        private double _parentSOI = 0;
        private Entity _parentObect;
        
        private EntitySpawnWindow()
	    {
	        _flags = ImGuiWindowFlags.AlwaysAutoResize;
            _bodyTypes = Enum.GetNames(typeof(BodyType));

            
            _factionEntites = new EntityNameSelector(
                StaticRefLib.Game.Factions.ToArray(), 
                EntityNameSelector.NameType.Owner );
            _factionOwnerEntites = new EntityNameSelector(
                StaticRefLib.Game.Factions.ToArray(), 
                EntityNameSelector.NameType.Owner );

            var bodies = _uiState.SelectedSystem.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>();
            var stars = _uiState.SelectedSystem.GetAllEntitiesWithDataBlob<StarInfoDB>();
            bodies.AddRange(stars);
            _sysBodies = new EntityNameSelector(bodies.ToArray(), EntityNameSelector.NameType.Owner);
            _parentObect = _sysBodies.GetSelectedEntity();
            var species = StaticRefLib.Game.GlobalManager.GetAllEntitiesWithDataBlob<SpeciesDB>().ToArray();
            _speciesEntites = new EntityNameSelector(species, EntityNameSelector.NameType.Owner);
            
            MinMaxStruct inner = new MinMaxStruct(10, 10000);
            MinMaxStruct hab = new MinMaxStruct(10000, 100000);
            MinMaxStruct outer = new MinMaxStruct(100000, 10000000);
            _bandinfo = (inner, hab, outer, true);
            var date = _uiState.SelectedSystem.StarSysDateTime;
            _parentMass = _parentObect.GetDataBlob<MassVolumeDB>().MassDry;
            _parentSOI = _parentObect.GetSOI_m();
            _sgp = GeneralMath.StandardGravitationalParameter(_objMass + _parentMass);
            _ke = OrbitalMath.FromPosition(new Vector3(10000, 0, 0), _sgp, date);
            
            _soiIcon = new Icon(_parentObect.GetDataBlob<PositionDB>());
            Shape soishape = new Shape();
            var soiau = Distance.MToAU(_parentSOI);
            soishape.Points = CreatePrimitiveShapes.Circle(0, 0, soiau, 64);
            byte r = 0;
            byte g = 200;
            byte b = 100;
            byte a = 200;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            soishape.Color = colour;
            _soiIcon.Shapes.Add(soishape);
            //_soiIcon.DebugShowCenter = true;

        }

        internal static EntitySpawnWindow GetInstance() {

            EntitySpawnWindow thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(EntitySpawnWindow)))
            {
                thisItem = new EntitySpawnWindow();
            }
            else
            {
                thisItem = (EntitySpawnWindow)_uiState.LoadedWindows[typeof(EntitySpawnWindow)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
           
            if (IsActive && ImGui.Begin("Spawn Entity", _flags))
            {
                if (ImGui.IsWindowAppearing())
                {
                    if (!_uiState.SelectedSysMapRender.UIWidgets.ContainsKey(nameof(EntitySpawnGraphic)))
                    {
                        var parentPos = _sysBodies.GetSelectedEntity().GetAbsolutePosition();
                        _icon = new EntitySpawnGraphic(parentPos + _sv.Position);
                        _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(EntitySpawnGraphic), _icon);
                    }
                    if (!_uiState.SelectedSysMapRender.UIWidgets.ContainsKey("soiIcon"))
                    {
                        _uiState.SelectedSysMapRender.UIWidgets.Add("soiIcon", _soiIcon);
                    }
                }

                if (ImGui.IsWindowCollapsed())
                {
                    if (_uiState.SelectedSysMapRender.UIWidgets.ContainsKey(nameof(EntitySpawnGraphic)))
                        _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(EntitySpawnGraphic));
                    
                    if (_uiState.SelectedSysMapRender.UIWidgets.ContainsKey("soiIcon"))
                        _uiState.SelectedSysMapRender.UIWidgets.Remove("soiIcon");
                }
                
                if (ImGui.Combo("##entityselector", ref _entityindex, _entitytypes, _entitytypes.Length)) 
                { 

                }

                if (_soiIcon != null)//hacky way to scale the icon to zoom
                    _soiIcon.Scale = _uiState.Camera.ZoomLevel;


                if (_entitytypes[_entityindex] == "Ship") 
                {
                    Ship();
                }
                
                if (_entitytypes[_entityindex] == "Planet")
                {
                    Planet();
                }
                
                if(_entitytypes[_entityindex] == "Colony")
                    Colony();
            }
            else
            {

            }
            ImGui.End();
            
        }
        
        float _radiusKM = 5000;
        float _massTon = 500000;
        float _density = 500;
        private int _xpos = 0;
        private int _ypos = 0;
        private int _rad = 100;
        private (MinMaxStruct inner, MinMaxStruct habitible, MinMaxStruct outer, bool hasHabitible) _bandinfo;
        SystemGenSettingsSD _sysGensettings =  SystemGenSettingsSD.DefaultSettings;
        private Entity _parentStar;

        private StarInfoDB _starInfo;
        void Planet()
        {
            
            ImGui.InputText("Name", _nameInputBuffer, 16);

            SetParent();

            var max = Math.Min(_bandinfo.outer.Max, _sysBodies.GetSelectedEntity().GetSOI_m());
            var min = Math.Min(_bandinfo.inner.Min, _sysBodies.GetSelectedEntity().GetDataBlob<MassVolumeDB>().RadiusInM);
            if (_sysBodies.GetSelectedEntity().HasDataBlob<StarInfoDB>())
                min = _bandinfo.inner.Min;

            bool enabled = false;
            if (_parentStar != null && _starInfo != null)
                enabled = true;

            
            if (ImGui.DragInt("Radius from parent", ref _rad, 100, (int)min * 1000, (int)max * 1000) && enabled)
            {
                var datetime = _sysBodies.GetSelectedEntity().StarSysDateTime;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsolutePosition();
                _icon.WorldPosition_m = new Vector3( parentPos.X + _xpos * 1000, parentPos.Y, _xpos);

                
                
                
                
                var bandRadius = _parentObect.GetAbsoluteFuturePosition(_parentStar.StarSysDateTime).Length();
                var zones = SystemBodyFactory.HabitibleZones(_sysGensettings, _starInfo);
                MinMaxStruct zone;
                // SystemBand band; 
                if (zones.hasHabitible && bandRadius > zones.habitible.Min && bandRadius < zones.habitible.Max)
                {
                    zone = zones.habitible;
                    // band = SystemBand.HabitableBand;
                }
                else if (bandRadius < zones.inner.Max && bandRadius > zones.inner.Min)
                {
                    zone = zones.inner;
                    // band = SystemBand.InnerBand;
                }
                else if (bandRadius > zones.outer.Min && bandRadius < zones.outer.Max)
                {
                    zone = zones.outer;
                    //band = SystemBand.OuterBand;
                }
                //else throw new Exception("bad radius");
                
            }
            

            //ImGui.Text("Band: " + );


            BodyType btype = BodyType.Unknown;
            if (ImGui.Combo("Body Type", ref _bodyTypeIndex, _bodyTypes, _bodyTypes.Length))
            {
                
                btype = (BodyType)_bodyTypeIndex;
                _massTon = (float)(0.001 * GeneralMath.Lerp(_sysGensettings.SystemBodyMassByType[btype], Math.Pow(_rng.NextDouble(), 3))); // cache mass, alos cube random nuber to make smaller bodies more likly.
                _density = (float) GeneralMath.Lerp(_sysGensettings.SystemBodyDensityByType[btype], _rng.NextDouble());
                var volume = _massTon * 1000 / _density;
                _radiusKM = (float)(MassVolumeDB.CalculateRadius_m(_massTon * 1000, _density) * 0.001);
            }

            if (ImGui.DragFloat("Mass in Tons", ref _massTon))
            {
                _radiusKM = (float)MassVolumeDB.CalculateRadius_m(_massTon, _density);
                SetMass(_massTon * 1000);

            }
            if (ImGui.DragFloat("Density: ", ref _density))
            {
                _radiusKM = (float)MassVolumeDB.CalculateRadius_m(_massTon, _density);
            }
            if (ImGui.DragFloat("Radius in Km", ref _radiusKM))
            {
                var volume = MassVolumeDB.CalculateVolume_m3(_radiusKM * 1000);
                _density = (float)MassVolumeDB.CalculateDensity(_massTon * 1000, volume);
            }

            

            OrbitEditWidget.Display(ref _ke, ref _sv, _parentSOI, OrbitEditWidget.WidgetStyle.Newtonion);
            


            if (ButtonED("Create Entity", enabled))
            {
                
                var system = _uiState.SelectedSystem;
                var newBody = SystemBodyFactory.GenerateSingleBody(_sysGensettings, system, _sysBodies.GetSelectedEntity(), btype, _rad);
                MassVolumeDB massvol = MassVolumeDB.NewFromMassAndDensity(_massTon * 1000, _density);
                newBody.SetDataBlob(massvol);
            }
            
        }

   
        private string[] _shipDesignNames = new string[0];
        private int _selectedDesignIndex = 0;

        void Ship()
        {
            if (_factionEntites.Combo("Faction Designs"))
            {
                _selectedDesignIndex = 0;
                _exsistingClasses = _factionEntites.GetSelectedEntity().GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
                _shipDesignNames = new string[_exsistingClasses.Count];
                for (int i = 0; i < _exsistingClasses.Count; i++)
                {
                    _shipDesignNames[i] = _exsistingClasses[i].Name;
                }

                if (_exsistingClasses.Count > 0 && _selectedDesignIndex >= 0)
                {
                    _parentMass = _sysBodies.GetSelectedEntity().GetDataBlob<MassVolumeDB>().MassDry;
                    _objMass = _exsistingClasses[_selectedDesignIndex].MassPerUnit; 
                    _sgp = GeneralMath.StandardGravitationalParameter(_objMass + _parentMass);
                    _ke.StandardGravParameter = _sgp;
                    if(_keIcon != null)
                        _keIcon.ForceUpdate(_ke, _sv);
                }

            }

            if (ImGui.Combo("Select Design", ref _selectedDesignIndex, _shipDesignNames, _shipDesignNames.Length))
            {
                
                _parentMass = _sysBodies.GetSelectedEntity().GetDataBlob<MassVolumeDB>().MassDry;
                _objMass = _exsistingClasses[_selectedDesignIndex].MassPerUnit; 
                _sgp = GeneralMath.StandardGravitationalParameter(_objMass + _parentMass);
                _ke.StandardGravParameter = _sgp;
                if(_keIcon != null)
                    _keIcon.ForceUpdate(_ke, _sv);
            }

            if (SetParent())
            {

            }


            _factionOwnerEntites.Combo("Set Owner Faction");

            BorderGroup.Begin("State Vectors");
            ImGui.Text("a: " + _ke.SemiMajorAxis);
            ImGui.Text("e: " + _ke.Eccentricity);
            ImGui.Text("Ω: " + _ke.LoAN);
            ImGui.Text("ω: " + _ke.AoP );
            if (OrbitEditWidget.Display(ref _ke, ref _sv, _parentSOI, OrbitEditWidget.WidgetStyle.Newtonion))
            {
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsolutePosition();
                _icon.WorldPosition_m = parentPos + _sv.Position;
                if(_keIcon != null)
                    _keIcon.ForceUpdate(_ke, _sv);
            }
            BorderGroup.End();
            ImGui.InputText("Ship Name", _nameInputBuffer, 16);

            bool createEnabled = false;

            if ( //check if we can enable the create button.
                _exsistingClasses.Count > 0 && 
                _selectedDesignIndex >= 0 &&
                _factionEntites.IsItemSelected &&
                _sysBodies.IsItemSelected)
            {
                createEnabled = true;
                
                if(_keIcon == null)
                {
                    var parentPos = _sysBodies.GetSelectedEntity().GetAbsolutePosition();
                    _keIcon = new KeIcon(_ke, _sv, parentPos, _parentSOI, _uiState);
                    _uiState.SelectedSysMapRender.UIWidgets["keIcon"] = _keIcon;
                }
            }

            
            
            
            if(ButtonED("Create Entity", createEnabled))
            {
                var selectedSystem = _uiState.SelectedSystem;
                string shipName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                //var parent = OrbitProcessor.FindSOIForPosition(_uiState.SelectedSystem, _icon.WorldPosition_m);
                
                ShipFactory.CreateShip(
                    _exsistingClasses[_selectedDesignIndex],
                    _factionEntites.GetSelectedEntity(),
                    _ke,
                    _sysBodies.GetSelectedEntity(),
                    shipName);
                //hacky force a refresh
                _uiState.StarSystemStates[selectedSystem.Guid] = SystemState.GetMasterState(selectedSystem);
                _uiState.SelectedSysMapRender.OnSelectedSystemChange(_uiState.SelectedSystem);

                _uiState.SelectedSysMapRender.UIWidgets.Remove("keIcon");
            }
            
            
        }

        private int _popCount = 100000;
        void Colony()
        {
            bool createEnabled = false;
            _sysBodies.Combo("Planet:");
            _factionOwnerEntites.Combo("Owner");

            _speciesEntites.Combo("Species");

            ImGui.SliderInt("Population Count", ref _popCount, 0, int.MaxValue);
            
            if (_sysBodies.IsItemSelected && _factionOwnerEntites.IsItemSelected && _speciesEntites.IsItemSelected)
                createEnabled = true;
            
            if(ButtonED("Create Entity", createEnabled))
            {
                var selectedSystem = _uiState.SelectedSystem;
                string shipName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                //var parent = OrbitProcessor.FindSOIForPosition(_uiState.SelectedSystem, _icon.WorldPosition_m);
                Vector3 ralitivePos = _icon.WorldPosition_m;
                Entity colonyEnitity = ColonyFactory.CreateColony(_factionOwnerEntites.GetSelectedEntity(), _speciesEntites.GetSelectedEntity(), _sysBodies.GetSelectedEntity(), _popCount);
                //hacky force a refresh
                _uiState.StarSystemStates[selectedSystem.Guid] = SystemState.GetMasterState(selectedSystem);
                _uiState.SelectedSysMapRender.OnSelectedSystemChange(_uiState.SelectedSystem);
                
            }
        }

        bool SetParent()
        {
            if (_sysBodies.Combo("Orbital Parent"))
            {
                _parentObect = _sysBodies.GetSelectedEntity();
                _parentStar = _parentObect;
                _parentMass = _parentObect.GetDataBlob<MassVolumeDB>().MassDry;
                _starInfo = _parentStar.GetDataBlob<StarInfoDB>();
                
                while (_starInfo == null)
                {
                    _parentStar = _sysBodies.GetSelectedEntity().GetSOIParentEntity();
                    _starInfo = _parentStar.GetDataBlob<StarInfoDB>();
                    _bandinfo = SystemBodyFactory.HabitibleZones(_sysGensettings, _starInfo);
                
                }
                _sgp = GeneralMath.StandardGravitationalParameter(_objMass + _parentMass);
                _ke.StandardGravParameter = _sgp;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsolutePosition();
                _icon.WorldPosition_m = parentPos + _sv.Position;
                _parentSOI = _parentObect.GetSOI_m();
                if (_keIcon != null)
                {
                    _keIcon.UpdateParent(parentPos, _parentSOI);
                    _keIcon.ForceUpdate(_ke, _sv);
                }

 
                _soiIcon.ResetPositionDB(_parentObect.GetDataBlob<PositionDB>());
                Shape soishape = new Shape();
                var soiau = Distance.MToAU(_parentSOI);
                soishape.Points = CreatePrimitiveShapes.Circle(0, 0, soiau, 64);
                byte r = 0;
                byte g = 200;
                byte b = 100;
                byte a = 200;
                SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
                soishape.Color = colour;
                _soiIcon.Shapes[0]=soishape;

                

                return true;
            }

            return false;
        }

        void SetMass(double massKG)
        {
            _objMass = massKG;
            _sgp = GeneralMath.StandardGravitationalParameter(_objMass + _parentMass);
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            
        }
    }

    public class EntitySpawnGraphic : Icon
    {
       

        public EntitySpawnGraphic(Vector3 position_m) : base(position_m)
        {
                 
            BasicShape();
        }
        
        
        void BasicShape()
        {
            byte r = 150;
            byte g = 50;
            byte b = 200;
            byte a = 255;
            
            Orbital.Vector2[] points = CreatePrimitiveShapes.Circle(0, 0, 3, 12);

            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() {Points = points, Color = colour});
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            var foo = camera.ViewCoordinate_m(WorldPosition_m);
            var trns = Matrix.IDTranslate(foo.x, foo.y);
            var scaleMtx = Matrix.IDScale(Scale, Scale);
            
            var scZm = Matrix.IDScale(camera.ZoomLevel, camera.ZoomLevel);
            var mtrx = scZm *  trns;
            DrawShapes = new Shape[Shapes.Count];
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                Orbital.Vector2[] drawPoints = new Orbital.Vector2[shape.Points.Length];
                for (int j = 0; j < drawPoints.Length; j++)
                {
                    double x = shape.Points[j].X;
                    double y = shape.Points[j].Y;
                    drawPoints[j] = trns.TransformToVector2(x, y);
                }
                DrawShapes[i] = new Shape() {Points = drawPoints, Color = shape.Color};
            }

        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            base.Draw(rendererPtr, camera);
        }
    }

    public static class OrbitEditWidget
    {
        public enum WidgetStyle
        {
            Newtonion,
            Keplerian2d1,
            Keplerian2d2,
            Keplerian3d
        }

        public static WidgetStyle Style = WidgetStyle.Keplerian2d2;

        public static bool LockPosition = false;
        //public static KeplerElements _ke;
        private static Orbital.Vector2 _vel = new Orbital.Vector2();
        private static Orbital.Vector2 _pos = new Orbital.Vector2();
        private static VectorWidget2d.Style posStyle = VectorWidget2d.Style.Cartesian;
        private static VectorWidget2d.Style velStyle = VectorWidget2d.Style.Polar;
        
        public static bool Display(ref KeplerElements ke, ref StateVectors sv, double soi, WidgetStyle style)
        {
            //_ke = ke;
            Style = style;
            _pos.X = sv.Position.X;
            _pos.Y = sv.Position.Y;
            _vel.X = sv.Velocity.X;
            _vel.Y = sv.Velocity.Y;
            bool changed = false;
            double maxRadius = Math.Min(double.MaxValue, soi);
            if(VectorWidget2d.Display("Position Vector", ref _pos, 0, maxRadius))
            {
                sv.Position = new Vector3(_pos.X, _pos.Y, 0);
                ke = OrbitalMath.KeplerFromPositionAndVelocity(
                    ke.StandardGravParameter, 
                    sv.Position,
                    sv.Velocity, 
                    ke.Epoch);
                changed = true;
            }
            
            switch (style)
            {
                case WidgetStyle.Newtonion:
                    if (NewtonionStyle(ref ke, ref sv))
                        changed = true;
                    break;
                case WidgetStyle.Keplerian2d1:
                    if(KeplerianStyle2d_1(ref ke, ref sv))
                        changed = true;
                    break;
                case WidgetStyle.Keplerian2d2:
                    if(KeplerianStyle2d_2(ref ke, ref sv))
                        changed = true;
                    break;
                case WidgetStyle.Keplerian3d:
                    if(KeplerianStyle3d_1(ref ke, ref sv))
                        changed = true;
                    break;
                
            }
            return changed;
        }


        
        static bool NewtonionStyle(ref KeplerElements ke, ref StateVectors sv)
        {
            if(VectorWidget2d.Display("Velocity Vector", ref _vel, 0, 299792458))
            {
                sv.Velocity = new Vector3(_vel.X, _vel.Y, 0);
                ke = OrbitalMath.KeplerFromPositionAndVelocity(
                    ke.StandardGravParameter, 
                    sv.Position, 
                    sv.Velocity, 
                    ke.Epoch);
                return true;
            }
            return false;
        }

        private static float _radius = 0;
        private static float _eccentricity = 0;
        private static float _semiMajorAxis = 1000;
        private static float _lop = 0;
        private static float _lopAndTrue = 0;
        private static bool _clockwise;
        static bool KeplerianStyle2d_1(ref KeplerElements ke, ref StateVectors sv)
        {
            bool changed = false;
            if (ImGui.DragFloat("Radius", ref _radius))
            {
                changed = true;
            }

            if (SliderAngleED("ϖ+ν", ref _lopAndTrue, LockPosition))
            {
                changed = true;
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Londitude of Periapsis + True Anomaly");

            if (SliderDouble("e", ref ke.Eccentricity, 0, double.MaxValue))
            {
                
                
                //double trueAnomaly = OrbitalMath.TrueAnomaly(eccentVector, position, velocity);
                //double eccentricAnomoly = OrbitalMath.GetEccentricAnomalyFromTrueAnomaly(trueAnomaly, _ke.Eccentricity);
                //var meanAnomaly = OrbitalMath.GetMeanAnomaly(_ke.Eccentricity, eccentricAnomoly);
                changed = true;
            }

            return changed;
        }

        static bool KeplerianStyle2d_2(ref KeplerElements ke, ref StateVectors sv)
        {
            bool changed = false;
            if(SliderDouble("a", ref ke.SemiMajorAxis, 0, double.MaxValue))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Semi Major Axis");
            if(SliderDouble("e", ref ke.Eccentricity, 0, double.MaxValue))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("e(ccentricity)");

            if(ImGui.SliderAngle("ϖ", ref _lop))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Londitude of Periapsis, is Ω + ω (Londitude of Assending Node + Argument of Periapsis)");
            if(ImGui.Checkbox("Clockwise Orbit", ref _clockwise))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("i(nclination) is 0 or 180 in a 2d orbit");
            if(SliderAngleED("ν", ref _trueAnomaly, LockPosition))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("True Anomaly");
            return changed;
        }

        private static float _inclination = 0;
        private static float _loAN = 0;
        private static float _aoP = 0;
        private static float _trueAnomaly = 0;
        static bool KeplerianStyle3d_1(ref KeplerElements ke, ref StateVectors sv)
        {
            bool changed = false;
            if(ImGui.DragFloat("a", ref _semiMajorAxis))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Semi Major Axis");
            
            if(SliderDouble("e", ref ke.Eccentricity, 0, double.MaxValue))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("e(ccentricity)");

            if(ImGui.SliderAngle("Ω", ref _loAN))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Londitude of AccendingNode");
            
            if(ImGui.SliderAngle("ω", ref _aoP))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Argument of Periapsis)");
            
            if(ImGui.SliderAngle("i", ref _inclination))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("i(nclination)");

            if(SliderAngleED("ν", ref _trueAnomaly, LockPosition))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("True Anomaly");

            return changed;


        }

    }
}
