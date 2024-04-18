using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using SDL2;
using Pulsar4X.Messaging;
using System.Threading.Tasks;

namespace Pulsar4X.SDL2UI
{
    public class ShipIcon : Icon
    {
        OrbitDB? _orbitDB;
        NewtonMoveDB? _newtonMoveDB;
        float _lop;
        Entity? _entity;
        public ShipIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                _orbitDB = entity.GetDataBlob<OrbitDB>();
                var i = _orbitDB.Inclination;
                var aop = _orbitDB.ArgumentOfPeriapsis;
                var loan = _orbitDB.LongitudeOfAscendingNode;
                _lop = (float)OrbitMath.GetLongditudeOfPeriapsis(i, aop, loan);
            }
            else if(entity.HasDataBlob<NewtonMoveDB>())
            {
                _newtonMoveDB = entity.GetDataBlob<NewtonMoveDB>();
            }

            Func<Message, bool> filterById = msg => msg.EntityId == entity.Id;

            MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, OnDBAdded, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, OnDBRemoved, filterById);

            _entity = entity;
            BasicShape();
            OnPhysicsUpdate();
        }

        public ShipIcon(PositionDB position) : base(position)
        {
            _entity = position.OwningEntity;
            Front(60, 100, 0, -110);
            Cargo(160, 160, 0, -120);
            Wings(260, 260, 80, 50, 0, 0);
            Reactors(100, 100, 0, 90);
            Engines(100, 60, 0, 130);
        }

        async Task OnDBAdded(Message message)
        {
            await Task.Run(() =>
            {
                if (message.DataBlob is OrbitDB)
                {
                    _orbitDB = (OrbitDB)message.DataBlob;
                    var i = _orbitDB.Inclination;
                    var aop = _orbitDB.ArgumentOfPeriapsis;
                    var loan = _orbitDB.LongitudeOfAscendingNode;
                    _lop = (float)OrbitMath.GetLongditudeOfPeriapsis(i, aop, loan);
                }
                else if (message.DataBlob is NewtonMoveDB)
                {
                    _newtonMoveDB = (NewtonMoveDB)message.DataBlob;
                    //NewtonVectors();
                }
            });
        }

        async Task OnDBRemoved(Message message)
        {
            await Task.Run(() =>
            {
                if (message.DataBlob is OrbitDB)
                    _orbitDB = null;
                else if (message.DataBlob is NewtonMoveDB)
                {
                    _newtonMoveDB = null;
                    //Shapes.RemoveAt(Shapes.Count-1);
                }
            });
        }

        void BasicShape()
        {
            //TODO break the vertical up depending on percentage of ship dedicated to each thing.
            //Front(6, 10, 0, -11);
            //Cargo(16, 16, 0, -12);
            //Wings(26, 26, 8, 5, 0, 0);
            //Reactors(10, 10, 0, 9);
            //Engines(10, 6, 0, 13);

            //For now we're just going to use a simple cheveron to represent ships, make something fancier in the future
            //by somone who has some design mojo.
            byte r = 50;
            byte g = 50;
            byte b = 200;
            byte a = 255;
            Orbital.Vector2[] points = {
            new Orbital.Vector2() { X = 0, Y = 5 },
            new Orbital.Vector2() { X = 5, Y = -5 },
            new Orbital.Vector2() { X = 0, Y = 0 },
            new Orbital.Vector2() { X = -5, Y = -5 },
            new Orbital.Vector2() { X = 0, Y = 5 }
            };

            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Points = points, Color = colour });
        }
        void Front(int width, int height, int offsetX, int offsetY) //crew
        {

            var points = CreatePrimitiveShapes.CreateArc(offsetX, offsetY, width * 0.5 , height * 0.5, CreatePrimitiveShapes.QuarterCircle, CreatePrimitiveShapes.HalfCircle, 16);
            byte r = 0;
            byte g = 100;
            byte b = 100;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() { Points = points, Color = colour });

        }

        void Cargo(int width, int height, int offsetX, int offsetY)//and fuel
        {
            byte r = 0;
            byte g = 0;
            byte b = 200;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };

            //TODO: change numbers depending on number of cargo containing components.
            int numberofPodsX = 4;
            int numberofPodsY = 2;

            int podWidth = width / numberofPodsX;
            int offsetx1 = (int)(offsetX - width * 0.5f + podWidth * 0.5);

            int podHeight = height / numberofPodsY;
            int offsety1 = (int)(offsetY + podHeight * 0.5);

            for (int podset = 0; podset < numberofPodsY; podset++)
            {
                offsety1 += podset * podHeight;

                int offsetx2 = offsetx1 - podWidth;

                for (int i = 0; i < numberofPodsX; i++)
                {
                    offsetx2 += podWidth;
                    Shape shape = new Shape() { Color = colour, Points = CreatePrimitiveShapes.RoundedCylinder(podWidth, height / numberofPodsY, offsetx2, offsety1) };
                    Shapes.Add(shape);
                }
            }
        }

        void Wings(int width, int height, int frontWidth, int backWidth, int offsetX, int offsetY)//FTL & guns
        {
            byte r = 84;
            byte g = 84;
            byte b = 84;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };


            Vector2 p0 = new Orbital.Vector2() { X = offsetX, Y = (int)(offsetY - height * 0.5) };
            Vector2 p1 = new Vector2() { X = offsetX + frontWidth, Y = (int)(offsetY - height * 0.5) };
            Vector2 p2 = new Vector2() { X = (int)(offsetX + width * 0.5), Y = (int)(offsetY - height * 0.3) };
            Vector2 p3 = new Vector2() { X = (int)(offsetX + width * 0.5), Y = -(int)(offsetY - height * 0.25) };
            Vector2 p4 = new Vector2() { X = offsetX + backWidth, Y = -(int)(offsetY - height * 0.5) };
            Vector2 p5 = new Vector2() { X = offsetX, Y = -(int)(offsetY - height * 0.5) };
            Vector2 p6 = new Vector2() { X = offsetX - backWidth, Y = (int)(offsetY + height * 0.5) };
            Vector2 p7 = new Vector2() { X = (int)(offsetX + -width * 0.5), Y = (int)(offsetY + height * 0.25) };
            Vector2 p8 = new Vector2() { X = (int)(offsetX + -width * 0.5), Y = -(int)(offsetY + height * 0.3) };
            Vector2 p9 = new Vector2() { X = offsetX - frontWidth, Y = -(int)(offsetY + height * 0.5) };
            Vector2 p10 = new Vector2() { X = offsetX, Y = -(int)(offsetY + height * 0.5) };
            var shape = new Shape() { Color = colour, Points = new Orbital.Vector2[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 } };
            Shapes.Add(shape);


        }
        void Reactors(int width, int height, int offsetX, int offsetY)
        {
            byte r = 100;
            byte g = 0;
            byte b = 0;
            byte a = 255;
            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };

            var shape = new Shape() { Color = colour, Points = CreatePrimitiveShapes.CreateArc(offsetX, offsetY, (int)(width * 0.5), (int)(height * 0.5), 0, CreatePrimitiveShapes.PI2, 12) };

            Shapes.Add(shape);

        }
        void Engines(int width, int height, int offsetX, int offsetY)
        {
            byte r1 = 200;
            byte g1 = 200;
            byte b1 = 200;
            byte a1 = 255;
            SDL.SDL_Color colourbox = new SDL.SDL_Color() { r = r1, g = g1, b = b1, a = a1 };

            byte r2 = 100;
            byte g2 = 150;
            byte b2 = 0;
            byte a2 = 255;
            SDL.SDL_Color colourCone = new SDL.SDL_Color() { r = r2, g = g2, b = b2, a = a2 };

            int thrusterCount = 3;
            int twidth = width / thrusterCount;
            int toffset = (int)(offsetX - width * 0.5f + twidth * 0.5);

            for (int i = 0; i < thrusterCount; i++)
            {
                int boxHeight = height / 3;
                int boxWidth = twidth;
                int coneHeight = height - boxHeight;
                Shapes.Add(new Shape() { Color = colourbox, Points = CreatePrimitiveShapes.Rectangle(toffset, (int)(offsetY + boxHeight * 0.5), boxWidth, boxHeight, CreatePrimitiveShapes.PosFrom.Center) });
                Shapes.Add(new Shape() { Color = colourCone, Points = CreatePrimitiveShapes.CreateArc(toffset, offsetY + boxHeight + coneHeight, (int)(boxWidth * 0.5), coneHeight, CreatePrimitiveShapes.QuarterCircle, CreatePrimitiveShapes.HalfCircle, 8) });
                toffset += twidth;
            }
        }

        // void NewtonVectors()
        // {
        //     byte r = 100;
        //     byte g = 50;
        //     byte b = 200;
        //     byte a = 255;
        //     SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
        //     var len = 0.00001 * _newtonMoveDB.OwningEntity.GetDataBlob<NewtonThrustAbilityDB>().ThrustInNewtons;
        //     var dv = _newtonMoveDB.ManuverDeltaV;
        //     var line = Vector3.Normalise(dv) * len ;
        //     Vector2[] points = new Vector2[2];
        //     points[0]= Vector2.Zero;
        //     points[1] = new Vector2(line.X, line.Y);
        //     var shape = new Shape() { Color = colour, Points = points };

        //     Shapes.Add(shape);
        // }



        public override void OnPhysicsUpdate()
        {
            if(_entity is null) return;

            try
            {
                var headingVector = _entity.GetRelativeState().Velocity;
                var heading = Math.Atan2(headingVector.Y, headingVector.X);
                Heading = (float)heading;
            }
            catch(Exception)
            {
                // I don't think we need to do anything here
            }
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            var mirrorMatrix = Matrix.IDMirror(true, false);
            var scaleMatrix = Matrix.IDScale(Scale, Scale);
            var rotateMatrix = Matrix.IDRotate(Heading - Math.PI * 0.5);//because the icons were done facing up, but angles are referenced from the right

            var shipMatrix = mirrorMatrix * scaleMatrix * rotateMatrix;

            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);

            DrawShapes = new Shape[this.Shapes.Count];
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                Vector2[] drawPoints = new Vector2[shape.Points.Length];
                for (int i2 = 0; i2 < shape.Points.Length; i2++)
                {
                    var tranlsatedPoint = shipMatrix.TransformD(shape.Points[i2].X, shape.Points[i2].Y);
                    int x = (int)(ViewScreenPos.x + tranlsatedPoint.X );
                    int y = (int)(ViewScreenPos.y + tranlsatedPoint.Y );
                    drawPoints[i2] = new Vector2() { X = x, Y = y };
                }
                DrawShapes[i] = new Shape() { Points = drawPoints, Color = shape.Color };
            }
        }
    }

    public class ProjectileIcon : Icon
    {
        OrbitDB? _orbitDB;
        float _lop;
        Entity? _entity;
        private Shape _flame;
        public ProjectileIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
        {
            _entity = entity;
            BasicShape();
            NewtonFlame();

            if (entity.HasDataBlob<OrbitDB>())
            {
                _orbitDB = entity.GetDataBlob<OrbitDB>();
                var i = _orbitDB.Inclination;
                var aop = _orbitDB.ArgumentOfPeriapsis;
                var loan = _orbitDB.LongitudeOfAscendingNode;
                _lop = (float)OrbitMath.GetLongditudeOfPeriapsis(i, aop, loan);
            }
            else if(entity.HasDataBlob<NewtonMoveDB>())
            {
                Shapes.Add(_flame);
            }

            Func<Message, bool> filterById = msg => msg.EntityId.Value == entity.Id;

            MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, DBAdded, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, DBRemoved, filterById);

            OnPhysicsUpdate();
        }

        public ProjectileIcon(Vector3 position_m) : base(position_m)
        {
        }

        async Task DBAdded(Message message)
        {
            await Task.Run(() =>
            {
                if (message.DataBlob is OrbitDB)
                {
                    _orbitDB = (OrbitDB)message.DataBlob;
                    var i = _orbitDB.Inclination;
                    var aop = _orbitDB.ArgumentOfPeriapsis;
                    var loan = _orbitDB.LongitudeOfAscendingNode;
                    _lop = (float)OrbitMath.GetLongditudeOfPeriapsis(i, aop, loan);
                }
                else if (message.DataBlob is NewtonMoveDB)
                {
                    if(!Shapes.Contains(_flame))
                        Shapes.Add(_flame);
                }
            });
        }

        async Task DBRemoved(Message message)
        {
            await Task.Run(() =>
            {
                if (message.DataBlob is OrbitDB)
                    _orbitDB = null;
                if (message.DataBlob is NewtonMoveDB)
                {
                    if (Shapes.Contains(_flame))
                        Shapes.Remove(_flame);
                }
            });
        }

        void BasicShape()
        {
            byte r = 150;
            byte g = 50;
            byte b = 200;
            byte a = 255;
            Vector2[] points = {
                new Vector2 { X = 0, Y = 4 },
                new Vector2 { X = 2, Y = -4 },
                new Vector2 { X = 0, Y = 0 },
                new Vector2 { X = -2, Y = -4 },
                new Vector2 { X = 0, Y = 4 }
            };

            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            Shapes.Add(new Shape() {Points = points, Color = colour});
        }

        void NewtonFlame()
        {
            byte r = 150;
            byte g = 50;
            byte b = 0;
            byte a = 200;
            Vector2[] points = {
                new Vector2 { X = 0, Y = 0 },
                new Vector2 { X = -2, Y = -2 },
                new Vector2 { X = 0, Y = -5 },
                new Vector2 { X = 2, Y = -2 },
                new Vector2 { X = 0, Y = 0 }
            };

            SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
            _flame = new Shape() {Points = points, Color = colour};
        }

        public override void OnPhysicsUpdate()
        {
            if(_entity is null) return;

            var headingVector = _entity.GetRelativeState().Velocity;//_orbitDB.InstantaneousOrbitalVelocityVector_m(atDateTime);
            var heading = Math.Atan2(headingVector.Y, headingVector.X);
            Heading = (float)heading;
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            var mirrorMatrix = Matrix.IDMirror(true, false);
            var scaleMatrix = Matrix.IDScale(Scale, Scale);
            var rotateMatrix = Matrix.IDRotate(Heading - Math.PI * 0.5);//because the icons were done facing up, but angles are referenced from the right

            var shipMatrix = mirrorMatrix * scaleMatrix * rotateMatrix;

            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);

            DrawShapes = new Shape[this.Shapes.Count];
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                Vector2[] drawPoints = new Vector2[shape.Points.Length];
                for (int i2 = 0; i2 < shape.Points.Length; i2++)
                {
                    var tranlsatedPoint = shipMatrix.TransformD(shape.Points[i2].X, shape.Points[i2].Y);
                    int x = (int)(ViewScreenPos.x + tranlsatedPoint.X );
                    int y = (int)(ViewScreenPos.y + tranlsatedPoint.Y );
                    drawPoints[i2] = new Vector2() { X = x, Y = y };
                }
                DrawShapes[i] = new Shape() { Points = drawPoints, Color = shape.Color };
            }
        }

    }


    public class BeamIcon : Icon
    {
        BeamInfoDB? _beamInfo;
        public BeamIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
        {
            _beamInfo = entity.GetDataBlob<BeamInfoDB>();
            OnPhysicsUpdate();
        }

        public BeamIcon(Vector3 position_m) : base(position_m)
        {
        }

        public override void OnPhysicsUpdate()
        {
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            if(_beamInfo is null) return;

            var p0 = camera.ViewCoordinate_m(_beamInfo.Positions[0]);
            var p1 = camera.ViewCoordinate_m(_beamInfo.Positions[1]);

            DrawShapes = new Shape[1];
            var s1 = new Shape();
            s1.Points = new Vector2[2];
            s1.Points[0] = new Vector2() {X = p0.x, Y = p0.y};
            s1.Points[1] = new Vector2() {X = p1.x, Y = p1.y};
            var clr = new SDL.SDL_Color()
            {
                r = 200,
                g = 0,
                b = 0,
                a = 255
            };
            s1.Color = clr;
            DrawShapes[0] = s1;
        }
    }
}
