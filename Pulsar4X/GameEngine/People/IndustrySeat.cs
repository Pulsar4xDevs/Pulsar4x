using System;
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

    /// <summary>The seated head's entity id, or -1 when the seat is empty or the colony has none.</summary>
    public static int HeadId(Entity colony)
        => colony.TryGetDataBlob<IndustrySeatDB>(out var seat) ? seat.HeadId : IndustrySeatDB.Empty;

    /// <summary>True when the colony has a seated industry head.</summary>
    public static bool HasHead(Entity colony) => HeadId(colony) >= 0;

    /// <summary>The seated head entity, when there is one and it still resolves.</summary>
    public static bool TryGetHead(Entity colony, out Entity head)
    {
        head = default!;
        int id = HeadId(colony);
        return id >= 0 && colony.Manager != null && colony.Manager.TryGetGlobalEntityById(id, out head);
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

        if (HoldsIndustrySeat(person, out var previousColony) && previousColony.Id != colony.Id)
            ClearSeat(previousColony, atDateTime);

        if (seat.HeadId >= 0)
            ClearSeat(colony, atDateTime);

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

    static void ClearSeat(Entity colony, DateTime atDateTime)
    {
        if (!colony.TryGetDataBlob<IndustrySeatDB>(out var seat) || seat.HeadId < 0)
            return;
        int leavingId = seat.HeadId;
        seat.HeadId = IndustrySeatDB.Empty;
        if (colony.Manager != null && colony.Manager.TryGetGlobalEntityById(leavingId, out var leaving))
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
