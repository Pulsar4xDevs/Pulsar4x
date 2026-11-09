using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;

namespace Pulsar4X.Logistics;

/// <summary>
/// Keeps the relocation offers in the market books in step with who is actually overcrowded.
///
/// One pass reads the same willingness numbers <see cref="MigrationPlanner"/> uses and then makes
/// each market say one of two things for a species: "this many people want to leave here" at a
/// crowded colony, and "this many will be taken" at the roomiest eligible colony in the same system with
/// room left. Eligible means the same faction, or another faction that has mutual
/// <see cref="Pulsar4X.Factions.FactionStanceRules.CanTrade"/> grants. Leaving asks are free; accepting
/// bids carry <see cref="SpeciesCargo.FareCreditsPerPerson"/> so the carrier is paid on delivery.
///
/// The offers are ordinary <see cref="MarketListing"/>s, so the existing freighter and trade goals
/// pick them up with no change: a ship loads at the origin through
/// <see cref="MarketExchangeAction"/> and lands at the destination through the same
/// <see cref="PassengerAdapter"/>. This class only writes listings; it moves nobody and builds no
/// actions.
///
/// It is idempotent and self-cleaning. Every run first retires every offer it posted itself
/// (<see cref="MarketListing.Relocation"/>), so a colony that is no longer crowded, a destination
/// that filled up and a duplicate left by an earlier pass all disappear. An ordinary listing for
/// the same cargo id is never touched: it is not marked as a relocation, and when one exists the
/// refresh leaves the species alone rather than fight over the id.
/// </summary>
public static class MigrationBook
{
    /// <summary>
    /// Refresh every market in the manager. Returns how many offers are in force afterwards.
    /// </summary>
    public static int Refresh(EntityManager manager)
    {
        RegisterSpecies(manager);
        Retire(manager);

        // A book holds one listing per cargo id, so a colony cannot both send people of a species
        // away and take that same species in during one pass. Sending wins, because that society is
        // the one that does not fit where it is.
        var sending = new HashSet<(int Colony, int Species)>();
        var offers = MigrationPlanner.Offers(manager);
        foreach (var offer in offers)
            sending.Add((offer.Origin.Id, offer.SpeciesId));

        int posted = 0;
        foreach (var offer in offers)
        {
            if (sending.Contains((offer.Destination.Id, offer.SpeciesId)))
                continue;
            if (offer.Leaving < MigrationPlanner.MinimumRoom || offer.Accepted < MigrationPlanner.MinimumRoom)
                continue;
            if (!manager.TryGetGlobalEntityById(offer.SpeciesId, out var species))
                continue;
            if (!species.HasDataBlob<Pulsar4X.People.SpeciesDB>())
                continue;

            var cargo = new SpeciesCargo(species);
            string cargoId = cargo.UniqueID;
            if (string.IsNullOrEmpty(cargoId))
                continue;

            // Both factions must resolve the species id before a freighter can fill either end.
            EnsureSpecies(offer.Origin, species);
            EnsureSpecies(offer.Destination, species);

            // A relocation needs both ends to be able to see the good by id.
            if (!CanResolve(offer.Origin, cargoId) || !CanResolve(offer.Destination, cargoId))
                continue;

            // An ordinary listing already owns this id at either end, so leave it alone.
            if (HasForeignListing(offer.Origin, cargoId) || HasForeignListing(offer.Destination, cargoId))
                continue;

            var leaving = new MarketListing
            {
                CargoId = cargoId,
                SellQuantity = offer.Leaving,
                Ask = 0,
                Bid = 0,
                Relocation = true,
            };
            var accepting = new MarketListing
            {
                CargoId = cargoId,
                BuyQuantity = offer.Accepted,
                Ask = 0,
                Bid = SpeciesCargo.FareCreditsPerPerson,
                Relocation = true,
            };

            bool atOrigin = MarketBook.SetListing(offer.Origin, leaving);
            bool atDestination = MarketBook.SetListing(offer.Destination, accepting);
            if (atOrigin)
                posted++;
            if (atDestination)
                posted++;
            // Only one end posted is not a relocation any more, so take it back out.
            if (atOrigin && !atDestination)
                MarketBook.RemoveListing(offer.Origin, cargoId);
            if (atDestination && !atOrigin)
                MarketBook.RemoveListing(offer.Destination, cargoId);
        }

        return posted;
    }

    /// <summary>
    /// Drop every offer this class posted. Run before posting so stale, filled and duplicated
    /// offers cannot survive a refresh.
    /// </summary>
    public static void Retire(EntityManager manager)
    {
        foreach (var market in manager.GetAllEntitiesWithDataBlob<LogiBaseDB>())
        {
            if (!market.TryGetDataBlob<LogiBaseDB>(out var book))
                continue;
            var stale = new List<string>();
            foreach (var (cargoId, listing) in book.Listings)
            {
                if (listing.Relocation)
                    stale.Add(cargoId);
            }
            foreach (var cargoId in stale)
                MarketBook.RemoveListing(market, cargoId);
        }
    }

    /// <summary>
    /// The player species is added after the faction is created, and an owned colony such as
    /// Luna Concord adds its own species the same way. Register here so a market pass can see them
    /// even when that earlier step was missed. Register is idempotent.
    /// </summary>
    static void RegisterSpecies(EntityManager manager)
    {
        if (manager.Game != null)
        {
            foreach (var faction in manager.Game.Factions.Values)
                SpeciesCargo.RegisterOwned(faction);
        }
        foreach (var faction in manager.GetAllEntitiesWithDataBlob<Factions.FactionInfoDB>())
            SpeciesCargo.RegisterOwned(faction);
    }

    static bool HasForeignListing(Entity colony, string cargoId)
    {
        return MarketBook.TryGet(colony, cargoId, out var listing) && !listing.Relocation;
    }

    static void EnsureSpecies(Entity colony, Entity species)
    {
        if (colony?.Manager?.Game == null)
            return;
        if (!colony.Manager.Game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
            return;
        if (!faction.TryGetDataBlob<Factions.FactionInfoDB>(out var info))
            return;
        SpeciesCargo.Register(info.Data, new[] { species });
    }

    static bool CanResolve(Entity colony, string cargoId)
    {
        // FactionInfoDB lives on the faction, never on the colony.
        if (colony?.Manager?.Game == null)
            return false;
        if (!colony.Manager.Game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
            return false;
        if (!faction.TryGetDataBlob<Factions.FactionInfoDB>(out var info))
            return false;
        return info.Data.CargoGoods.Contains(cargoId);
    }
}
