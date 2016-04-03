using OpenTK;
using OpenTK.Graphics;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel.SystemView
{
    public class Scene
    {
        public List<Vector3> position_data;
        public List<float> scale_data;
        public Mesh mesh;
        public Camera camera;
        public bool IsInitialized = false;
        public int position_buffer_id;


        public List<SystemObjectRenderInfo> SystemBodies { get; set; } = new List<SystemObjectRenderInfo>();

        //public Scene(List<Vector3> position_data, List<float> scale_data, Mesh mesh, Camera camera)
        //{
        //    this.position_data = position_data;
        //    this.scale_data = scale_data;
        //    this.mesh = mesh;
        //    this.camera = camera;
        //}

        public Scene(StarSystem starSys, AuthenticationToken authToken, List<float> scale_data, Camera camera)
        {
            this.scale_data = scale_data;
            this.camera = camera;
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectRenderInfo(item));
            }
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectRenderInfo(item));
            }
        }
    }


    public class SystemObjectRenderInfo : ViewModelBase
    {

        #region MoveSomehwereElse                        
        public string DEFAULT_PLANET_ICON = "./Resources/Textures/DefaultIcon.png";
        public string DEFAULT_TASKGROUP_ICON = "./Resources/Textures/DefaultTGIcon.png";
        public string DEFAULT_JUMPPOINT_ICON = "./Resources/Textures/DefaultJPIcon.png";
        public string DEFAULT_TEXTURE = "./Resources/Textures/DefaultTexture.png";
        public string DEFAULT_GLFONT = "./Resources/Fonts/PulsarFont.xml";
        public string DEFAULT_GLFONT2 = "./Resources/Fonts/DejaVuSansMonoBold.xml";
        //Vector2 DEFAULT_TEXT_SIZE = new Vector2(16, 16);
        #endregion MoveSomewhereElse

        public DateTime _currentTime;
        public DateTime CurrentTime { get { return _currentTime; } set { _currentTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(Position)); } }

        private Entity _systemObjectEntity;

        private PositionDB _positionDB;
        private MassVolumeDB _massVolumeDB;


        public ECSLib.Vector4 Position
        {
            get { return _positionDB.Position; }
        }

        public Color4 ItemColour { get; private set; } //maybe use something more generic and move to view
        public string ItemTextureName { get; private set; }

        public string LabelName { get; set; }

        public double ItemRadiusAU { get { return _massVolumeDB.Radius; } }
        public double ItemRadiusKM { get { return _massVolumeDB.RadiusInKM; } }

        #region TODO move to View

 
        // create name lable:
        

        public Vector2 Size { get; set; }
        
        public Vector3 PositionGL { get { return new Vector3((float)Position.X, (float)Position.Y, (float)Position.Z); } } //TODO fix this.use different units for display? 



        #endregion TODO move to View



        public SystemObjectRenderInfo(Entity systemObjectEntity)
        {


            _systemObjectEntity = systemObjectEntity;

            _positionDB = systemObjectEntity.GetDataBlob<PositionDB>();
            _massVolumeDB = systemObjectEntity.GetDataBlob<MassVolumeDB>();

            if (systemObjectEntity.HasDataBlob<StarInfoDB>())//is a star
                StarSetup(systemObjectEntity.GetDataBlob<StarInfoDB>());
            else if (systemObjectEntity.HasDataBlob<SystemBodyDB>())//is an object other than a star
                Planetetup(systemObjectEntity.GetDataBlob<SystemBodyDB>());
        }

        private void StarSetup(StarInfoDB starInfo)
        {
            switch (starInfo.SpectralType)
            {
                case SpectralType.A:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                default:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
            }

            double maxTempValue = 60000; //todo get this from game settings?
            double divisor = maxTempValue / 255;
            byte temp = Convert.ToByte(starInfo.Temperature / divisor);

            ItemColour = new Color4(temp, 100 - temp, 0, 0);//stab in the dark.

        }
        private void Planetetup(SystemBodyDB sysBodyInfo)
        {
            //maybe look at proceduraly genning textures? also look at diferentiating an icon (zoomed out) and a textue (zoomed right in)
            switch (sysBodyInfo.Type)
            {
                case BodyType.Asteroid:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.Comet:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.DwarfPlanet:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.GasDwarf:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.GasGiant:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.IceGiant:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.Moon:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                case BodyType.Terrestrial:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
                default:
                    {
                        ItemTextureName = DEFAULT_PLANET_ICON;
                    }
                    break;
            }
        }

    }
}
