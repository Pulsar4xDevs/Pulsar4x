using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using Pulsar4X.SDL2UI.ManuverNodes;
using SDL2;

namespace Pulsar4X.SDL2UI;

public class RouteTrajectory : IDrawData
{
    private List<ITrajectorySegment> _segments = new List<ITrajectorySegment>();
    private List<ManuverNode> _nodes = new List<ManuverNode>();

    private SDL.SDL_Point[] DrawPoints = new SDL.SDL_Point[0];
    
    
    ECSLib.IPosition _positionDB;
    Orbital.Vector3 _worldPosition_m { get; set; }
    public Orbital.Vector3 WorldPosition_m
    {
        get 
        {
            return _positionDB.AbsolutePosition + _worldPosition_m;
        }
        set 
        { 
            _worldPosition_m = value; 
        }
    }

    public KeplerSegment GetSegment(int index)
    {
        return (KeplerSegment)_segments[index];
    }
    
    public RouteTrajectory(Entity entity)
    {
        var positionDB = entity.GetDataBlob<PositionDB>();
       // var parentPosDB = entity.GetSOIParentPositionDB();
        //ManuverNode node = new ManuverNode(entity, entity.StarSysDateTime);
        //AddNode(node, parentPosDB);
        
        _positionDB = positionDB;

    }

    public RouteTrajectory(Entity entity, ManuverNode node)
    {
        var positionDB = entity.GetDataBlob<PositionDB>();
        var parentPosDB = entity.GetSOIParentPositionDB();
        AddNode(node, parentPosDB);
        
        _positionDB = positionDB;

    }

    public void AddNode(ManuverNode node, IPosition parentPositionDB)
    {
        _segments.Add(new KeplerSegment(node, parentPositionDB));
        _nodes.Add(node);
    }

    public int Count
    {
        get { return _nodes.Count; }
    }

    public void UpdateNode(int index)
    {
        _segments[index].FullUpdate();
        _camHash = 0;
    }


    public  void OnPhysicsUpdate()
    {
        
    }

    private int _camHash = 0;

    public void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        var camHash = camera.GetHashCode();
        if (_camHash == camHash) //if the camera position or zoom has not changed, don't bother doing stuff
            return;
        _camHash = camHash;

        int numPoints = 0;
        foreach (ITrajectorySegment segment in _segments)
        {
            var seg = (KeplerSegment)segment;
            numPoints += seg.IndexCount;
        }

        if (DrawPoints.Length != numPoints)
            DrawPoints = new SDL.SDL_Point[numPoints];

        foreach (ITrajectorySegment segment in _segments)
        {
            var seg = (KeplerSegment)segment;

            //resize for zoom
            //translate to position
            
            var foo = camera.ViewCoordinateV2_m(seg.ParentPositionDB.AbsolutePosition); //camera position and zoom
            
            var trns = Matrix.IDTranslate(foo.X, foo.Y);
            var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
            var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom

            int index = seg.IndexStart;
            var spos = camera.ViewCoordinateV2_m(seg.ParentPositionDB.AbsolutePosition + seg._node.NodePosition);
            //_drawPoints[0] = mtrx.TransformToSDL_Point(_bodyrelativePos.X, _bodyrelativePos.Y);
            DrawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
            for (int i = 1; i < seg.IndexCount; i++)
            {
                if (index < seg.Points.Length - 1)

                    index++;
                else
                    index = 0;
                
                DrawPoints[i] = mtrx.TransformToSDL_Point(seg.Points[index].X, seg.Points[index].Y);
            }
        }

    }

    SDL.SDL_Color colrGold = new SDL.SDL_Color()
    {
        r = 255,
        g = 215,
        b = 0,
        a = 255
    };
    public  void Draw(IntPtr rendererPtr, Camera camera)
    {
        SDL.SDL_SetRenderDrawColor(rendererPtr, colrGold.r, colrGold.g, colrGold.b, colrGold.a);

        SDL.SDL_RenderDrawLines(rendererPtr, DrawPoints, DrawPoints.Length);
        
    }
}

interface ITrajectorySegment
{
    public void CreatePointArray();
    public void FullUpdate();
}

public class ManuverLinesComplete : IDrawData
{
    public ManuverSequence SelectedSequence;
    public ManuverSequence RootSequence = new ManuverSequence();
    public KeplerElements[] OrbitSegments = new KeplerElements[0];
    public double[] OrbitSegmentsSeconds = new double[0];
    public int[] OrbitSegmentEndIndex = new int[0];

    public ManuverNode[] EditingNodes = new ManuverNode[0];
    public int EditingOrbitIndex = 0;
    public int EditingNodesCount
    {
        get { return EditingNodes.Length; }
    }

    public ManuverLinesComplete()
    {
        SelectedSequence = RootSequence;
        RootSequence.SequenceName = "Manuvers";
    }

    SDL.SDL_Color editClr = new SDL.SDL_Color()
    {
        r = 255,
        g = 215,
        b = 0,
        a = 255
    };
    SDL.SDL_Color obtClr = new SDL.SDL_Color()
    {
        r = 0,
        g = 215,
        b = 0,
        a = 255
    };


    private Vector2[] points = new Vector2[0];
    private SDL.SDL_Point[] DrawPoints = new SDL.SDL_Point[0];
    public void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        points = RenderManuverLines.CreatePointArray(RootSequence);
        if (DrawPoints.Length != points.Length)
            DrawPoints = new SDL.SDL_Point[points.Length];

        var foo = camera.ViewCoordinateV2_m(RootSequence.ParentPosition.AbsolutePosition); //camera position and zoom
            
        var trns = Matrix.IDTranslate(foo.X, foo.Y);
        var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
        var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom
        
        for (int i = 0; i < points.Length; i++)
        {
            DrawPoints[i] = mtrx.TransformToSDL_Point(points[i].X, points[i].Y);
        }
        
    }

    public void OnPhysicsUpdate()
    {
        
    }

    public void Draw(IntPtr rendererPtr, Camera camera)
    {
        SDL.SDL_SetRenderDrawColor(rendererPtr, obtClr.r, obtClr.g, obtClr.b, obtClr.a);

        SDL.SDL_RenderDrawLines(rendererPtr, DrawPoints, DrawPoints.Length);
    }
}

public static class RenderManuverLines
{
    public static List<(KeplerElements ke, double startAngle)> GetData(ManuverSequence manuverSequence)
    {
        List<(KeplerElements ke, double startAngle)> list = new List<(KeplerElements ke, double startAngle)>();
        foreach (var node in manuverSequence.ManuverNodes)
        {
            var tgtOrbit = node.TargetOrbit;
            list.Add((tgtOrbit, node.GetNodeAnomaly));
        }

        foreach (var manSeq in manuverSequence.ManuverSequences)
        {
            list.AddRange(GetData(manSeq));
        }

        return list;
    }

    public static Vector2[] CreatePointArray(ManuverSequence manuverSequence)
    {
        int res = 128;
        var data = GetData(manuverSequence);
        Vector2[] pointArray = new Vector2[data.Count * res];


        foreach (var item in data)
        {
            double xc = 0;
            double yc = 0;
            double a = item.ke.SemiMajorAxis;
            double b = item.ke.SemiMinorAxis;

            Array.Copy(CreatePrimitiveShapes.ElipsePoints(xc, yc, 0, a, b, res), pointArray, res);

        }

        return pointArray;
    }
    

}