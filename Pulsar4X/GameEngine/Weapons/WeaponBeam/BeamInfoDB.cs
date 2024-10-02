using System;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;

namespace Pulsar4X.Datablobs;
public class BeamInfoDB : BaseDataBlob
{
    public enum BeamStates
    {
        Fired,
        AtTarget,
        HitTarget,
        MissedTarget
    };

    public BeamStates BeamState;
    public double Frequency;
    public double Energy;
    public int FiredBy;
    public Vector3 VelocityVector;
    public (Vector3, Vector3) Positions;
    public bool HitsTarget;
    public Entity TargetEntity;
    public Vector3 LaunchPosition;
    private PositionDB _posDB;
    public PositionDB PosDB {
        get
        {
            if (_posDB == null)
                _posDB = OwningEntity.GetDataBlob<PositionDB>();
            return _posDB;
        }}

    public BeamInfoDB(int launchedBy, Entity targetEntity, bool hitsTarget)
    {
        FiredBy = launchedBy;
        TargetEntity = targetEntity;
        HitsTarget = hitsTarget;
        BeamState = BeamStates.Fired;
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }
}