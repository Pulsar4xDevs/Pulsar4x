using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Pulsar4X.SDL2UI.ManuverNodes;
using SDL2;

namespace Pulsar4X.SDL2UI;

public class ManuverLinesComplete : IDrawData
{
    public ManuverSequence SelectedSequence;
    public ManuverSequence RootSequence = new ManuverSequence();
    public ManuverNode[] EditingNodes = new ManuverNode[0];

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

    public void AddNewNode(Entity orderEntity, DateTime nodeTime)
    {
        ManuverNode newNode = new ManuverNode(orderEntity, nodeTime);
        AddNewNode(newNode);
    }

    public void AddNewNode(ManuverNode node)
    {
        DateTime nodeTime = node.NodeTime;
        var val = RenderManuverLines.FindNodeTime(RootSequence, nodeTime);


        if (val[0].nodeIndex != -1) //if has priorNode
        {
            node.PriorOrbit = val[0].seq.ManuverNodes[val[0].nodeIndex].TargetOrbit;
        }

        if (val[1].nodeIndex != -1) //if has next node
        {
            val[1].seq.ManuverNodes[val[1].nodeIndex].PriorOrbit = node.TargetOrbit;
            SelectedSequence.ManuverNodes.Insert(0,node);
        }
        else
        {
            SelectedSequence.ManuverNodes.Add(node); 
        }
    }

    public void AddNewEditNode(Entity orderEntity, DateTime nodeTime)
    {
        ManuverNode newNode = new ManuverNode(orderEntity, nodeTime);
        var val = RenderManuverLines.FindNodeTime(RootSequence, nodeTime);
        
        if (val[0].nodeIndex != -1) //if has priorNode
        {
            newNode.PriorOrbit = val[0].seq.ManuverNodes[val[0].nodeIndex].TargetOrbit;
        }

        if (val[1].nodeIndex != -1) //if has next node
        {
            val[1].seq.ManuverNodes[val[1].nodeIndex].PriorOrbit = newNode.TargetOrbit;
        }

        EditingNodes = new ManuverNode[1];
        EditingNodes[0] = newNode;
    }

    public void AddExsistingEditingNodes()
    {
        foreach (var node in EditingNodes)
        {
            AddNewNode(node);
        }

        EditingNodes = new ManuverNode[0];
    }

    public void AddSequence(string name)
    {
        var newseq = new ManuverSequence();
        newseq.SequenceName = "Thrust Manuver";
        SelectedSequence.ManuverSequences.Add(newseq);
        SelectedSequence = newseq;
        AddExsistingEditingNodes();
        
    }

    public void ManipulateNode(int nodeIndex, double _progradeDV, double _radialDV, double tseconds)
    {
        var nodeToEdit = EditingNodes[nodeIndex];
        nodeToEdit.ManipulateNode(_progradeDV, _radialDV, 0, tseconds);
        if (tseconds != 0)
        {
            var nodeTime = nodeToEdit.NodeTime;
            var val = RenderManuverLines.FindNodeTime(RootSequence, nodeTime);
        
            if (val[0].nodeIndex != -1) //if has priorNode
            {
                nodeToEdit.PriorOrbit = val[0].seq.ManuverNodes[val[0].nodeIndex].TargetOrbit;
            }

            if (val[1].nodeIndex != -1) //if has next node
            {
                val[1].seq.ManuverNodes[val[1].nodeIndex].PriorOrbit = nodeToEdit.TargetOrbit;
            }
        }
            
        
    }


    private Vector2[] points = new Vector2[0];
    private SDL.SDL_Point[] DrawPoints = new SDL.SDL_Point[0];
    
    private SDL.SDL_Point[] DrawPointsEditing = new SDL.SDL_Point[0];
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

        points = RenderManuverLines.CreatePointArray(EditingNodes);
        if(DrawPointsEditing.Length != points.Length)
            DrawPointsEditing = new SDL.SDL_Point[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            DrawPointsEditing[i] = mtrx.TransformToSDL_Point(points[i].X, points[i].Y);
        }
    }

    public void OnPhysicsUpdate()
    {
        
    }

    public void Draw(IntPtr rendererPtr, Camera camera)
    {
        SDL.SDL_SetRenderDrawColor(rendererPtr, obtClr.r, obtClr.g, obtClr.b, obtClr.a);
        SDL.SDL_RenderDrawLines(rendererPtr, DrawPoints, DrawPoints.Length);
        
        SDL.SDL_SetRenderDrawColor(rendererPtr, editClr.r, editClr.g, editClr.b, editClr.a);
        SDL.SDL_RenderDrawLines(rendererPtr, DrawPointsEditing, DrawPointsEditing.Length);
        if(DrawPoints.Length > 1)
            SDL.SDL_RenderDrawLine(rendererPtr, DrawPoints[0].x, DrawPoints[0].y, DrawPoints[1].x, DrawPoints[1].y);
    }
}

