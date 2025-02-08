using System;
using Pulsar4X.Client.Rendering;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI;
public class PointOfInterestIcon : Icon
{
    public PointOfInterestIcon(PositionDB positionDB) : base(positionDB)
    {
        BasicShape();
        OnPhysicsUpdate();
    }

    void BasicShape()
    {
        //For now we're just going to use a simple cheveron to represent ships, make something fancier in the future
        //by somone who has some design mojo.
        byte r = 115;
        byte g = 115;
        byte b = 115;
        byte a = 165;
        Vector2[] points = {
            new Vector2() { X = 0, Y = 5 },
            new Vector2() { X = 5, Y = 0 },
            new Vector2() { X = 0, Y = -5 },
            new Vector2() { X = -5, Y = 0 },
            new Vector2() { X = 0, Y = 5 }
        };

        SDL.SDL_Color colour = new SDL.SDL_Color() { r = r, g = g, b = b, a = a };
        Shapes.Add(new Shape() { Points = points, Color = colour });
    }

    public override void OnFrameUpdate(Camera2 camera)
    {
        Scale = 1f;
        Scale /= camera.Zoom;
        base.OnFrameUpdate(camera);
    }

    public override void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        Scale = 1f;
        Scale /= camera.ZoomLevel;
        base.OnFrameUpdate(matrix, camera);
        // var scaledPosition = camera.ScaledPosition(WorldPosition_m);

        // var mirrorMatrix = Matrix.IDMirror(true, false);
        // var scaleMatrix = Matrix.IDScale(Scale, Scale);
        // //var rotateMatrix = Matrix.IDRotate(Heading - Math.PI * 0.5);//because the icons were done facing up, but angles are referenced from the right
        // var posMatrix = Matrix.IDTranslate(scaledPosition.X, scaledPosition.Y);

        // var shipMatrix = mirrorMatrix * scaleMatrix * posMatrix;

        // ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);

        // DrawShapes = new Shape[this.Shapes.Count];
        // for (int i = 0; i < Shapes.Count; i++)
        // {
        //     var shape = Shapes[i];

        //     DrawShapes[i] = new Shape()
        //     {
        //         Points = shipMatrix.TransformToVector2(shape.Points),
        //         Color = shape.Color
        //     };

        //     // Vector2[] drawPoints = new Vector2[shape.Points.Length];
        //     // for (int j = 0; j < shape.Points.Length; j++)
        //     // {
        //     //     drawPoints
        //     //     var tranlsatedPoint = shipMatrix.TransformD(shape.Points[j].X, shape.Points[j].Y);
        //     //     int x = (int)tranlsatedPoint.X;
        //     //     int y = (int)tranlsatedPoint.Y;
        //     //     drawPoints[j] = new Vector2() { X = x, Y = y };
        //     // }
        //     // DrawShapes[i] = new Shape() { Points = drawPoints, Color = shape.Color };
        // }
    }
}