using System;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    class SysBodyIcon : Icon
    {
        SystemBodyInfoDB _systemBodyInfoDB;
        BodyType _bodyType;
        MassVolumeDB _massVolDB;
        Random _rng;

        public SysBodyIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
        {
            _positionDB = entity.GetDataBlob<PositionDB>();
            Setup(entity);
        }

        void Setup(Entity entity)
        {

            _systemBodyInfoDB = entity.GetDataBlob<SystemBodyInfoDB>();
            _bodyType = _systemBodyInfoDB.BodyType;
            _massVolDB = entity.GetDataBlob<MassVolumeDB>();

            _rng = new Random(entity.Guid.GetHashCode()); //use entity guid as a seed for psudoRandomness. 

            switch (_bodyType)
            {
                case BodyType.Asteroid:
                    Asteroid();
                    break;
                case BodyType.Terrestrial:
                    Terestrial();
                    break;
                default:
                    Unknown();
                    break;
            }

            if (entity.HasDataBlob<AtmosphereDB>())
            {

            }
            if (_systemBodyInfoDB.Colonies.Count > 0)
            {

            }
        }

        void Terestrial()
        {

            int vertDiameter = 8;
            int horDiameter = 8;
            int segments = 32;
            var points = CreatePrimitiveShapes.CreateArc(0, 0, horDiameter, vertDiameter, 0, Math.PI * 2, segments);


            //colors picked out of my ass for a blue/green look. 
            //TODO: use minerals for this? but migth not have that info. going to have to work in with sensor stuff. 
            byte r = 0;
            byte g = 100;
            byte b = 100;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Color = colour, Points = points });
        }

        void Asteroid()
        {
            int vertDiameter = _rng.Next(4, 8);
            int horDiameter = _rng.Next(4, 8);
            int segments = _rng.Next(8, 32);
            int jagMax = _rng.Next(5, 8);
            int jagMin = _rng.Next(4, jagMax);

            var points = CreatePrimitiveShapes.CreateArc(0, 0, horDiameter, vertDiameter, 0, Math.PI * 2, segments);
            for (int i = 0; i < segments; i = i + 2)
            {
                //this is not right, need to pull the points in towards the center, not just pull them left. 
                double x = points[i].X - _rng.Next(jagMin, jagMax);
                double y = points[i].Y - _rng.Next(jagMin, jagMax);
                points[i] = new PointD() { X = x, Y = y };
            }
            //colors picked out of my ass for a brown look. 
            //TODO: use minerals for this? but migth not have that info. going to have to work in with sensor stuff. 
            byte r = 150;
            byte g = 100;
            byte b = 50;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Color = colour, Points = points });
        }

        void Unknown()
        {
            int vertDiameter = 8;
            int horDiameter = 8;
            int segments = 8;
            var points = CreatePrimitiveShapes.CreateArc(0, 0, horDiameter, vertDiameter, 0, Math.PI * 2, segments);

            //colors picked out of my ass for a blue/green look. 
            //TODO: use minerals for this? but migth not have that info. going to have to work in with sensor stuff. 
            byte r = 100;
            byte g = 100;
            byte b = 100;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Color = colour, Points = points });
        }

        /*
        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            var bodyRadiusAU = _massVolDB.Radius;
            var viewRadius = camera.ViewDistance(bodyRadiusAU);
            if (viewRadius < 8)
                ShapesScaleWithZoom = false;
            else
                ShapesScaleWithZoom = true;
            base.OnFrameUpdate(matrix, camera);

        }*/
    }
}

