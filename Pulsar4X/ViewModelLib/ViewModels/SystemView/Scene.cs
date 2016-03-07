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


        public Dictionary<Guid, SystemObjectRenderInfo> SystemBodies { get; set; }

        public Scene(List<Vector3> position_data, List<float> scale_data, Mesh mesh, Camera camera)
        {
            this.position_data = position_data;
            this.scale_data = scale_data;
            this.mesh = mesh;
            this.camera = camera;
        }

        public Scene(StarSystem starSys, AuthenticationToken authToken)
        {
            foreach(var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>(authToken))
            {
                SystemBodies.Add(item.Guid, new SystemObjectRenderInfo(item));
            }
        }
    }


    public class SystemObjectRenderInfo : ViewModelBase
    {
        public DateTime _currentTime;
        public DateTime CurrentTime { get { return _currentTime; } set { _currentTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(Position)); } }

        private Entity _systemBody;
        private StarInfoDB _starInfo;
        private SystemBodyDB _sysBodyInfo;
        private Entity _parentEntity;

        SystemObjectRenderInfo _parentObject;

        GLUtilities.GLQuad Graphic { get; set; } 
        // create name lable:
        GLUtilities.GLFont NameLabel { get; set; }

        Vector2 _size;
        
        Vector3 PositionGL { get { return new Vector3((float)Position.X, (float)Position.Y, (float)Position.Z); } } //TODO fix this.use different units for display? 

        Color4 _starColour;

        string _texture;

        public ECSLib.Vector4 Position
        {
            get { return _systemBody.GetDataBlob<PositionDB>().Position; }
        }

        public SystemObjectRenderInfo(Entity systemBody)
        {
            
            _systemBody = systemBody;
            _parentEntity = systemBody.GetDataBlob<OrbitDB>().Parent;

            var glEffect = new GLUtilities.GLEffectBasic21("./Resources/Shaders/Basic20_Vertex_Shader.glsl", "./Resources/Shaders/Basic20_Fragment_Shader.glsl");
            //var glEffect = new GLUtilities.GLEffectBasic30("./Resources/Shaders/Basic30_Vertex_Shader.glsl", "./Resources/Shaders/Basic30_Fragment_Shader.glsl");

            _starInfo.SpectralType

            Graphic = new GLUtilities.GLQuad(glEffect,
                                                        PositionGL,
                                                        _size,
                                                        _starColour,
                                                        _texture);

            NameLabel = new GLUtilities.GLFont(glEffect,
            new Vector3(PositionGL.X, (float)(PositionGL.Y - (oStar.Radius / Constants.Units.KmPerAu)), 0),
            UIConstants.DEFAULT_TEXT_SIZE, Color.White, UIConstants.Textures.DEFAULT_GLFONT2, oStar.Name);


        }


    }
}
