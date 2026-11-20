using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.People;

namespace GameEngine.People;

/// <summary>
/// Seating rules for a colony's market head, and what the seat allows. With a head seated the colony's market prices
/// its own listings (auto-priced rows) and sells stock above the warehouse Max (<see cref="Pulsar4X.Logistics.StockRules"/>).
/// Without one, only prices the player set are listed. The head is a hired civilian who holds no city-hall chair and no
/// industry seat. All writes go through here.
/// </summary>
public static class MarketSeat
{
    public const string OnePost = "One person holds one post: the city-hall chair, the industry seat or the market seat.";

    /// <summary>The seated head's entity id, or -1 when the seat is empty or its id no longer names a live person of the colony's faction.</summary>
    public static int HeadId(Entity colony)
        => TryGetHead(colony, out var head) ? head.Id : MarketSeatDB.Empty;

    /// <summary>True when the colony has a seated market head who still exists and works for the colony's faction.</summary>
    public static bool HasHead(Entity colony) => TryGetHead(colony, out _);

    /// <summary>The seated head, when there is one. A dead or foreign id is an empty seat.</summary>
    public static bool TryGetHead(Entity colony, out Entity head)
    {
        head = default!;
        int id = colony.TryGetDataBlob<MarketSeatDB>(out var seat) ? seat.HeadId : MarketSeatDB.Empty;
        if (id < 0 || colony.Manager == null || !colony.Manager.TryGetGlobalEntityById(id, out head))
            return false;
        if (head == null || !head.IsValid || head.FactionOwnerID != colony.FactionOwnerID || !head.HasDataBlob<CommanderDB>())
        {
            head = default!;
            return false;
        }
        return true;
    }

    /// <summary>
    /// True when the colony's market may set prices by itself: a market head is seated, or the colony belongs to an NPC
    /// faction (not the local human player), whose markets are always run for it. Gates auto-priced rows and the
    /// warehouse Max sale.
    /// </summary>
    public static bool ManagesMarket(Entity colony)
    {
        if (HasHead(colony))
            return true;
        var game = colony.Manager?.Game;
        return game == null || game.LocalHumanFactionId < 0 || colony.FactionOwnerID != game.LocalHumanFactionId;
    }

    /// <summary>True when the person is the seated market head of the colony they are assigned to.</summary>
    public static bool HoldsMarketSeat(Entity person, out Entity colony)
    {
        colony = default!;
        if (!person.TryGetDataBlob<CommanderDB>(out var db) || db.AssignedTo < 0 || person.Manager == null)
            return false;
        if (!person.Manager.TryGetGlobalEntityById(db.AssignedTo, out var post) || HeadId(post) != person.Id)
            return false;
        colony = post;
        return true;
    }

    /// <summary>Checks that <paramref name="person"/> can head <paramref name="colony"/>'s market. Null when they can; otherwise the reason.</summary>
    public static string? SeatRefusal(Entity colony, Entity? person, int requestedId)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return "A market head sits a colony.";
        if (person == null || !person.IsValid
            || person.FactionOwnerID != colony.FactionOwnerID
            || !person.TryGetDataBlob<CommanderDB>(out var db))
            return $"Civilian {requestedId} not found.";
        if (db.Type != CommanderTypes.Civilian)
            return "Only a hired civilian administrator can head the market.";
        if (IndustrySeat.HoldsChair(person) || SitsChairOn(colony, person.Id))
            return $"{NameOf(person)} holds a city-hall chair. {OnePost}";
        if (IndustrySeat.HoldsIndustrySeat(person, out _))
            return $"{NameOf(person)} is an industry head. {OnePost}";
        return null;
    }

    /// <summary>Seats the person as the colony's market head. A head of another colony's market moves. Null on success, or the refusal.</summary>
    public static string? Seat(Entity colony, Entity person, DateTime atDateTime)
    {
        string? refusal = SeatRefusal(colony, person, person?.Id ?? -1);
        if (refusal != null)
            return refusal;
        var seat = Of(colony);
        if (seat.HeadId == person!.Id)
            return null;
        ClearIfDangling(colony);
        if (HoldsMarketSeat(person, out var previous) && previous.Id != colony.Id)
            ClearSeat(previous, atDateTime);
        ClearSeat(colony, atDateTime);
        seat.HeadId = person.Id;
        person.GetDataBlob<CommanderDB>().AssignedTo = colony.Id;
        ShipLogBook.NoteTookPost(colony, person, atDateTime, "the market seat");
        return null;
    }

    /// <summary>Empties the colony's market seat. Null on success (an empty seat is fine), or the refusal.</summary>
    public static string? Unseat(Entity colony, DateTime atDateTime)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return "A market head sits a colony.";
        ClearSeat(colony, atDateTime);
        return null;
    }

    /// <summary>Empties the seat of this person, if they hold one, without a log line (a commander leaving the game).</summary>
    public static void ClearOnRemoval(Entity person)
    {
        if (!person.TryGetDataBlob<CommanderDB>(out var db) || db.AssignedTo < 0 || person.Manager == null)
            return;
        if (person.Manager.TryGetGlobalEntityById(db.AssignedTo, out var post)
            && post.TryGetDataBlob<MarketSeatDB>(out var seat) && seat.HeadId == person.Id)
            seat.HeadId = MarketSeatDB.Empty;
    }

    /// <summary>After a load: a seat whose id no longer names a live person of the colony's faction is emptied.</summary>
    internal static void AfterLoad(Game game)
    {
        foreach (var manager in game.GlobalManagerDictionary.Values)
        {
            foreach (var colony in manager.GetAllEntitiesWithDataBlob<MarketSeatDB>())
                ClearIfDangling(colony);
        }
    }

    internal static bool ClearIfDangling(Entity colony)
    {
        if (!colony.TryGetDataBlob<MarketSeatDB>(out var seat) || seat.HeadId < 0 || TryGetHead(colony, out _))
            return false;
        seat.HeadId = MarketSeatDB.Empty;
        return true;
    }

    static void ClearSeat(Entity colony, DateTime atDateTime)
    {
        if (!colony.TryGetDataBlob<MarketSeatDB>(out var seat) || seat.HeadId < 0)
            return;
        int leavingId = seat.HeadId;
        seat.HeadId = MarketSeatDB.Empty;
        if (colony.Manager != null && colony.Manager.TryGetGlobalEntityById(leavingId, out var leaving)
            && leaving.IsValid && leaving.FactionOwnerID == colony.FactionOwnerID)
        {
            if (leaving.TryGetDataBlob<CommanderDB>(out var db) && db.AssignedTo == colony.Id)
                db.AssignedTo = -1;
            ShipLogBook.NoteLeftPost(colony, leaving, atDateTime, "the market seat");
        }
    }

    /// <summary>The colony's seat datablob, created empty on first use.</summary>
    public static MarketSeatDB Of(Entity colony)
    {
        if (!colony.TryGetDataBlob<MarketSeatDB>(out var seat))
        {
            seat = new MarketSeatDB();
            colony.SetDataBlob(seat);
        }
        return seat;
    }

    static bool SitsChairOn(Entity post, int personId)
    {
        if (!post.TryGetDataBlob<AdminSpaceDB>(out var admin))
            return false;
        foreach (var chair in admin.CommanderSeats)
        {
            if (chair.CommanderID == personId)
                return true;
        }
        return false;
    }

    static string NameOf(Entity person)
        => person.TryGetDataBlob<CommanderDB>(out var db) && !string.IsNullOrEmpty(db.Name) ? db.Name : "This person";
}
