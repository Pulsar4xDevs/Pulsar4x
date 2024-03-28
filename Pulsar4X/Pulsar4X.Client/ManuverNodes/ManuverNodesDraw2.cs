using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using Pulsar4X.SDL2UI;

using SDL2;

namespace Pulsar4X.ImGuiNetUI.ManuverNodes;

public class ManuverNodesDraw2 : IDrawData
{
    private NavSequenceDB _db;

    private SDL.SDL_Color _warpColour = new SDL.SDL_Color()
    {
        r = 0, g = 200, b = 200, a = 100,
    };
    private SDL.SDL_Color _newtStartColor = new SDL.SDL_Color()
    {
        r = 200, g = 200, b = 200, a = 100,
    };
    private SDL.SDL_Color _driftColor = new SDL.SDL_Color()
    {
        r = 200, g = 200, b = 200, a = 100,
    };

    public ManuverNodesDraw2(Entity entity)
    {
        if (entity.TryGetDatablob(out NavSequenceDB db))
            _db = db;
        Setup();
    }

    void Setup()
    {
        _points = new List<Vector2>();
        _colors = new List<(SDL.SDL_Color color, int count)>();
        foreach (var manuver in _db.ManuverNodes)
        {
            var startState = OrbitalMath.GetStateVectors(manuver.StartKepler, manuver.StartDateTime);
            var endState = OrbitalMath.GetStateVectors(manuver.EndKepler, manuver.EndDateTime);

            switch (manuver.TypeOfManuver)
            {
                case Manuver.ManuverType.Warp:
                {
                    _points.Add((Vector2)startState.position);
                    _points.Add((Vector2)endState.position);
                    _colors.Add((_warpColour, 2));
                    break;
                }
                case Manuver.ManuverType.Drift:
                {
                    var a = manuver.EndKepler.SemiMajorAxis;
                    var e = manuver.EndKepler.Eccentricity;
                    var lop = manuver.EndKepler.LoAN + manuver.EndKepler.AoP;
                    var startPos = (Vector2)startState.position;
                    var endPos = (Vector2)endState.position;
                    var kp = CreatePrimitiveShapes.KeplerPoints(a, e, lop, startPos, endPos);
                    _points.AddRange(kp);
                    _colors.Add((_driftColor, kp.Length));
                    break;
                }
                case Manuver.ManuverType.NewtonSimple:
                {
                    var startPos = (Vector2)startState.position;
                    var endPos = (Vector2)endState.position;
                    var sweepAngle = Math.PI * 0.5;
                    var r = RadiusFromFocal(manuver.StartKepler, sweepAngle);
                    var sweepPnt = new Vector2()
                    {
                        X = r * Math.Cos(sweepAngle),
                        Y = r * Math.Sin(sweepAngle)
                    };
                    var kp = CreatePrimitiveShapes.KeplerPoints(manuver.StartKepler, startPos, sweepPnt);
                    _points.AddRange(kp);
                    _colors.Add((_newtStartColor, kp.Length));

                    r = RadiusFromFocal(manuver.EndKepler, sweepAngle);
                    sweepPnt = new Vector2()
                    {
                        X = r * Math.Cos(sweepAngle),
                        Y = r * Math.Sin(sweepAngle)
                    };
                    kp = CreatePrimitiveShapes.KeplerPoints(manuver.EndKepler, endPos, sweepPnt);
                    _points.AddRange(kp);
                    _colors.Add((_driftColor, kp.Length));

                    break;
                }
            }
        }
    }

    double RadiusFromFocal(KeplerElements ke, double theta)
    {
        double a = ke.SemiMajorAxis;
        double e = ke.Eccentricity;
        double phi = ke.LoAN + ke.AoP;
        return EllipseMath.RadiusAtTrueAnomaly(a, e, phi, theta);
    }

    private List<Vector2> _points = new List<Vector2>();
    private List<(SDL.SDL_Color color, int count)> _colors = new List<(SDL.SDL_Color color, int count)>();
    private SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
    
    public void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        /*
        var foo = camera.ViewCoordinateV2_m(WorldPosition_m); //camera position and zoom
            
        var trns = Matrix.IDTranslate(foo.X, foo.Y);
        var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
        var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom

        int index = _index;
        var spos = camera.ViewCoordinateV2_m(_bodyAbsolutePos);

        //_drawPoints[0] = mtrx.TransformToSDL_Point(_bodyrelativePos.X, _bodyrelativePos.Y);
        _drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
        for (int i = 1; i < _numberOfDrawSegments; i++)
        {
            if (index < _numberOfArcSegments - 1)

                index++;
            else
                index = 0;
                
            _drawPoints[i] = mtrx.TransformToSDL_Point(_points[index].X, _points[index].Y);
        }*/
    }

    public void OnPhysicsUpdate()
    {
        if (_drawPoints.Length != _points.Count)
            _drawPoints = new SDL.SDL_Point[_points.Count];
    }

    public void Draw(IntPtr rendererPtr, Camera camera)
    {
        if (_drawPoints.Length < 1)
            return;
        SDL.SDL_Color colour;
        int k = 0;
        for (int i = 0; i < _colors.Count - 1; i++)
        {
            colour = _colors[i].color;
            SDL.SDL_SetRenderDrawColor(rendererPtr, colour.r, colour.g, colour.b, colour.a);
            for (int j = 0; j < _colors[i].count; j++)
            {
                k = i + j;
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[k].x, _drawPoints[k].y, _drawPoints[k + 1].x, _drawPoints[k +1].y);
            }
            
            
        }
    }
}