public static class RenderManuverLines
{
    public static List<(KeplerElements ke, Vector2 startPos)> GetData(ManuverSequence manuverSequence)
    {
        List<(KeplerElements ke, Vector2 startAngle)> list = new List<(KeplerElements ke, Vector2 startAngle)>();
        foreach (var node in manuverSequence.ManuverNodes)
        {
            var tgtOrbit = node.TargetOrbit;
            list.Add((tgtOrbit, (Vector2)node.NodePosition));
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
        

        List<Vector2[]> arraylist = new List<Vector2[]>();
        var pointCount = 0;
        for (int index = 0; index < data.Count; index++)
        {
            (KeplerElements ke, Vector2 startPos) item = data[index];
            double le = item.ke.LinearEccentricity;
            double e = item.ke.Eccentricity;
            double lop = item.ke.LoAN + item.ke.AoP;
            double a = item.ke.SemiMajorAxis;
            double b = item.ke.SemiMinorAxis;
            Vector2 startPos = item.startPos;
            Vector2 endPos = startPos;
            if (index < data.Count - 1)
                endPos = data[index + 1].startPos; 
            
            var kp = CreatePrimitiveShapes.KeplerPoints(a, e, lop, startPos, endPos);
            arraylist.Add(kp);
            pointCount += kp.Length;
        }

        Vector2[] pointArray = new Vector2[pointCount];
        int paIndex = 0;
        for (int i = 0; i < arraylist.Count; i++)
        {
            var source = arraylist[i];
            Array.Copy(source, 0, pointArray, paIndex, source.Length );
            paIndex += source.Length;
        }
        
        return pointArray;
    }
    
    
    public static Vector2[] CreatePointArray(ManuverNode[] manuverNodes)
    {
        int res = 128;
        List<(KeplerElements ke, Vector2 startPos)> data = new List<(KeplerElements ke, Vector2 startPos)>();
        foreach (var node in manuverNodes)
        {
            var tgtOrbit = node.TargetOrbit;
            data.Add((tgtOrbit, (Vector2)node.NodePosition));
        }
        

        List<Vector2[]> arraylist = new List<Vector2[]>();
        var pointCount = 0;
        for (int index = 0; index < data.Count; index++)
        {
            (KeplerElements ke, Vector2 startPos) item = data[index];
            double le = item.ke.LinearEccentricity;
            double e = item.ke.Eccentricity;
            double lop = item.ke.LoAN + item.ke.AoP;
            double a = item.ke.SemiMajorAxis;
            double b = item.ke.SemiMinorAxis;
            Vector2 startPos = item.startPos;
            Vector2 endPos = startPos;
            if (index < data.Count - 1)
                endPos = data[index + 1].startPos; 
            
            var kp = CreatePrimitiveShapes.KeplerPoints(a, e, lop, startPos, endPos);
            arraylist.Add(kp);
            pointCount += kp.Length;
        }

        Vector2[] pointArray = new Vector2[pointCount];
        int paIndex = 0;
        for (int i = 0; i < arraylist.Count; i++)
        {
            var source = arraylist[i];
            Array.Copy(source, 0, pointArray, paIndex, source.Length );
            paIndex += source.Length;
        }
        
        return pointArray;
    }

    public static (ManuverSequence seq, int nodeIndex)[] FindNodeTime(ManuverSequence manuverSequence, DateTime nodeTime)
    {

        (ManuverSequence seq, int priorNodeIndex)[] returnValue = new (ManuverSequence seq, int priorNodeIndex)[2];
        returnValue[0] =  (manuverSequence, -1);
        returnValue[1] = (manuverSequence, -1);
        
        if(manuverSequence.ManuverNodes.Count > 0)
        {
            for (int i = 0; i < manuverSequence.ManuverNodes.Count; i++)
            {
                ManuverNode node = manuverSequence.ManuverNodes[i];
                if (nodeTime >= node.NodeTime)
                {
                    returnValue[0] = (manuverSequence, i);
                    if (manuverSequence.ManuverNodes.Count > i + 1)
                        returnValue[1] = (manuverSequence, i + 1);
                    return returnValue;
                }
            }
        }

        if (manuverSequence.ManuverSequences.Count > 0)
        {
            foreach (ManuverSequence seq in manuverSequence.ManuverSequences)
            {
                var val = FindNodeTime(seq, nodeTime);
                if (val[0].nodeIndex > -1)
                    returnValue = val;
                if (val[1].nodeIndex > -1)
                    return returnValue;
            }
        }

        return returnValue;

    }
}