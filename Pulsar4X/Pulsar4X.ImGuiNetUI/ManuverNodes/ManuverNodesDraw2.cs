using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
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
        int i = 0;
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
        return EllipseMath.RadiusFromFocal(a, e, phi, theta);
    }

    private List<Vector2> _points = new List<Vector2>();
    private List<(SDL.SDL_Color color, int count)> _colors = new List<(SDL.SDL_Color color, int count)>();
    private SDL.SDL_Point[] DrawPoints = new SDL.SDL_Point[0];
    public void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        
    }

    public void OnPhysicsUpdate()
    {
        
    }

    public void Draw(IntPtr rendererPtr, Camera camera)
    {
        throw new NotImplementedException();
    }
}