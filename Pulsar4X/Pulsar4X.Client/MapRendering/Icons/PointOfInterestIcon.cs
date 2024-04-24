using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI;
public class PointOfInterestIcon : Icon
{
    Entity? _entity;
    public PointOfInterestIcon(Entity entity) : base(entity.GetDataBlob<PositionDB>())
    {
        _entity = entity;
        BasicShape();
        OnPhysicsUpdate();
    }

    public PointOfInterestIcon(PositionDB position) : base(position)
    {
        _entity = position.OwningEntity;
    }

    void BasicShape()
    {
        //For now we're just going to use a simple cheveron to represent ships, make something fancier in the future
        //by somone who has some design mojo.
        byte r = 115;
        byte g = 115;
        byte b = 115;
        byte a = 165;
        Orbital.Vector2[] points = {
            new Orbital.Vector2() { X = 0, Y = 5 },
            new Orbital.Vector2() { X = 5, Y = 0 },
            new Orbital.Vector2() { X = 0, Y = -5 },
            new Orbital.Vector2() { X = -5, Y = 0 },
            new Orbital.Vector2() { X = 0, Y = 5 }
        };

        SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
        Shapes.Add(new Shape() { Points = points, Color = colour });
    }
    
    public override void OnPhysicsUpdate()
    {
        if(_entity is null) return;
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