using System;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Messaging;

namespace Pulsar4X.JumpPoints;

/// <summary>
/// Gives one faction a jump that already exists. A grav hit and an intel sale both call this.
/// Does not roll, does not hide survey pins, and does not move the gate.
/// </summary>
public static class JumpPointReveal
{
    public static void Grant(Game game, int factionId, Entity jumpEntity, DateTime at)
    {
        if (game == null || jumpEntity == null || !jumpEntity.IsValid)
            return;
        if (!jumpEntity.TryGetDataBlob<JumpPointDB>(out var jump))
            return;
        if (!game.Factions.TryGetValue(factionId, out var factionEntity))
            return;
        if (!factionEntity.TryGetDataBlob<FactionInfoDB>(out var info))
            return;

        jump.IsDiscovered.Add(factionId);
        info.RememberJumpPoint(jumpEntity);
        jumpEntity.Manager?.ShowNeutralEntityToFaction(factionId, jumpEntity.Id);

        EventManager.Instance.Publish(
            Event.Create(
                EventType.JumpPointDetected,
                at,
                "Jump Point discovered",
                factionId,
                jumpEntity.Manager?.ManagerID,
                jumpEntity.Id));

        RevealOtherSide(game, jump, factionId, info, at);
    }

    static void RevealOtherSide(Game game, JumpPointDB jump, int factionId, FactionInfoDB info, DateTime at)
    {
        if (jump.DestinationId <= 0)
            return;
        if (!game.GlobalManager.TryGetGlobalEntityById(jump.DestinationId, out var destination))
            return;
        if (!destination.TryGetDataBlob<JumpPointDB>(out var farSide))
            return;

        if (!info.KnownSystems.Contains(destination.Manager.ManagerID))
        {
            info.KnownSystems.Add(destination.Manager.ManagerID);
            EventManager.Instance.Publish(
                Event.Create(
                    EventType.NewSystemDiscovered,
                    at,
                    "New system discovered",
                    factionId,
                    destination.Manager.ManagerID,
                    destination.Id));
            MessagePublisher.Instance.Publish(
                Message.Create(
                    MessageTypes.StarSystemRevealed,
                    destination.Id,
                    destination.Manager.ManagerID,
                    factionId));
        }

        farSide.IsDiscovered.Add(factionId);
        info.RememberJumpPoint(destination);
        destination.Manager.ShowNeutralEntityToFaction(factionId, destination.Id);
        EventManager.Instance.Publish(
            Event.Create(
                EventType.JumpPointDetected,
                at,
                "Jump Point discovered",
                factionId,
                destination.Manager.ManagerID,
                destination.Id));
    }
}
