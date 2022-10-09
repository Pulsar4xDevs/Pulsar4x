using System;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using Pulsar4X.SDL2UI.ManuverNodes;

namespace Pulsar4X.SDL2UI;

public class KeplerSegment : ITrajectorySegment
{
    internal ManuverNode _node;
    private KeplerElements _keplerOrbit;
    private DateTime _startTime;
    private DateTime _endTime;
    public Vector2[] Points;
    byte _numberOfArcSegments = 180;
    float _segmentArcSweepRadians;
    private bool IsRetrogradeOrbit;
    public IPosition ParentPositionDB;
    //public IPosition StartPositionDB;
    //public OrbitMath StartPosition
    public int IndexStart = 0;
    public int IndexEnd = 0;
    public int IndexCount = 0;

    public TimeSpan SegmentTimeSpan
    {
        get { return _endTime - _startTime; }
    }
    
    public KeplerSegment(ManuverNode node, IPosition ParentPositionDB)
    {
        _node = node;
        this.ParentPositionDB = ParentPositionDB;
        _keplerOrbit = node.TargetOrbit;
        _startTime = node.NodeTime;
        CreatePointArray();
        SetEndTime(_startTime + TimeSpan.FromSeconds(_keplerOrbit.Period));
        
    }

    public void FullUpdate()
    {
        _keplerOrbit = _node.TargetOrbit;
        CreatePointArray();
        SetEndTime(_endTime);
    }

    public void SetEndTime(DateTime endTime)
    {
        _endTime = endTime;
        //we find the point in the ellipse which is closesed to startTime  and end time:
        var startPos = (Vector2)OrbitMath.GetRelativePosition(_keplerOrbit, _startTime);
        var endPos = (Vector2)OrbitMath.GetRelativePosition(_keplerOrbit, _endTime);
        
        double minDistStart = (startPos - Points[0]).Length();
        double minDistEnd = minDistStart;
        for (int i =0; i < Points.Length; i++)
        {
            double distStart = (startPos - Points[i]).Length();
            if (distStart < minDistStart)
            {
                minDistStart = distStart;
                IndexStart = i;
            }
            double distEnd = (endPos - Points[i]).Length();
            if (distEnd < minDistEnd)
            {
                minDistEnd = distEnd;
                IndexEnd = i;
            }
        }
        
        if (IndexStart < IndexEnd)
            IndexCount = IndexEnd - IndexStart;
        else
            IndexCount = Points.Length - (IndexStart - IndexEnd);
    }

    public void CreatePointArray()
    {
        if (_keplerOrbit.Inclination > 1.5708 && _keplerOrbit.Inclination < 4.71239)
        {
            IsRetrogradeOrbit = true;
        }
        _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
        Points = new Vector2[_numberOfArcSegments + 1];

        var loAN = -_keplerOrbit.LoAN;
        var incl = _keplerOrbit.Inclination;
        var mtxloan = Matrix3d.IDRotateZ(loAN);
        var mtxincl = Matrix3d.IDRotateX(incl); 
        var mtxaop = Matrix3d.IDRotateZ(_keplerOrbit.AoP);
        var lop = OrbitMath.GetLongditudeOfPeriapsis(_keplerOrbit.Inclination, _keplerOrbit.AoP, _keplerOrbit.LoAN);
        var _loP_radians = (float)lop;
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
            Points[i] = new Vector2() { X = x2, Y = y2 };
            angle -= _segmentArcSweepRadians;
        }

        if (IsRetrogradeOrbit)
        {
            var mtxr1 = Matrix3d.IDRotateZ(-_loP_radians);
            var mtxr2 = Matrix3d.IDRotateZ(_loP_radians);
            var mtxr = mtxr1 * mtxincl * mtxr2;
            for (int i = 0; i < Points.Length; i++)
            {
                var pnt = mtxr.Transform(new Vector3(Points[i].X, Points[i].Y, 0));
                Points[i] = new Vector2() {X = pnt.X, Y = pnt.Y};
            }
        }
    }

}