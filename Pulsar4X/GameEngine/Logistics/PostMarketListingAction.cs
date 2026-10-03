using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;

namespace Pulsar4X.Logistics;

/// <summary>
/// Writes one listing onto the commanded colony's book. Instant: the book updates inside HandleOrder.
/// </summary>
public class PostMarketListingAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => true;

    public override string Name => "Post Market Listing";

    public override string Details => _details;

    string _details = "";
    MarketListing _listing = new();
    Entity? _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding!;

    public PostMarketListingAction()
    {
        UseActionLanes = false;
    }

    public static PostMarketListingAction Create(Entity colony, MarketListing listing)
    {
        return new PostMarketListingAction
        {
            RequestingFactionGuid = colony.FactionOwnerID,
            EntityCommandingGuid = colony.Id,
            _listing = listing.Copy(),
            CreatedDate = colony.StarSysDateTime,
            ActionOnDate = colony.StarSysDateTime,
        };
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (_isFinished)
            return;
        if (_entityCommanding == null)
        {
            Fail("No listing");
            return;
        }

        if (!MarketBook.SetListing(_entityCommanding, _listing))
        {
            Fail("Listing rejected");
            return;
        }

        _details = _listing.CargoId;
        _isFinished = true;
    }

    internal override bool IsFinished() => _isFinished;

    internal override bool IsValidCommand(Game game)
    {
        return CommandHelpers.IsCommandValid(
            game.GlobalManager,
            RequestingFactionGuid,
            EntityCommandingGuid,
            out _,
            out _entityCommanding);
    }

    public override EntityAction Clone()
    {
        return new PostMarketListingAction
        {
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            _listing = _listing.Copy(),
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentGoalId = ParentGoalId,
        };
    }

    void Fail(string message)
    {
        _details = message;
        Status = ActionStatus.Failed;
        _isFinished = true;
    }
}
