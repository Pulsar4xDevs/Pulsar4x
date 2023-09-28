using System;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;

namespace Pulsar4X.Datablobs;
public class BeamInfoDB : BaseDataBlob
{
    public double Frequency;
    public string FiredBy;
    public Vector3 VelocityVector;
    public Vector3[] Positions;
    public bool HitsTarget;
    public Entity TargetEntity;
    private PositionDB _posDB;
    public PositionDB PosDB {
        get
        {
            if (_posDB == null)
                _posDB = OwningEntity.GetDataBlob<PositionDB>();
            return _posDB;
        }}

    public BeamInfoDB(string launchedBy, Entity targetEntity, bool hitsTarget)
    {
        FiredBy = launchedBy;
        TargetEntity = targetEntity;
        HitsTarget = hitsTarget;
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }
}