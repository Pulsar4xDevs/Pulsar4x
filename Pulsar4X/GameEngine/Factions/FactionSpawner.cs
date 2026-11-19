using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Fleets;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Factions
{
    /// <summary>
    /// Multi-faction phase 9: monthly check that charters a company when a system's unmet demand has
    /// sat at the full shortage premium for 180 days with no seller, and folds a company back into
    /// its sponsor after six months broke with no stock.
    /// </summary>
    public static class FactionSpawner
    {
        public const int UnmetDays = 180;
        public const int CooldownDays = 180;
        public const int FoldDays = 180;
        public const int SeedPeople = 5000;
        public const decimal SeedFunds = 5_000_000m;
        public const int FactionHeadroom = 4;
        public const int DefaultMaxSpawned = 6;
        public const string SettingsId = "npc-company-settings";

        public static void TickSystem(Game game, StarSystem system, DateTime now)
        {
            if (game == null || system == null)
                return;
            game.NpcCompanies ??= new NpcCompanyBook();
            FoldDue(game, system, now);
            ConsiderSpawn(game, system, now);
        }

        /// <summary>Test helper: run the monthly check on every system.</summary>
        public static void Tick(Game game, DateTime now)
        {
            if (game == null)
                return;
            foreach (var system in game.Systems.OrderBy(s => s.ID, StringComparer.Ordinal))
                TickSystem(game, system, now);
        }

        /// <summary>Test helper: charter a company for this good, still applying caps, funds and the kit.</summary>
        public static Entity? TrySpawn(Game game, StarSystem system, string cargoId, Entity sponsorColony, DateTime now)
            => Spawn(game, system, cargoId, sponsorColony, now, forced: true);

        static void ConsiderSpawn(Game game, StarSystem system, DateTime now)
        {
            if (!CanSpawnIn(game, system, now, out _))
                return;

            var unmet = UnmetPremiumGoods(game, system, now);
            var book = game.NpcCompanies;
            string prefix = system.ID + ":";
            foreach (var key in book.UnmetSince.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                var cargoId = key.Substring(prefix.Length);
                if (!unmet.ContainsKey(cargoId))
                    book.UnmetSince.Remove(key);
            }

            string? chosen = null;
            long bestBuy = -1;
            foreach (var (cargoId, state) in unmet.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                string key = prefix + cargoId;
                if (!book.UnmetSince.ContainsKey(key))
                    book.UnmetSince[key] = now;
                var since = book.UnmetSince[key];
                if ((now - since).TotalDays + 0.001 < UnmetDays)
                    continue;
                if (state.TotalBuy > bestBuy)
                {
                    bestBuy = state.TotalBuy;
                    chosen = cargoId;
                }
            }
            if (chosen == null)
                return;
            var sponsor = unmet[chosen].Sponsor;
            if (sponsor == null)
                return;
            if (Spawn(game, system, chosen, sponsor, now, forced: false) != null)
                book.UnmetSince.Remove(prefix + chosen);
        }

        static bool CanSpawnIn(Game game, StarSystem system, DateTime now, out string? reason)
        {
            reason = null;
            int live = game.NpcCompanies.Spawned.Count(s => game.Factions.ContainsKey(s.FactionId));
            int cap = game.Settings?.MaxSpawnedFactions > 0 ? game.Settings.MaxSpawnedFactions : DefaultMaxSpawned;
            if (live >= cap)
            {
                reason = "game cap";
                return false;
            }
            if (game.Factions.Count >= Game.MaxFactions - FactionHeadroom)
            {
                reason = "faction mask headroom";
                return false;
            }
            // Mask indexes are never recycled, so the live faction count can be well below 32
            // after folds while the next allocation would still throw.
            if (game.NextFactionMaskIndex >= Game.MaxFactions - FactionHeadroom)
            {
                reason = "faction mask exhausted";
                return false;
            }
            if (game.NpcCompanies.LastSpawnBySystem.TryGetValue(system.ID, out var last)
                && (now - last).TotalDays + 0.001 < CooldownDays)
            {
                reason = "system cooldown";
                return false;
            }
            return true;
        }

        sealed class DemandState
        {
            public long TotalBuy;
            public Entity? Sponsor;
            public long SponsorBuy;
        }

        static Dictionary<string, DemandState> UnmetPremiumGoods(Game game, StarSystem system, DateTime now)
        {
            var buys = new Dictionary<string, DemandState>(StringComparer.Ordinal);
            var selling = new HashSet<string>(StringComparer.Ordinal);
            var premium = new HashSet<string>(StringComparer.Ordinal);

            foreach (var colony in Colonies(system))
            {
                if (colony.TryGetDataBlob<LogiBaseDB>(out var book))
                {
                    foreach (var listing in book.Listings.Values)
                    {
                        if (listing.Relocation || string.IsNullOrEmpty(listing.CargoId))
                            continue;
                        if (listing.SellQuantity > 0)
                            selling.Add(listing.CargoId);
                        if (listing.BuyQuantity > 0)
                        {
                            if (!buys.TryGetValue(listing.CargoId, out var state))
                                buys[listing.CargoId] = state = new DemandState();
                            state.TotalBuy += listing.BuyQuantity;
                            if (listing.BuyQuantity > state.SponsorBuy
                                || (listing.BuyQuantity == state.SponsorBuy && (state.Sponsor == null || colony.Id < state.Sponsor.Id)))
                            {
                                if (IsBloc(game, colony))
                                {
                                    state.SponsorBuy = listing.BuyQuantity;
                                    state.Sponsor = colony;
                                }
                            }
                        }
                    }
                }
                if (colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
                {
                    foreach (var row in policy.Rows.Values)
                    {
                        if (row.BidFactor > 0 && BlocDemand.Premium(row, now) >= 2)
                            premium.Add(row.CargoId);
                    }
                }
            }

            var result = new Dictionary<string, DemandState>(StringComparer.Ordinal);
            foreach (var (id, state) in buys)
            {
                if (state.TotalBuy <= 0 || selling.Contains(id) || !premium.Contains(id) || state.Sponsor == null)
                    continue;
                result[id] = state;
            }
            return result;
        }

        static bool IsBloc(Game game, Entity colony)
            => game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
               && faction.TryGetDataBlob<FactionInfoDB>(out var info)
               && info.NpcEconomy
               && info.SponsorFactionId == 0;

        static Entity? Spawn(Game game, StarSystem system, string cargoId, Entity sponsorColony, DateTime now, bool forced)
        {
            if (!CanSpawnIn(game, system, now, out _))
                return null;
            if (!game.Factions.TryGetValue(sponsorColony.FactionOwnerID, out var sponsor)
                || !sponsor.TryGetDataBlob<FactionInfoDB>(out var sponsorInfo))
                return null;
            if (sponsorInfo.Money.GetCurrentFunds() < SeedFunds)
                return null;
            if (!TryTakePeople(sponsorColony, SeedPeople, out var species, out _))
                return null;

            var kit = KitFor(game, cargoId);
            if (kit == null)
            {
                ReturnPeople(sponsorColony, species, SeedPeople);
                return null;
            }

            var body = PlaceOn(game, system, sponsor, sponsorColony, cargoId, kit);
            if (body == null)
            {
                ReturnPeople(sponsorColony, species, SeedPeople);
                return null;
            }

            string goodName = GoodName(sponsorInfo.Data, cargoId);
            string companyName = UniqueName(game, CompanyName(game, sponsor, goodName));
            string abbr = Abbreviation(companyName);

            var blueprint = new ColonyBlueprint
            {
                UniqueID = "npc-company-" + cargoId,
                Name = companyName,
                OwnerFaction = companyName,
                StartingPopulation = SeedPeople,
                StartingCrewPool = 0,
                Body = body.GetDataBlob<NameDB>().DefaultName,
                System = system.ID,
                Installations = kit.Installations,
                Cargo = kit.Cargo,
                ComponentDesigns = kit.Installations?.Select(i => i.Id).Distinct(StringComparer.Ordinal).ToList(),
                Market = new List<ColonyBlueprint.MarketRowBlueprint>
                {
                    new() { CargoId = cargoId, Role = "Sell" }
                },
                SeatAdministrator = true,
                SeatIndustryHead = true,
                TechFromStartingItems = true,
            };

            Entity? company = null;
            Entity colony;
            try
            {
                sponsorInfo.Money.AddExpense(now, TransactionCategory.Charter,
                    $"Charter of {companyName} to supply {goodName}", SeedFunds);

                // CreateBasicFaction allocates a mask index and throws once 32 have ever been handed out.
                company = FactionFactory.CreateBasicFaction(game, companyName, abbr, 0);
                company.FactionOwnerID = company.Id;
                var info = company.GetDataBlob<FactionInfoDB>();
                info.AutoHires = true;
                info.NpcEconomy = false;
                info.SponsorFactionId = sponsor.Id;
                info.SpawnedGood = cargoId;
                info.Money.AddIncome(now, TransactionCategory.Charter,
                    $"Seed funds from {sponsor.GetDataBlob<NameDB>().DefaultName}", SeedFunds);
                if (!info.KnownSystems.Contains(system.ID))
                    info.KnownSystems.Add(system.ID);
                if (!info.Species.Contains(species))
                    info.Species.Add(species);
                SpeciesCargo.Register(info.Data, info.Species);

                CopyLibraries(sponsorInfo, info);
                SeedStances(game, company, info);

                colony = ColonyFactory.CreateFromBlueprint(game, company, species, system, body, blueprint);
            }
            catch
            {
                ReturnPeople(sponsorColony, species, SeedPeople);
                sponsorInfo.Money.AddIncome(now, TransactionCategory.Charter, $"Refund: {companyName} failed to found", SeedFunds);
                if (company != null)
                {
                    if (company.TryGetDataBlob<FactionInfoDB>(out var failed))
                    {
                        decimal leftover = failed.Money.GetCurrentFunds();
                        if (leftover > 0)
                            failed.Money.AddExpense(now, TransactionCategory.Charter, "Founding failed", leftover);
                    }
                    game.Factions.Remove(company.Id);
                }
                throw;
            }

            game.NpcCompanies.LastSpawnBySystem[system.ID] = now;
            game.NpcCompanies.Spawned.Add(new SpawnedCompany
            {
                FactionId = company.Id,
                SponsorFactionId = sponsor.Id,
                ColonyId = colony.Id,
                SystemId = system.ID,
                CargoId = cargoId,
                SpawnedAt = now,
            });

            string message = $"{sponsor.GetDataBlob<NameDB>().DefaultName} has chartered {companyName} to supply {goodName}.";
            EventManager.Instance.Publish(Event.Create(
                EventType.NpcCompanySpawned, now, message, company.Id, system.ID, colony.Id,
                game.Factions.Keys.ToList()));
            return colony;
        }

        static void FoldDue(Game game, StarSystem system, DateTime now)
        {
            foreach (var record in game.NpcCompanies.Spawned.Where(s => s.SystemId == system.ID).ToList())
            {
                if (!game.Factions.TryGetValue(record.FactionId, out var company)
                    || !company.TryGetDataBlob<FactionInfoDB>(out var info))
                {
                    game.NpcCompanies.Spawned.Remove(record);
                    continue;
                }
                var colony = info.Colonies.FirstOrDefault(c => c.Id == record.ColonyId) ?? info.Colonies.FirstOrDefault();
                bool broke = info.Money.GetCurrentFunds() <= 0 && HasNoStock(colony, info);
                if (!broke)
                {
                    record.BrokeSince = null;
                    continue;
                }
                record.BrokeSince ??= now;
                if ((now - record.BrokeSince.Value).TotalDays + 0.001 < FoldDays)
                    continue;
                Fold(game, record, company, info, colony, now);
            }
        }

        static void Fold(Game game, SpawnedCompany record, Entity company, FactionInfoDB info, Entity? colony, DateTime now)
        {
            if (!game.Factions.TryGetValue(record.SponsorFactionId, out var sponsor)
                || !sponsor.TryGetDataBlob<FactionInfoDB>(out var sponsorInfo))
                return;

            if (colony != null)
            {
                info.Colonies.Remove(colony);
                if (!sponsorInfo.Colonies.Contains(colony))
                    sponsorInfo.Colonies.Add(colony);
                Rehome(colony, company, sponsor);
                RehomeColonyPeople(colony, info, sponsorInfo, company, sponsor);
                if (colony.TryGetDataBlob<ColonyInfoDB>(out var colonyInfo)
                    && colonyInfo.PlanetEntity.TryGetDataBlob<MineralsDB>(out var minerals))
                    minerals.GrantFactionAccess(sponsorInfo.FactionMask);
            }

            RehomeShipsAndFleets(game, company, sponsor);
            RetargetShipments(game, company.Id, sponsor.Id);
            MoveCommuters(game, company.Id, sponsor.Id);

            decimal leftover = info.Money.GetCurrentFunds();
            if (leftover > 0)
            {
                info.Money.AddExpense(now, TransactionCategory.Charter, "Fold back into sponsor", leftover);
                sponsorInfo.Money.AddIncome(now, TransactionCategory.Charter,
                    $"Fold of {company.GetDataBlob<NameDB>().DefaultName}", leftover);
            }

            string message = $"{company.GetDataBlob<NameDB>().DefaultName} has folded back into {sponsor.GetDataBlob<NameDB>().DefaultName}.";
            EventManager.Instance.Publish(Event.Create(
                EventType.NpcCompanyFolded, now, message, sponsor.Id, record.SystemId, colony?.Id,
                game.Factions.Keys.ToList()));

            info.EventLog?.Unsubscribe();
            game.Factions.Remove(company.Id);
            game.NpcCompanies.Spawned.Remove(record);
            foreach (var other in game.Factions.Values)
            {
                if (!other.TryGetDataBlob<FactionInfoDB>(out var otherInfo))
                    continue;
                otherInfo.Stances.Remove(company.Id);
                otherInfo.Grants.Remove(company.Id);
            }
        }

        /// <summary>
        /// The people who sit the colony's posts (city-hall chairs and the industry seat) keep their posts and now
        /// work for the sponsor. An industry seat whose head cannot be found is emptied, so it never points at
        /// someone of the folded faction.
        /// </summary>
        static void RehomeColonyPeople(Entity colony, FactionInfoDB from, FactionInfoDB to, Entity company, Entity sponsor)
        {
            var people = new Dictionary<int, Entity>();
            foreach (var commander in from.Commanders.ToList())
            {
                if (commander.TryGetDataBlob<CommanderDB>(out var db) && db.AssignedTo == colony.Id)
                    people[commander.Id] = commander;
            }
            if (colony.TryGetDataBlob<IndustrySeatDB>(out var seat) && seat.HeadId >= 0
                && colony.Manager != null && colony.Manager.TryGetGlobalEntityById(seat.HeadId, out var head)
                && head.FactionOwnerID == company.Id)
                people[head.Id] = head;
            if (colony.TryGetDataBlob<AdminSpaceDB>(out var admin))
            {
                foreach (var chair in admin.CommanderSeats)
                {
                    if (chair.CommanderID >= 0 && colony.Manager != null
                        && colony.Manager.TryGetGlobalEntityById(chair.CommanderID, out var holder)
                        && holder.FactionOwnerID == company.Id)
                        people[holder.Id] = holder;
                }
            }

            foreach (var person in people.Values)
            {
                from.Commanders.Remove(person);
                if (!to.Commanders.Contains(person))
                    to.Commanders.Add(person);
                person.FactionOwnerID = sponsor.Id;
                if (person.TryGetDataBlob<NameDB>(out var names))
                {
                    string name = names.GetName(company.Id);
                    if (string.IsNullOrEmpty(name))
                        name = names.DefaultName;
                    if (!string.IsNullOrEmpty(name))
                        names.SetName(sponsor.Id, name);
                }
            }

            IndustrySeat.ClearIfDangling(colony);
        }

        static void Rehome(Entity entity, Entity from, Entity to)
        {
            from.GetDataBlob<FactionOwnerDB>().RemoveEntity(entity);
            to.GetDataBlob<FactionOwnerDB>().SetOwned(entity);
            entity.FactionOwnerID = to.Id;
            if (entity.TryGetDataBlob<NameDB>(out var names))
            {
                string name = names.GetName(from.Id);
                if (string.IsNullOrEmpty(name))
                    name = names.DefaultName;
                if (!string.IsNullOrEmpty(name))
                    names.SetName(to.Id, name);
            }
        }

        static void RehomeShipsAndFleets(Game game, Entity company, Entity sponsor)
        {
            var seen = new HashSet<int>();
            void Take(Entity entity)
            {
                if (entity == null || entity.Id == company.Id || !seen.Add(entity.Id))
                    return;
                if (entity.FactionOwnerID == company.Id || company.GetDataBlob<FactionOwnerDB>().OwnedEntities.ContainsKey(entity.Id))
                    Rehome(entity, company, sponsor);
            }

            foreach (var entity in company.GetDataBlob<FactionOwnerDB>().OwnedEntities.Values.ToList())
                Take(entity);
            foreach (var system in game.Systems)
            {
                foreach (var ship in system.GetAllEntitiesWithDataBlob<ShipInfoDB>())
                    Take(ship);
                foreach (var fleet in system.GetAllEntitiesWithDataBlob<FleetDB>())
                    Take(fleet);
            }
        }

        static void RetargetShipments(Game game, int companyId, int sponsorId)
        {
            foreach (var system in game.Systems)
            {
                foreach (var holder in system.GetAllEntitiesWithDataBlob<CargoStorageDB>())
                {
                    foreach (var shipment in ShipmentBook.At(holder))
                    {
                        if (shipment.BuyerFactionId != companyId && shipment.SellerFactionId != companyId)
                            continue;
                        if (shipment.BuyerFactionId == companyId)
                            shipment.BuyerFactionId = sponsorId;
                        if (shipment.SellerFactionId == companyId)
                            shipment.SellerFactionId = sponsorId;
                    }
                }
            }
        }

        static void MoveCommuters(Game game, int companyId, int sponsorId)
        {
            foreach (var system in game.Systems)
            {
                foreach (var colony in system.GetAllEntitiesWithDataBlob<ColonyWorkforceDB>())
                {
                    var db = colony.GetDataBlob<ColonyWorkforceDB>();
                    if (!db.Commuters.TryGetValue(companyId, out var n) || n == 0)
                    {
                        db.Commuters.Remove(companyId);
                        continue;
                    }
                    db.Commuters.Remove(companyId);
                    if (sponsorId == companyId)
                        continue;
                    db.Commuters.TryGetValue(sponsorId, out var have);
                    db.Commuters[sponsorId] = have + n;
                }
            }
        }

        static bool HasNoStock(Entity? colony, FactionInfoDB info)
        {
            if (colony == null || !colony.TryGetDataBlob<CargoStorageDB>(out var storage))
                return true;
            foreach (var cargo in info.Data.CargoGoods.GetAll().Values)
            {
                if (storage.GetUnitsStored(cargo, includeEscro: false) > 0)
                    return false;
            }
            return true;
        }

        static NpcCompanyKitBlueprint? KitFor(Game game, string cargoId)
        {
            var kits = game.StartingGameData?.NpcCompanyKits;
            if (kits == null || kits.Count == 0)
                return null;
            foreach (var kit in kits.Values)
            {
                if (kit.Goods != null && kit.Goods.Any(g => string.Equals(g, cargoId, StringComparison.Ordinal)))
                    return kit;
            }
            bool mineral = game.StartingGameData.Minerals.ContainsKey(cargoId)
                || kits.Values.Any(k => string.Equals(k.Category, "mining", StringComparison.OrdinalIgnoreCase)
                    && game.Factions.Values.Select(f => f.GetDataBlob<FactionInfoDB>().Data)
                        .Any(d => d.CargoGoods.IsMineral(cargoId) || d.LockedCargoGoods.IsMineral(cargoId)));
            // Cheaper: ask any faction store, else mining if it looks like a mineral unique id in Minerals values.
            foreach (var faction in game.Factions.Values)
            {
                if (faction.TryGetDataBlob<FactionInfoDB>(out var info)
                    && (info.Data.CargoGoods.IsMineral(cargoId) || info.Data.LockedCargoGoods.IsMineral(cargoId)))
                {
                    return kits.Values.FirstOrDefault(k => string.Equals(k.Category, "mining", StringComparison.OrdinalIgnoreCase));
                }
            }
            return kits.Values.FirstOrDefault(k => string.Equals(k.Category, "manufacturing", StringComparison.OrdinalIgnoreCase))
                ?? kits.Values.FirstOrDefault();
        }

        static Entity? PlaceOn(Game game, StarSystem system, Entity sponsor, Entity sponsorColony, string cargoId, NpcCompanyKitBlueprint kit)
        {
            var sponsorBody = sponsorColony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
            if (CanProduce(sponsor, sponsorBody, cargoId, kit))
                return sponsorBody;
            var info = sponsor.GetDataBlob<FactionInfoDB>();
            Entity? best = null;
            long bestAmount = -1;
            foreach (var body in system.GetAllEntitiesWithDataBlob<MineralsDB>())
            {
                if (!HasDeposit(info, body, cargoId, out long amount))
                    continue;
                if (amount > bestAmount || (amount == bestAmount && (best == null || body.Id < best.Id)))
                {
                    bestAmount = amount;
                    best = body;
                }
            }
            return best ?? (CanProduce(sponsor, sponsorBody, cargoId, kit) ? sponsorBody : sponsorBody);
        }

        static bool CanProduce(Entity sponsor, Entity body, string cargoId, NpcCompanyKitBlueprint kit)
        {
            string category = kit.Category ?? "";
            if (string.Equals(category, "mining", StringComparison.OrdinalIgnoreCase))
                return HasDeposit(sponsor.GetDataBlob<FactionInfoDB>(), body, cargoId, out _);
            // Refining, farming and manufacturing can be done on the sponsor's body.
            return true;
        }

        static bool HasDeposit(FactionInfoDB info, Entity body, string cargoId, out long amount)
        {
            amount = 0;
            if (!body.TryGetDataBlob<MineralsDB>(out var minerals))
                return false;
            int id;
            try
            {
                if (info.Data.CargoGoods.IsMineral(cargoId))
                    id = info.Data.CargoGoods.GetMineral(cargoId).ID;
                else if (info.Data.LockedCargoGoods.IsMineral(cargoId))
                    id = info.Data.LockedCargoGoods.GetMineral(cargoId).ID;
                else
                    return false;
            }
            catch
            {
                return false;
            }
            if (!minerals.Minerals.TryGetValue(id, out var deposit))
                return false;
            amount = deposit.Amount.For(info.FactionMask) ?? 0;
            return amount > 0 || deposit.Accessibility > 0;
        }

        static void CopyLibraries(FactionInfoDB from, FactionInfoDB to)
        {
            foreach (var id in from.Data.Techs.Keys.ToList())
            {
                if (to.Data.LockedTechs.ContainsKey(id))
                    to.Data.Unlock(id);
                if (!to.Data.Techs.ContainsKey(id))
                    continue;
                int target = from.Data.Techs[id].Level;
                while (to.Data.Techs[id].Level < target)
                    to.Data.IncrementTechLevel(id);
            }
            foreach (var cargo in from.Data.CargoGoods.GetAll().Values)
            {
                if (to.Data.LockedCargoGoods.Contains(cargo.UniqueID))
                    to.Data.Unlock(cargo.UniqueID);
            }
            foreach (var id in from.Data.ComponentTemplates.Keys.ToList())
            {
                if (to.Data.LockedComponentTemplates.ContainsKey(id))
                    to.Data.Unlock(id);
            }
            foreach (var id in from.Data.IndustryTypes.Keys.ToList())
            {
                if (to.Data.LockedIndustryTypes.ContainsKey(id))
                    to.Data.Unlock(id);
            }
            foreach (var id in from.Data.Armor.Keys.ToList())
            {
                if (to.Data.LockedArmor.ContainsKey(id))
                    to.Data.Unlock(id);
            }
            foreach (var id in from.Data.CargoTypes.Keys.ToList())
            {
                if (to.Data.LockedCargoTypes.ContainsKey(id))
                    to.Data.Unlock(id);
            }
        }

        static void SeedStances(Game game, Entity company, FactionInfoDB info)
        {
            foreach (var other in game.Factions.Values)
            {
                if (other.Id == company.Id)
                    continue;
                if (!other.TryGetDataBlob<FactionInfoDB>(out var otherInfo))
                    continue;
                info.Stances[other.Id] = FactionStance.Friendly;
                otherInfo.Stances[company.Id] = FactionStance.Friendly;
                FactionStanceRules.SeedFromStance(info, other.Id, FactionStance.Friendly);
                FactionStanceRules.SeedFromStance(otherInfo, company.Id, FactionStance.Friendly);
            }
        }

        static bool TryTakePeople(Entity colony, long count, out Entity species, out int speciesId)
        {
            species = null!;
            speciesId = 0;
            var pop = colony.GetDataBlob<ColonyInfoDB>().Population;
            var pair = pop.OrderByDescending(p => p.Value).ThenBy(p => p.Key).FirstOrDefault(p => p.Value >= count);
            if (pair.Value < count)
                return false;
            speciesId = pair.Key;
            pop[speciesId] = pair.Value - count;
            if (!colony.Manager.TryGetEntityById(speciesId, out species))
                return false;
            return true;
        }

        static void ReturnPeople(Entity colony, Entity species, long count)
        {
            if (colony == null || species == null)
                return;
            var pop = colony.GetDataBlob<ColonyInfoDB>().Population;
            pop.TryGetValue(species.Id, out long have);
            pop[species.Id] = have + count;
        }

        static string CompanyName(Game game, Entity sponsor, string goodName)
        {
            var settings = Settings(game);
            string template = string.IsNullOrEmpty(settings?.NameTemplate) ? "{Region} {Good} Co." : settings.NameTemplate;
            string sponsorName = sponsor.GetDataBlob<NameDB>().DefaultName;
            string region = sponsorName;
            if (settings?.Regions != null && settings.Regions.TryGetValue(sponsorName, out var mapped) && !string.IsNullOrEmpty(mapped))
                region = mapped;
            else if (!string.IsNullOrEmpty(sponsorName))
                region = sponsorName.Split(' ')[0];
            return template.Replace("{Region}", region).Replace("{Good}", goodName);
        }

        static string UniqueName(Game game, string name)
        {
            if (game.Factions.Values.All(f => f.GetDataBlob<NameDB>().DefaultName != name))
                return name;
            for (int n = 2; n < 100; n++)
            {
                string candidate = name + " " + n;
                if (game.Factions.Values.All(f => f.GetDataBlob<NameDB>().DefaultName != candidate))
                    return candidate;
            }
            return name + " " + game.Factions.Count;
        }

        static string Abbreviation(string name)
        {
            var parts = name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var letters = new string(parts.Where(p => p.Length > 0 && char.IsLetter(p[0])).Select(p => char.ToUpperInvariant(p[0])).Take(4).ToArray());
            return string.IsNullOrEmpty(letters) ? "CO" : letters;
        }

        static string GoodName(FactionDataStore data, string cargoId)
        {
            string name = data.GetName(cargoId);
            return string.IsNullOrEmpty(name) ? cargoId : name;
        }

        static NpcCompanySettingsBlueprint? Settings(Game game)
        {
            var store = game.StartingGameData;
            if (store?.NpcCompanySettings != null && store.NpcCompanySettings.TryGetValue(SettingsId, out var s))
                return s;
            return store?.NpcCompanySettings?.Values.FirstOrDefault();
        }

        static List<Entity> Colonies(StarSystem system)
            => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>();
    }
}
