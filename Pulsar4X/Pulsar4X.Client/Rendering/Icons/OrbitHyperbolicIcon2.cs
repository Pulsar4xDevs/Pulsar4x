using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using SDL3;

namespace Pulsar4X.Client;

public class OrbitHyperbolicIcon2 : OrbitIconBase
{
    private readonly double _parentSoiRadiusM;

    internal OrbitHyperbolicIcon2(Pulsar4X.Api.OrbitView orbit, IPosition bodyPosition,
        IPosition parentPosition, UserOrbitSettings.OrbitBodyType bodyType,
        List<List<UserOrbitSettings>> settings)
        : base(orbit, bodyPosition, parentPosition, bodyType, settings)
    {
        TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
        _parentSoiRadiusM = orbit.ParentSoiRadiusM;

        UpdateUserSettings();
        CreatePointArray();
        OnPhysicsUpdate();
    }

    protected override void CreatePointArray()
    {
        double p = EllipseMath.SemiLatusRectum(SemiMaj, _eccentricity);
        double angleToSOIPoint = EllipseMath.TrueAnomalyAtRadus(_parentSoiRadiusM, p, _eccentricity);

        _points = CreatePrimitiveShapes.HyperbolicPoints(SemiMaj, _eccentricity, _loP_radians, angleToSOIPoint, _numberOfArcSegments + 1);
    }

    public override void OnPhysicsUpdate()
    {
        Vector3 pos = BodyPositionDB.RelativePosition;
        _bodyrelativePos = new Vector2() { X = pos.X, Y = pos.Y };
        var apos = BodyPositionDB.AbsolutePosition;
        _bodyAbsolutePos = new Vector2(apos.X, apos.Y);

        //we find the point in the ellipse which is closest to the body so we can start drawing from the body.
        double minDist = (_bodyrelativePos - _points[0]).Length();

        for (int i =0; i < _points.Length; i++)
        {
            double dist = (_bodyrelativePos - _points[i]).Length();
            if (dist < minDist)
            {
                minDist = dist;
                _index = i;
            }
        }
        UpdateUserSettings();
    }

    public override void OnFrameUpdate(Matrix matrix, Camera camera)
    {

        //resize for zoom
        //translate to position

        var foo = camera.ViewCoordinateV2_m(WorldPosition_m); //camera position and zoom
        var trns = Matrix.IDTranslate(foo.X, foo.Y);
        var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
        var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom
        var spos = camera.ViewCoordinateV2_m(_bodyAbsolutePos);

        int remaining = RemainingPointCount();
        if (_drawPoints.Length != remaining + 1)
            _drawPoints = new SDL.Point[remaining + 1];

        _drawPoints[0] = new SDL.Point(){ X = (int)spos.X, Y = (int)spos.Y};

        int i2 = 1;
        if (IsRetrogradeOrbit)
        {
            for (int i = _index - 1; i >= 0 && i2 < _drawPoints.Length; i--)
            {
                _drawPoints[i2] = mtrx.TransformToSDL_Point(_points[i].X, _points[i].Y);
                i2++;
            }
        }
        else
        {
            for (int i = _index + 1; i < _points.Length && i2 < _drawPoints.Length; i++)
            {
                _drawPoints[i2] = mtrx.TransformToSDL_Point(_points[i].X, _points[i].Y);
                i2++;
            }
        }
    }

    /// <summary>
    /// Hyperbola is an open arc (SOI → periapsis → SOI). Do not wrap like an ellipse.
    /// Remaining trail is toward array start (retrograde) or array end (prograde).
    /// </summary>
    int RemainingPointCount()
    {
        if (_points == null || _points.Length == 0)
            return 0;
        if (IsRetrogradeOrbit)
            return Math.Max(0, _index);
        return Math.Max(0, _points.Length - _index - 1);
    }

    public override void UpdateUserSettings()
    {
        int remaining = RemainingPointCount();
        _drawPoints = new SDL.Point[remaining + 1];
        _numberOfDrawSegments = Math.Max(1, _drawPoints.Length - 1);
        _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;
    }

    public override void Draw(IntPtr rendererPtr, Camera camera)
    {
        //now we draw a line between each of the points in the translatedPoints[] array.
        if (_drawPoints.Length <= _numberOfDrawSegments - 1)
            return;
        float alpha = _userSettings.MaxAlpha;
        for (int i = 0; i < _drawPoints.Length - 1; i++)
        {
            SDL.SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up.
            SDL.RenderLine(rendererPtr, _drawPoints[i].X, _drawPoints[i].Y, _drawPoints[i + 1].X, _drawPoints[i +1].Y);
            alpha -= _alphaChangeAmount;
        }
    }
}