using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using SDL3;

namespace Pulsar4X.Client;

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

    SDL.Color editClr = new SDL.Color()
    {
        R = 255,
        G = 215,
        B = 0,
        A = 255
    };
    SDL.Color obtClr = new SDL.Color()
    {
        R = 0,
        G = 215,
        B = 0,
        A = 255
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
    private SDL.FPoint[] DrawPoints = new SDL.FPoint[0];
    private SDL.FPoint[] DrawPointsEditing = new SDL.FPoint[0];

    /// <summary>
    /// Screen positions of editing nodes, computed during OnFrameUpdate.
    /// Used by ManeuverNodePanel to anchor the ImGui overlay.
    /// </summary>
    public SDL.FPoint[] EditingNodeScreenPositions = new SDL.FPoint[0];

    /// <summary>
    /// Screen positions of committed nodes in the root sequence.
    /// </summary>
    public SDL.FPoint[] CommittedNodeScreenPositions = new SDL.FPoint[0];

    public void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        points = RenderManuverLines.CreatePointArray(RootSequence);
        if (DrawPoints.Length != points.Length)
            DrawPoints = new SDL.FPoint[points.Length];

        var foo = camera.ViewCoordinateV2_m(RootSequence.ParentPosition.AbsolutePosition); //camera position and zoom
        var trns = Matrix.IDTranslate(foo.X, foo.Y);
        var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
        var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom

        for (int i = 0; i < points.Length; i++)
        {
            var result = mtrx.TransformToSDL_Point(points[i].X, points[i].Y);
            DrawPoints[i] = new SDL.FPoint() { X = result.X, Y = result. Y };
        }

        points = RenderManuverLines.CreatePointArray(EditingNodes);
        if(DrawPointsEditing.Length != points.Length)
            DrawPointsEditing = new SDL.FPoint[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var result = mtrx.TransformToSDL_Point(points[i].X, points[i].Y);
            DrawPointsEditing[i] = new SDL.FPoint() { X = result.X, Y = result. Y };
        }

        // Compute screen positions for editing node markers
        if (EditingNodeScreenPositions.Length != EditingNodes.Length)
            EditingNodeScreenPositions = new SDL.FPoint[EditingNodes.Length];
        for (int i = 0; i < EditingNodes.Length; i++)
        {
            var nodePos = EditingNodes[i].NodePosition;
            var result = mtrx.TransformToSDL_Point(nodePos.X, nodePos.Y);
            EditingNodeScreenPositions[i] = new SDL.FPoint() { X = result.X, Y = result.Y };
        }

        // Compute screen positions for committed node markers
        var committedNodes = RenderManuverLines.GetAllNodes(RootSequence);
        if (CommittedNodeScreenPositions.Length != committedNodes.Count)
            CommittedNodeScreenPositions = new SDL.FPoint[committedNodes.Count];
        for (int i = 0; i < committedNodes.Count; i++)
        {
            var nodePos = committedNodes[i].NodePosition;
            var result = mtrx.TransformToSDL_Point(nodePos.X, nodePos.Y);
            CommittedNodeScreenPositions[i] = new SDL.FPoint() { X = result.X, Y = result.Y };
        }
    }

    public void OnPhysicsUpdate()
    {
    }

    public void Draw(IntPtr rendererPtr, Camera camera)
    {
        SDL.SetRenderDrawColor(rendererPtr, obtClr.R, obtClr.G, obtClr.B, obtClr.A);
        SDL.RenderLines(rendererPtr, DrawPoints, DrawPoints.Length);
        SDL.SetRenderDrawColor(rendererPtr, editClr.R, editClr.G, editClr.B, editClr.A);
        SDL.RenderLines(rendererPtr, DrawPointsEditing, DrawPointsEditing.Length);
        if(DrawPoints.Length > 1)
            SDL.RenderLine(rendererPtr, DrawPoints[0].X, DrawPoints[0].Y, DrawPoints[1].X, DrawPoints[1].Y);

        // Draw committed node markers (green diamonds)
        SDL.SetRenderDrawColor(rendererPtr, obtClr.R, obtClr.G, obtClr.B, obtClr.A);
        for (int i = 0; i < CommittedNodeScreenPositions.Length; i++)
        {
            DrawDiamond(rendererPtr, CommittedNodeScreenPositions[i].X, CommittedNodeScreenPositions[i].Y, 6);
        }

        // Draw editing node markers (yellow diamonds)
        SDL.SetRenderDrawColor(rendererPtr, editClr.R, editClr.G, editClr.B, editClr.A);
        for (int i = 0; i < EditingNodeScreenPositions.Length; i++)
        {
            DrawDiamond(rendererPtr, EditingNodeScreenPositions[i].X, EditingNodeScreenPositions[i].Y, 8);
        }
    }

    private static void DrawDiamond(IntPtr rendererPtr, float cx, float cy, float size)
    {
        // Draw a diamond shape (rotated square)
        SDL.RenderLine(rendererPtr, cx, cy - size, cx + size, cy);         // top to right
        SDL.RenderLine(rendererPtr, cx + size, cy, cx, cy + size);         // right to bottom
        SDL.RenderLine(rendererPtr, cx, cy + size, cx - size, cy);         // bottom to left
        SDL.RenderLine(rendererPtr, cx - size, cy, cx, cy - size);         // left to top
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
            double e = item.ke.Eccentricity;
            double lop = item.ke.LoAN + item.ke.AoP;
            double a = item.ke.SemiMajorAxis;
            double b = item.ke.SemiMinorAxis;
            Vector2 startPos = item.startPos;
            Vector2 endPos = startPos;
            if (index < data.Count - 1)
                endPos = data[index + 1].startPos;

            Vector2[] kp;
            if (startPos.X == endPos.X && startPos.Y == endPos.Y)
            {
                // Single node with no next node: draw a full orbit.
                // KeplerPoints returns degenerate (2-point) output when start==end
                // because the sweep angle is 0, so generate the orbit directly.
                int n = 128;
                kp = new Vector2[n];
                double startAng = Math.Atan2(startPos.Y, startPos.X);
                double step = 2 * Math.PI / (n - 1);
                for (int j = 0; j < n; j++)
                {
                    double theta = startAng + step * j;
                    double r = EllipseMath.RadiusAtTrueAnomaly(a, e, lop, theta);
                    kp[j] = new Vector2(r * Math.Cos(theta), r * Math.Sin(theta));
                }
            }
            else
            {
                kp = CreatePrimitiveShapes.KeplerPoints(a, e, lop, startPos, endPos);
            }
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

    /// <summary>
    /// Collects all ManuverNodes from a sequence tree (for marker rendering).
    /// </summary>
    public static List<ManuverNode> GetAllNodes(ManuverSequence manuverSequence)
    {
        var nodes = new List<ManuverNode>();
        nodes.AddRange(manuverSequence.ManuverNodes);
        foreach (var seq in manuverSequence.ManuverSequences)
        {
            nodes.AddRange(GetAllNodes(seq));
        }
        return nodes;
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