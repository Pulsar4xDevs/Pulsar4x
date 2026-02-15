using System;
using System.Collections.Generic;
using ImGuiNET;
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

    // Ap/Pe screen positions for editing nodes' predicted orbits
    private SDL.FPoint _parentScreenPos;
    private SDL.FPoint[] _editingPeScreenPositions = new SDL.FPoint[0];
    private SDL.FPoint[] _editingApScreenPositions = new SDL.FPoint[0];
    private double[] _editingPeDistances = new double[0];
    private double[] _editingApDistances = new double[0];
    private double[] _editingEccentricities = new double[0];

    // Encounter rendering data
    private EncounterPrediction[] _encounters = Array.Empty<EncounterPrediction>();
    private SDL.FPoint[] _encounterBodyScreenPositions = new SDL.FPoint[0];
    private float[] _encounterSOIScreenRadii = new float[0];
    private SDL.FPoint[] _encounterShipScreenPositions = new SDL.FPoint[0];

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

        // Parent body is at the origin in the relative coordinate system
        var parentResult = mtrx.TransformToSDL_Point(0, 0);
        _parentScreenPos = new SDL.FPoint() { X = parentResult.X, Y = parentResult.Y };

        // Compute Ap/Pe screen positions for editing nodes' predicted orbits
        if (_editingPeScreenPositions.Length != EditingNodes.Length)
        {
            _editingPeScreenPositions = new SDL.FPoint[EditingNodes.Length];
            _editingApScreenPositions = new SDL.FPoint[EditingNodes.Length];
            _editingPeDistances = new double[EditingNodes.Length];
            _editingApDistances = new double[EditingNodes.Length];
            _editingEccentricities = new double[EditingNodes.Length];
        }
        for (int i = 0; i < EditingNodes.Length; i++)
        {
            var ke = EditingNodes[i].TargetOrbit;
            _editingEccentricities[i] = ke.Eccentricity;
            _editingPeDistances[i] = ke.Periapsis;
            _editingApDistances[i] = ke.Apoapsis;

            if (ke.Eccentricity < 0.001 || ke.Eccentricity >= 1.0)
                continue;

            // Periapsis: true anomaly = 0, at angle = lop from focus
            // Apoapsis: true anomaly = pi, at angle = lop + pi from focus
            double lop = ke.LoAN + ke.AoP;
            double peR = ke.Periapsis;
            double apR = ke.Apoapsis;

            var peWorld = new Vector2(peR * Math.Cos(lop), peR * Math.Sin(lop));
            var apWorld = new Vector2(-apR * Math.Cos(lop), -apR * Math.Sin(lop));

            var peResult = mtrx.TransformToSDL_Point(peWorld.X, peWorld.Y);
            var apResult = mtrx.TransformToSDL_Point(apWorld.X, apWorld.Y);
            _editingPeScreenPositions[i] = new SDL.FPoint() { X = peResult.X, Y = peResult.Y };
            _editingApScreenPositions[i] = new SDL.FPoint() { X = apResult.X, Y = apResult.Y };
        }

        // Gather encounters from all editing nodes
        var encounterList = new List<EncounterPrediction>();
        for (int i = 0; i < EditingNodes.Length; i++)
        {
            if (EditingNodes[i].Encounters != null)
                encounterList.AddRange(EditingNodes[i].Encounters);
        }
        _encounters = encounterList.ToArray();

        if (_encounterBodyScreenPositions.Length != _encounters.Length)
        {
            _encounterBodyScreenPositions = new SDL.FPoint[_encounters.Length];
            _encounterSOIScreenRadii = new float[_encounters.Length];
            _encounterShipScreenPositions = new SDL.FPoint[_encounters.Length];
        }
        for (int i = 0; i < _encounters.Length; i++)
        {
            var bodyPos = _encounters[i].BodyPositionAtEncounter;
            var bodyScreen = mtrx.TransformToSDL_Point(bodyPos.X, bodyPos.Y);
            _encounterBodyScreenPositions[i] = new SDL.FPoint() { X = bodyScreen.X, Y = bodyScreen.Y };

            var shipPos = _encounters[i].ShipPositionAtEncounter;
            var shipScreen = mtrx.TransformToSDL_Point(shipPos.X, shipPos.Y);
            _encounterShipScreenPositions[i] = new SDL.FPoint() { X = shipScreen.X, Y = shipScreen.Y };

            // Compute SOI screen radius by transforming an offset point
            var soiEdge = mtrx.TransformToSDL_Point(bodyPos.X + _encounters[i].SOIRadius_m, bodyPos.Y);
            _encounterSOIScreenRadii[i] = MathF.Abs(soiEdge.X - bodyScreen.X);
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

        // Draw Ap/Pe diamond markers on editing nodes' predicted orbits
        for (int i = 0; i < EditingNodes.Length; i++)
        {
            if (_editingEccentricities[i] < 0.001 || _editingEccentricities[i] >= 1.0)
                continue;

            // Periapsis - cyan
            if (camera.IsOnScreen(_editingPeScreenPositions[i].X, _editingPeScreenPositions[i].Y))
            {
                SDL.SetRenderDrawColor(rendererPtr, 0, 200, 255, 255);
                DrawDiamond(rendererPtr, _editingPeScreenPositions[i].X, _editingPeScreenPositions[i].Y, 6);
            }

            // Apoapsis - orange
            if (camera.IsOnScreen(_editingApScreenPositions[i].X, _editingApScreenPositions[i].Y))
            {
                SDL.SetRenderDrawColor(rendererPtr, 255, 165, 0, 255);
                DrawDiamond(rendererPtr, _editingApScreenPositions[i].X, _editingApScreenPositions[i].Y, 6);
            }
        }

        // Draw encounter predictions
        for (int i = 0; i < _encounters.Length; i++)
        {
            var bodyPt = _encounterBodyScreenPositions[i];
            var shipPt = _encounterShipScreenPositions[i];
            float soiR = _encounterSOIScreenRadii[i];

            if (_encounters[i].EntersSOI)
            {
                // SOI entry - green
                SDL.SetRenderDrawColor(rendererPtr, 100, 255, 100, 80);
                DrawCircle(rendererPtr, bodyPt.X, bodyPt.Y, soiR, 64);
                SDL.SetRenderDrawColor(rendererPtr, 100, 255, 100, 200);
                DrawDiamond(rendererPtr, bodyPt.X, bodyPt.Y, 5);
            }
            else
            {
                // Near-miss - cyan
                SDL.SetRenderDrawColor(rendererPtr, 100, 200, 255, 40);
                DrawCircle(rendererPtr, bodyPt.X, bodyPt.Y, soiR, 64);
                SDL.SetRenderDrawColor(rendererPtr, 100, 200, 255, 150);
                DrawDiamond(rendererPtr, bodyPt.X, bodyPt.Y, 5);
            }

            // Closest approach line
            SDL.SetRenderDrawColor(rendererPtr, 200, 200, 200, 100);
            SDL.RenderLine(rendererPtr, shipPt.X, shipPt.Y, bodyPt.X, bodyPt.Y);
        }
    }

    /// <summary>
    /// Draws ImGui text labels at Ap/Pe positions for editing nodes.
    /// Must be called during the ImGui render pass (not SDL Draw).
    /// </summary>
    public void DrawApsisLabels()
    {
        if (_editingEccentricities.Length < EditingNodes.Length)
            return; // Not yet computed by OnFrameUpdate

        int labelId = 0;
        for (int i = 0; i < EditingNodes.Length; i++)
        {
            if (_editingEccentricities[i] < 0.001 || _editingEccentricities[i] >= 1.0)
                continue;

            DrawApsisLabel(_editingPeScreenPositions[i], _parentScreenPos, FormatDistance(_editingPeDistances[i]), "Pe", 0, 200, 255, labelId++);
            DrawApsisLabel(_editingApScreenPositions[i], _parentScreenPos, FormatDistance(_editingApDistances[i]), "Ap", 255, 165, 0, labelId++);
        }
    }

    private static void DrawApsisLabel(SDL.FPoint pos, SDL.FPoint parentPos, string distText, string prefix, byte r, byte g, byte b, int id)
    {
        // Compute outward direction from parent body to apsis point
        float dx = pos.X - parentPos.X;
        float dy = pos.Y - parentPos.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return;
        float nx = dx / len;
        float ny = dy / len;

        string labelText = prefix + ": " + distText;
        var textSize = ImGui.CalcTextSize(labelText);

        // Offset from diamond center along the outward radial direction
        const float offsetDist = 10f;
        float labelX = pos.X + nx * offsetDist;
        float labelY = pos.Y + ny * offsetDist;

        // Anchor the text so it extends outward from the orbit line:
        // If the outward direction points left, right-align the text
        if (nx < 0) labelX -= textSize.X;
        // If the outward direction points up, bottom-align the text
        if (ny < 0) labelY -= textSize.Y;

        // Draw directly to the foreground draw list to avoid creating ImGui windows,
        // which would steal focus from active widgets (like DragFloat sliders).
        uint color = ImGui.GetColorU32(new System.Numerics.Vector4(r / 255f, g / 255f, b / 255f, 1f));
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddText(new System.Numerics.Vector2(labelX, labelY), color, labelText);
    }

    private static string FormatDistance(double meters)
    {
        double au = Distance.MToAU(meters);
        if (au >= 0.01)
            return au.ToString("F2") + " AU";
        double km = Distance.MToKm(meters);
        return km.ToString("N0") + " km";
    }

    private static void DrawDiamond(IntPtr rendererPtr, float cx, float cy, float size)
    {
        // Draw a diamond shape (rotated square)
        SDL.RenderLine(rendererPtr, cx, cy - size, cx + size, cy);         // top to right
        SDL.RenderLine(rendererPtr, cx + size, cy, cx, cy + size);         // right to bottom
        SDL.RenderLine(rendererPtr, cx, cy + size, cx - size, cy);         // bottom to left
        SDL.RenderLine(rendererPtr, cx - size, cy, cx, cy - size);         // left to top
    }

    private static void DrawCircle(IntPtr rendererPtr, float cx, float cy, float radius, int segments)
    {
        if (radius < 1f)
            return;
        double step = 2 * Math.PI / segments;
        float prevX = cx + radius;
        float prevY = cy;
        for (int s = 1; s <= segments; s++)
        {
            float x = cx + radius * (float)Math.Cos(s * step);
            float y = cy + radius * (float)Math.Sin(s * step);
            SDL.RenderLine(rendererPtr, prevX, prevY, x, y);
            prevX = x;
            prevY = y;
        }
    }

    public void DrawEncounterLabels()
    {
        if (_encounters.Length == 0)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        for (int i = 0; i < _encounters.Length; i++)
        {
            var enc = _encounters[i];
            var bodyPt = _encounterBodyScreenPositions[i];

            string prefix;
            uint color;
            if (enc.EntersSOI)
            {
                prefix = ">> SOI: " + enc.BodyName;
                color = ImGui.GetColorU32(new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f));
            }
            else
            {
                prefix = "CA: " + enc.BodyName;
                color = ImGui.GetColorU32(new System.Numerics.Vector4(0.4f, 0.8f, 1f, 1f));
            }

            string labelText = prefix + " " + FormatDistance(enc.ClosestApproach_m);
            float labelX = bodyPt.X + 8;
            float labelY = bodyPt.Y + 8;
            drawList.AddText(new System.Numerics.Vector2(labelX, labelY), color, labelText);
        }
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