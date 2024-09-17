using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;

namespace Pulsar4X.Engine.Orders;

public class NewtonSimpleCommand : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
    public override bool IsBlocking => true;
    public override string Name { get {return _name;}}

    string _name = "Newtonion Simple thrust";

    public override string Details
    {
        get
        {
            return _details;
        }
    }
    string _details = "";

    Entity _factionEntity;
    Entity _entityCommanding;
    internal override Entity EntityCommanding { get { return _entityCommanding; } }
    public Vector3 OrbitrelativeDeltaV;
    public KeplerElements StartKE;
    public KeplerElements TargetKE;

    NewtonSimpleMoveDB _db;

    DateTime _vectorDateTime;

    public List<(string item, double value)> DebugDetails = new List<(string, double)>();

    public static void CreateCommand(int faction, Entity orderEntity, Vector3 position, Vector3 startvelocity, Vector3 endvelocity, DateTime manuverNodeTime, string name="Newtonion thrust")
    {
        var sgp = orderEntity.GetDataBlob<OrbitDB>().GravitationalParameter_m3S2;
        KeplerElements startKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, startvelocity, manuverNodeTime);
        KeplerElements tgtKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, endvelocity, manuverNodeTime);
        CreateCommand(faction, orderEntity, manuverNodeTime, startKE, tgtKE);
    }

    public static void CreateCommand(int faction, Entity orderEntity, DateTime manuverNodeTime, KeplerElements startKE, KeplerElements finKE, string name="Newtonion Simple thrust")
    {

        var startVec = OrbitalMath.GetStateVectors(startKE, manuverNodeTime);

        var tgtVec = OrbitalMath.GetStateVectors(finKE, manuverNodeTime);


        var manuverVector = tgtVec.velocity - startVec.velocity;
        var manuverDV = manuverVector.Length();

        var cmd = new NewtonSimpleCommand()
        {
            RequestingFactionGuid = faction,
            EntityCommandingGuid = orderEntity.Id,
            _entityCommanding = orderEntity,
            CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
            OrbitrelativeDeltaV = new Vector3(manuverVector.X, manuverVector.Y, 0),
            StartKE = startKE,
            TargetKE = finKE,
            _vectorDateTime = manuverNodeTime,
            ActionOnDate = manuverNodeTime,
            _name = name,

        };

        // FIXME:
        //StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        cmd.UpdateDetailString();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!IsRunning && atDateTime >= ActionOnDate)
        {
            var parent = _entityCommanding.GetSOIParentEntity();

            if(parent == null) throw new NullReferenceException("parent cannot be null");

            // var currentVel = _entityCommanding.GetRelativeFutureVelocity(atDateTime);

            // var parentMass = _entityCommanding.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            // var myMass = _entityCommanding.GetDataBlob<MassVolumeDB>().MassTotal;
            // var sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass);

            // var futurePosition = _entityCommanding.GetRelativeFuturePosition(_vectorDateTime);
            // var futureVector = _entityCommanding.GetRelativeFutureVelocity(_vectorDateTime);

            _db = new NewtonSimpleMoveDB(parent, StartKE, TargetKE, ActionOnDate);
            _entityCommanding.SetDataBlob(_db);

            UpdateDetailString();
            IsRunning = true;
        }
    }

    public override void UpdateDetailString()
    {
        if(ActionOnDate > _entityCommanding.StarSysDateTime)
            _details = "Waiting " + (ActionOnDate - _entityCommanding.StarSysDateTime).ToString("d'd 'h'h 'm'm 's's'") + "\n"
                       + "   to expend  " + Stringify.Velocity(OrbitrelativeDeltaV.Length()) + " Δv";
        else if(IsRunning)
            _details = "Manuvering ";
    }

    public override bool IsFinished()
    {
        if (IsRunning && _db.IsComplete)
            return true;
        else
            return false;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            return true;
        else
            return false;
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}