using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib.ComponentFeatureSets;
using Pulsar4X.Orbital;
using SDL2;
using Vector3 = Pulsar4X.Orbital.Vector3;


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
        Vector3 _position = new Vector3();
        private Vector3 _graphicPos = new Vector3();
        private EntitySpawnGraphic _icon;
        
        private EntityNameSelector _sysBodies;
        private EntityNameSelector _factionEntites;
        private EntityNameSelector _factionOwnerEntites;
        
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
            
            MinMaxStruct inner = new MinMaxStruct(10, 10000);
            MinMaxStruct hab = new MinMaxStruct(10000, 100000);
            MinMaxStruct outer = new MinMaxStruct(100000, 10000000);
            _bandinfo = (inner, hab, outer, true);
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
                        _icon = new EntitySpawnGraphic(_graphicPos);
                        _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(EntitySpawnGraphic), _icon);
                    }
                }

                if (ImGui.IsWindowCollapsed())
                {
                    if (_uiState.SelectedSysMapRender.UIWidgets.ContainsKey(nameof(EntitySpawnGraphic)))
                        _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(EntitySpawnGraphic));
                }
                
                if (ImGui.Combo("##entityselector", ref _entityindex, _entitytypes, _entitytypes.Length)) 
                { 

                }


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
        void Planet()
        {
            ImGui.InputText("Name", _nameInputBuffer, 16);
            
            if (_sysBodies.Combo("Orbital Parent"))
            {
                var starInfo = _sysBodies.GetSelectedEntity().GetDataBlob<StarInfoDB>();
                starInfo = _sysBodies.GetSelectedEntity().GetDataBlob<StarInfoDB>();
                Entity parentstar;
                while (starInfo == null)
                {
                    parentstar = _sysBodies.GetSelectedEntity().GetSOIParentEntity();
                    starInfo = parentstar.GetDataBlob<StarInfoDB>();
                    _bandinfo = SystemBodyFactory.HabitibleZones(_sysGensettings, starInfo);
                
                }
            }

            var max = Math.Min(_bandinfo.outer.Max, _sysBodies.GetSelectedEntity().GetSOI_m());
            var min = Math.Min(_bandinfo.inner.Min, _sysBodies.GetSelectedEntity().GetDataBlob<MassVolumeDB>().RadiusInM);
            if (_sysBodies.GetSelectedEntity().HasDataBlob<StarInfoDB>())
                min = _bandinfo.inner.Min;
            if (ImGui.DragInt("Radius from parent", ref _rad, 10, (int)min * 1000, (int)max * 1000))
            {
                var datetime = _sysBodies.GetSelectedEntity().StarSysDateTime;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsoluteFuturePosition(datetime);
                _icon.WorldPosition_m = new Vector3( parentPos.X + _xpos * 1000, parentPos.Y, _xpos);
                
            }
            

            
            if (ImGui.Combo("Body Type", ref _bodyTypeIndex, _bodyTypes, _bodyTypes.Length))
            {
                var setting = SystemGenSettingsSD.DefaultSettings;
                BodyType btype = (BodyType)_bodyTypeIndex;
                _massTon = (float)(0.001 * GeneralMath.SelectFromRange(setting.SystemBodyMassByType[btype], Math.Pow(_rng.NextDouble(), 3))); // cache mass, alos cube random nuber to make smaller bodies more likly.
                _density = (float) GeneralMath.SelectFromRange(setting.SystemBodyDensityByType[btype], _rng.NextDouble());
                var volume = _massTon * 1000 / _density;
                _radiusKM = (float)(MassVolumeDB.CalculateRadius_m(_massTon * 1000, _density) * 0.001);
            }

            if (ImGui.DragFloat("Mass in Tons", ref _massTon))
            {
                _radiusKM = (float)MassVolumeDB.CalculateRadius_m(_massTon, _density);
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

            
            /*
            if (ImGui.DragInt("X km ralitve", ref _xpos))
            {
                var datetime = _sysBodies.GetSelectedEntity().StarSysDateTime;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsoluteFuturePosition(datetime);
                _icon.WorldPosition_m = new Vector3( parentPos.X + _xpos * 1000);
            }

            if (ImGui.DragInt("Y km ralitve", ref _ypos))
            {
                var datetime = _sysBodies.GetSelectedEntity().StarSysDateTime;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsoluteFuturePosition(datetime);
                _icon.WorldPosition_m = new Vector3( parentPos.Y + _ypos * 1000);
            }
*/

            
            


            if (ImGui.Button("Create Entity"))
            {
                
                var system = _uiState.SelectedSystem;
                var newBody = SystemBodyFactory.GenerateSingleBody(_sysGensettings, system, _sysBodies.GetSelectedEntity(), _rad);
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
            }
            
            ImGui.Combo("Select Design", ref _selectedDesignIndex, _shipDesignNames, _shipDesignNames.Length);

            _sysBodies.Combo("Orbital Parent");
            if (ImGui.DragInt("X km ralitve", ref _xpos))
            {
                var datetime = _sysBodies.GetSelectedEntity().StarSysDateTime;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsoluteFuturePosition(datetime);
                _icon.WorldPosition_m = new Vector3( parentPos.X + _xpos * 1000, parentPos.Y, 0);
            }

            if (ImGui.DragInt("Y km ralitve", ref _ypos))
            {
                var datetime = _sysBodies.GetSelectedEntity().StarSysDateTime;
                var parentPos = _sysBodies.GetSelectedEntity().GetAbsoluteFuturePosition(datetime);
                _icon.WorldPosition_m = new Vector3(parentPos.X, parentPos.Y + _ypos * 1000,0);
            }

            _factionOwnerEntites.Combo("Set Owner Faction");
            ImGui.InputText("Ship Name", _nameInputBuffer, 16);

            bool createEnabled = false;

            if ( //check if we can enable the create button.
                _exsistingClasses.Count > 0 && 
                _selectedDesignIndex >= 0 &&
                _factionEntites.IsItemSelected &&
                _sysBodies.IsItemSelected)
                createEnabled = true;

            if (!createEnabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
            }

            if(ImGui.Button("Create Entity") && createEnabled)
            {
                var selectedSystem = _uiState.SelectedSystem;
                string shipName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                //var parent = OrbitProcessor.FindSOIForPosition(_uiState.SelectedSystem, _icon.WorldPosition_m);
                Vector3 ralitivePos = _icon.WorldPosition_m;
                Entity _spawnedship = ShipFactory.CreateShip(
                    _exsistingClasses[_selectedDesignIndex], 
                    _factionEntites.GetSelectedEntity(), 
                    ralitivePos,
                    _sysBodies.GetSelectedEntity(),
                    _uiState.SelectedSystem, 
                    shipName);
                //hacky force a refresh
                _uiState.StarSystemStates[selectedSystem.Guid] = SystemState.GetMasterState(selectedSystem);
                _uiState.SelectedSysMapRender.OnSelectedSystemChange(_uiState.SelectedSystem);
                
            }

            if (!createEnabled)
            {
                ImGui.PopStyleVar();
            }
        }

        void Colony()
        {
            
            _sysBodies.Combo("Planet:");
            _factionOwnerEntites.Combo("Owner");
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
            
            PointD[] points = CreatePrimitiveShapes.Circle(0, 0, 3, 12);

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
                PointD[] drawPoints = new PointD[shape.Points.Length];
                for (int j = 0; j < drawPoints.Length; j++)
                {
                    double x = shape.Points[j].X;
                    double y = shape.Points[j].Y;
                    drawPoints[j] = trns.TransformD(x, y);
                }
                DrawShapes[i] = new Shape() {Points = drawPoints, Color = shape.Color};
            }

        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            base.Draw(rendererPtr, camera);
        }
    }
}
