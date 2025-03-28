using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;

namespace Pulsar4X.Technology;

public class FundingChangedOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Funding Changed";

    public override string Details => "Instantly changes the funding level of a given lab";

    internal override Entity EntityCommanding => _labEntity;

    private Entity _labEntity;
    private byte _fundingLevel;

    private FundingChangedOrder(Entity labEntity, byte fundingLevel)
    {
        _labEntity = labEntity;
        _fundingLevel = fundingLevel;
    }

    public static FundingChangedOrder Create(Entity labEntity, byte fundingLevel)
    {
        return new FundingChangedOrder(labEntity, fundingLevel);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!_labEntity.TryGetDataBlob<ResearcherDB>(out var researcherDB))
            return;

        if(_fundingLevel < 0 || _fundingLevel > 5)
            return;

        researcherDB.FundingLevel = _fundingLevel;

        EventManager.Instance.Publish(
            Event.Create(
                    EventType.TechnologyFundingChanged,
                    atDateTime,
                    "Funding level changed",
                    _labEntity.FactionOwnerID,
                    _labEntity.Manager.ManagerID,
                    _labEntity.Id));
    }

    internal override bool IsFinished()
    {
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return true;
    }
}