using System;
using Pulsar4X.Industry;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.People;

namespace GameEngine.People;

/// <summary>
/// Seating rules for a colony's industry head. The head is a hired civilian who is not also in a city-hall
/// chair. All writes go through here (the orders, the API command), so the rule is applied one way.
/// </summary>
public static class IndustrySeat
{
    public const string BothPosts = "One person cannot hold both the city-hall chair and the industry seat.";

    /// <summary>
    /// The seated head's entity id, or -1 when the seat is empty, the colony has none, or the stored id no longer
    /// names a live person of the colony's faction (see <see cref="TryGetHead"/>).
    /// </summary>
    public static int HeadId(Entity colony)
        => TryGetHead(colony, out var head) ? head.Id : IndustrySeatDB.Empty;

    /// <summary>True when the colony has a seated industry head who still exists and works for the colony's faction.</summary>
    public static bool HasHead(Entity colony) => TryGetHead(colony, out _);

    /// <summary>
    /// The seated head, when there is one. The seat only counts as filled while the stored id resolves to a
    /// living commander owned by the colony's faction; a dead or foreign id is an empty seat.
    /// </summary>
    public static bool TryGetHead(Entity colony, out Entity head)
    {
        head = default!;
        int id = StoredHeadId(colony);
        if (id < 0 || colony.Manager == null || !colony.Manager.TryGetGlobalEntityById(id, out head))
            return false;
        if (head == null || !head.IsValid || head.FactionOwnerID != colony.FactionOwnerID
            || !head.HasDataBlob<CommanderDB>())
        {
            head = default!;
            return false;
        }
        return true;
    }

    // What the seat datablob holds, whether or not it still resolves.
    static int StoredHeadId(Entity colony)
        => colony.TryGetDataBlob<IndustrySeatDB>(out var seat) ? seat.HeadId : IndustrySeatDB.Empty;

    /// <summary>
    /// Empties the seat of this person, if they hold one, without a log line. For a commander being removed from
    /// the game: nothing may be left pointing at them.
    /// </summary>
    public static void ClearOnRemoval(Entity person)
    {
        if (!person.TryGetDataBlob<CommanderDB>(out var db) || db.AssignedTo < 0 || person.Manager == null)
            return;
        if (person.Manager.TryGetGlobalEntityById(db.AssignedTo, out var post)
            && post.TryGetDataBlob<IndustrySeatDB>(out var seat) && seat.HeadId == person.Id)
        {
            seat.HeadId = IndustrySeatDB.Empty;
            IndustryTools.ClearStockOrders(post, "the industry head is gone");
        }
    }

    /// <summary>
    /// After a load: any seat whose stored id no longer names a live person of the colony's faction is emptied
    /// (a save from before commanders cleared their seat on death can hold one).
    /// </summary>
    internal static void AfterLoad(Game game)
    {
        foreach (var manager in game.GlobalManagerDictionary.Values)
        {
            foreach (var colony in manager.GetAllEntitiesWithDataBlob<IndustrySeatDB>())
                ClearIfDangling(colony);
        }
    }

    /// <summary>Empties the seat when its stored id is not a live head. Returns true when it did.</summary>
    internal static bool ClearIfDangling(Entity colony)
    {
        if (!colony.TryGetDataBlob<IndustrySeatDB>(out var seat) || seat.HeadId < 0 || TryGetHead(colony, out _))
            return false;
        seat.HeadId = IndustrySeatDB.Empty;
        IndustryTools.ClearStockOrders(colony, "the industry head is gone");
        return true;
    }

    /// <summary>True when the person is the seated industry head of the colony they are assigned to.</summary>
    public static bool HoldsIndustrySeat(Entity person, out Entity colony)
    {
        colony = default!;
        if (!person.TryGetDataBlob<CommanderDB>(out var db) || db.AssignedTo < 0 || person.Manager == null)
            return false;
        if (!person.Manager.TryGetGlobalEntityById(db.AssignedTo, out var post))
            return false;
        if (HeadId(post) != person.Id)
            return false;
        colony = post;
        return true;
    }

    /// <summary>True when the person sits any city-hall (<see cref="AdminSpaceDB"/>) seat.</summary>
    public static bool HoldsChair(Entity person)
    {
        if (!person.TryGetDataBlob<CommanderDB>(out var db) || db.AssignedTo < 0 || person.Manager == null)
            return false;
        return person.Manager.TryGetGlobalEntityById(db.AssignedTo, out var post) && SitsChairOn(post, person.Id);
    }

