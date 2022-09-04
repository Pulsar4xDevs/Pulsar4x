using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;

namespace Pulsar4X.SDL2UI;

public class RouteTrajectory : Icon
{
    private List<ITrajectorySegment> _segments;

    public RouteTrajectory(IPosition positionDB) : base(positionDB)
    {
    }

    public RouteTrajectory(Vector3 position_m) : base(position_m)
    {
    }

    public override void OnPhysicsUpdate()
    {
        base.OnPhysicsUpdate();
    }

    public override void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        base.OnFrameUpdate(matrix, camera);
    }

    public override void Draw(IntPtr rendererPtr, Camera camera)
    {
        base.Draw(rendererPtr, camera);
    }
}

interface ITrajectorySegment
{
    public void CreatePointArray();
}

public class KeplerSegment : ITrajectorySegment
{
    private KeplerElements _keplerOrbit;
    private DateTime _startTime;
    private DateTime _endTime;
    private Vector2[] _points;
    byte _numberOfArcSegments = 180;
    float _segmentArcSweepRadians;
    private bool IsRetrogradeOrbit;
    
    public void CreatePointArray()
    {
        if (_keplerOrbit.Inclination > 1.5708 && _keplerOrbit.Inclination < 4.71239)
        {
            IsRetrogradeOrbit = true;
        }
        _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
        //_numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
        
        //_drawPoints = new SDL.SDL_Point[_numberOfDrawSegments];
        _points = new Vector2[_numberOfArcSegments + 1];

        var loAN = -_keplerOrbit.LoAN;
        var incl = _keplerOrbit.Inclination;
        var mtxloan = Matrix3d.IDRotateZ(loAN);
        var mtxincl = Matrix3d.IDRotateX(incl); 
        var mtxaop = Matrix3d.IDRotateZ(_keplerOrbit.AoP);
        var lop = OrbitMath.GetLongditudeOfPeriapsis(_keplerOrbit.Inclination, _keplerOrbit.AoP, _keplerOrbit.LoAN);
        var _loP_radians = (float)lop;
        //var mtx =  mtxaop * mtxincl * mtxloan;
        var mtx =  mtxaop * mtxloan;
        double angle = 0;

        var coslop = 1 * Math.Cos(_loP_radians);
        var sinlop = 1 * Math.Sin(_loP_radians);
        //TODO: figure out propper matrix rotations for this, will be a bit more elegent. 
        for (int i = 0; i < _numberOfArcSegments + 1; i++)
        {

            double x1 = _keplerOrbit.SemiMajorAxis *  Math.Sin(angle) - _keplerOrbit.LinearEccentricity; //we add the focal distance so the focal point is "center"
            double y1 = _keplerOrbit.SemiMinorAxis * Math.Cos(angle);
            
            double x2 = (x1 * coslop) - (y1 * sinlop);
            double y2 = (x1 * sinlop) + (y1 * coslop);
            
            //Vector3 pnt = new Vector3(x1, y1, 0);
            //pnt = mtx.Transform(pnt);
            //Points[i] = new PointD() {X = pnt.X, Y = pnt.Y};
            _points[i] = new Vector2() { X = x2, Y = y2 };
            angle += _segmentArcSweepRadians;
        }

        if (IsRetrogradeOrbit)
        {
            var mtxr1 = Matrix3d.IDRotateZ(-_loP_radians);
            var mtxr2 = Matrix3d.IDRotateZ(_loP_radians);
            var mtxr = mtxr1 * mtxincl * mtxr2;
            for (int i = 0; i < _points.Length; i++)
            {
                var pnt = mtxr.Transform(new Vector3(_points[i].X, _points[i].Y, 0));
                    _points[i] = new Vector2() {X = pnt.X, Y = pnt.Y};
            }
        }
    }


    
}