    static bool SitsChairOn(Entity post, int personId)
    {
        if (!post.TryGetDataBlob<AdminSpaceDB>(out var admin))
            return false;
        foreach (var seat in admin.CommanderSeats)
        {
            if (seat.CommanderID == personId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// The refusal for seating this person in a city-hall chair, or null when they hold no industry seat.
    /// Call before any chair assignment.
    /// </summary>
    public static string? ChairRefusal(Entity person)
    {
        if (MarketSeat.HoldsMarketSeat(person, out _))
            return $"{NameOf(person)} is a market head. {MarketSeat.OnePost}";
        if (!HoldsIndustrySeat(person, out _))
            return null;
        return $"{NameOf(person)} is an industry head. {BothPosts}";
    }

    /// <summary>
    /// Checks that <paramref name="person"/> can head <paramref name="colony"/>'s industry. Null when they can;
    /// otherwise the reason. Does not change anything.
    /// </summary>
    public static string? SeatRefusal(Entity colony, Entity? person, int requestedId)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return "An industry head sits a colony.";
        if (person == null || !person.IsValid
            || person.FactionOwnerID != colony.FactionOwnerID
            || !person.TryGetDataBlob<CommanderDB>(out var db))
            return $"Civilian {requestedId} not found.";
        if (db.Type != CommanderTypes.Civilian)
            return "Only a hired civilian administrator can head industry.";
        if (HoldsChair(person) || (colony.TryGetDataBlob<AdminSpaceDB>(out _) && SitsChairOn(colony, person.Id)))
            return $"{NameOf(person)} holds a city-hall chair. {BothPosts}";
        if (MarketSeat.HoldsMarketSeat(person, out _))
            return $"{NameOf(person)} is a market head. {MarketSeat.OnePost}";
        return null;
    }

    /// <summary>
    /// Seats <paramref name="person"/> as the colony's industry head. A person already heading another
    /// colony's industry moves; whoever sat this seat leaves it. Returns null on success, or the refusal.
    /// </summary>
    public static string? Seat(Entity colony, Entity person, DateTime atDateTime)
    {
        string? refusal = SeatRefusal(colony, person, person?.Id ?? -1);
        if (refusal != null)
            return refusal;

        var seat = Of(colony);
        if (seat.HeadId == person.Id)
            return null;
        ClearIfDangling(colony); // a dead or foreign id is no one to push out

        if (HoldsIndustrySeat(person, out var previousColony) && previousColony.Id != colony.Id)
            ClearSeat(previousColony, atDateTime);

        // A new head replacing a seated one keeps the colony's stock orders: the seat is never empty, so no job is left headless.
        if (seat.HeadId >= 0)
            ClearSeat(colony, atDateTime, keepStockOrders: true);

        seat.HeadId = person.Id;
        person.GetDataBlob<CommanderDB>().AssignedTo = colony.Id;
        ShipLogBook.NoteTookPost(colony, person, atDateTime, "the industry seat");
        return null;
    }

    /// <summary>Empties the colony's industry seat. Returns null on success (an empty seat is fine), or the refusal.</summary>
    public static string? Unseat(Entity colony, DateTime atDateTime)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return "An industry head sits a colony.";
        ClearSeat(colony, atDateTime);
        return null;
    }

    static void ClearSeat(Entity colony, DateTime atDateTime, bool keepStockOrders = false)
    {
        if (!colony.TryGetDataBlob<IndustrySeatDB>(out var seat) || seat.HeadId < 0)
            return;
        int leavingId = seat.HeadId;
        seat.HeadId = IndustrySeatDB.Empty;
        if (!keepStockOrders)
            IndustryTools.ClearStockOrders(colony, "the industry head left");
        if (colony.Manager != null && colony.Manager.TryGetGlobalEntityById(leavingId, out var leaving)
            && leaving.IsValid && leaving.FactionOwnerID == colony.FactionOwnerID)
        {
            if (leaving.TryGetDataBlob<CommanderDB>(out var db) && db.AssignedTo == colony.Id)
                db.AssignedTo = -1;
            ShipLogBook.NoteLeftPost(colony, leaving, atDateTime, "the industry seat");
        }
    }

    /// <summary>The colony's seat datablob, created empty on first use.</summary>
    public static IndustrySeatDB Of(Entity colony)
    {
        if (!colony.TryGetDataBlob<IndustrySeatDB>(out var seat))
        {
            seat = new IndustrySeatDB();
            colony.SetDataBlob(seat);
        }
        return seat;
    }

    static string NameOf(Entity person)
    {
        if (person.TryGetDataBlob<CommanderDB>(out var db) && !string.IsNullOrEmpty(db.Name))
            return db.Name;
        return "This person";
    }
}
