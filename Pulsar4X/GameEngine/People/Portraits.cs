using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Technology;

namespace Pulsar4X.People;

/// <summary>
/// Portrait seeds and roles (portraits plan, engine side). A seed is the one number the Client's renderer turns into a face;
/// the engine holds no image data. Every person gets one at creation, kept for life, and it is saved. A hired candidate keeps
/// the seed they had as a candidate.
/// <para>The seed comes from its own stream, not <c>game.RNG</c>: a seed is a hash of the game's master seed and a saved
/// counter, so drawing one never moves the game's random sequence (names, rolls, damage stay as they were) and the same
/// master seed gives the same seeds in the same creation order. An old save with no seed gets one derived from the person's
/// entity id (<see cref="FromId"/>) when it loads.</para>
/// </summary>
public static class Portraits
{
    // A 32-bit finaliser (murmur3-style avalanche): small changes to the input change every output bit.
    static int Mix(uint x)
    {
        x ^= x >> 16;
        x *= 0x85EBCA6B;
        x ^= x >> 13;
        x *= 0xC2B2AE35;
        x ^= x >> 16;
        int result = unchecked((int)x);
        return result == 0 ? 1 : result; // 0 means "not set yet", so a real seed is never 0
    }

    /// <summary>The seed for the n-th person created in a game with this master seed. Never 0.</summary>
    internal static int Derive(int masterSeed, int draw)
        => Mix(unchecked((uint)masterSeed * 0x9E3779B1u + (uint)draw * 0x7FEB352Du + 0x68E31DA4u));

    /// <summary>The seed an old save's person gets from their entity (or candidate) id. Same id, same seed. Never 0.</summary>
    public static int FromId(int id) => Mix(unchecked((uint)id ^ 0x5BD1E995u));

    /// <summary>A person's seed: the stored one, or the id-derived one when none is stored yet.</summary>
    public static int SeedOf(int entityId, int stored) => stored != 0 ? stored : FromId(entityId);

    public static int SeedOf(Entity person)
        => person.TryGetDataBlob<CommanderDB>(out var db) ? SeedOf(person.Id, db.PortraitSeed) : FromId(person.Id);

    /// <summary>The pack a person with no species and no faction species gets.</summary>
    public const string DefaultPack = "human";

    /// <summary>The portrait pack of a species entity, or null when the id does not resolve to a species.</summary>
    static string? PackOfSpecies(EntityManager? manager, int speciesId)
    {
        if (speciesId < 0 || manager == null || !manager.TryGetGlobalEntityById(speciesId, out var species)
            || !species.TryGetDataBlob<SpeciesDB>(out var db))
            return null;
        return string.IsNullOrWhiteSpace(db.PortraitPack) ? DefaultPack : db.PortraitPack;
    }

    /// <summary>
    /// The portrait pack for a person, derived each time from species data (never saved per person): their own species if they
    /// have one that still exists, else the owning faction's primary species (the first on its list), else "human".
    /// </summary>
    public static string PackFor(EntityManager? manager, int speciesId, int factionId)
    {
        var own = PackOfSpecies(manager, speciesId);
        if (own != null)
            return own;
        if (manager != null && factionId >= 0 && manager.TryGetGlobalEntityById(factionId, out var faction)
            && faction.TryGetDataBlob<FactionInfoDB>(out var info))
        {
            foreach (var species in info.Species)
            {
                var pack = PackOfSpecies(manager, species.Id);
                if (pack != null)
                    return pack;
            }
        }
        return DefaultPack;
    }

    /// <summary>The pack for a commander entity: their <see cref="CommanderDB.SpeciesId"/>, else their faction's (or <paramref name="fallbackFactionId"/>'s) primary species.</summary>
    public static string PackFor(Entity person, int fallbackFactionId = -1)
    {
        int speciesId = person.TryGetDataBlob<CommanderDB>(out var db) ? db.SpeciesId : -1;
        int factionId = person.FactionOwnerID >= 0 ? person.FactionOwnerID : fallbackFactionId;
        return PackFor(person.Manager, speciesId, factionId);
    }

    /// <summary>A candidate's seed, or the id-derived one for a candidate saved before seeds existed.</summary>
    public static int SeedOf(Candidate candidate) => SeedOf(candidate.Id, candidate.PortraitSeed);

    /// <summary>A lab technician's seed, or the id-derived one for a technician saved before seeds existed.</summary>
    public static int SeedOf(Scientist scientist) => SeedOf(scientist.ID, scientist.PortraitSeed);

    /// <summary>
    /// The role that picks the person's uniform, from their current post:
    /// <list type="bullet">
    /// <item><description><c>industry-head</c>: seated as a colony's industry head.</description></item>
    /// <item><description><c>captain</c>: a navy or ground officer (and anyone passed as a ship's captain by the caller).</description></item>
    /// <item><description><c>scientist</c>: a scientist commander.</description></item>
    /// <item><description><c>administrator</c>: a civilian in a city-hall (or bridge admin) chair.</description></item>
    /// <item><description><c>civilian</c>: any other civilian, including a hired administrator nobody has seated and a head who was unseated.</description></item>
    /// </list>
    /// <c>candidate</c> and <c>lab-technician</c> belong to the candidate and lab-technician objects, not to commanders.
    /// </summary>
    public static string RoleOf(Entity person)
    {
        if (!person.TryGetDataBlob<CommanderDB>(out var db))
            return PortraitRoles.Civilian;
        if (GameEngine.People.IndustrySeat.HoldsIndustrySeat(person, out _))
            return PortraitRoles.IndustryHead;
        switch (db.Type)
        {
            case CommanderTypes.Navy:
            case CommanderTypes.Ground:
                return PortraitRoles.Captain;
            case CommanderTypes.Scientist:
                return PortraitRoles.Scientist;
        }
        return GameEngine.People.IndustrySeat.HoldsChair(person) ? PortraitRoles.Administrator : PortraitRoles.Civilian;
    }

    /// <summary>
    /// After a load: a person saved before seeds existed (stored seed 0) gets the seed derived from their id, so the face is the same
    /// on every load of that save. Covers commanders, candidates in colony pools and lab technicians.
    /// </summary>
    internal static void AfterLoad(Game game)
    {
        foreach (var manager in game.GlobalManagerDictionary.Values)
        {
            foreach (var person in manager.GetAllEntitiesWithDataBlob<CommanderDB>())
            {
                var db = person.GetDataBlob<CommanderDB>();
                if (db.PortraitSeed == 0)
                    db.PortraitSeed = FromId(person.Id);
            }
            foreach (var colony in manager.GetAllEntitiesWithDataBlob<CandidatePoolDB>())
            {
                foreach (var candidate in colony.GetDataBlob<CandidatePoolDB>().Candidates)
                {
                    if (candidate.PortraitSeed == 0)
                        candidate.PortraitSeed = FromId(candidate.Id);
                }
            }
        }
        foreach (var faction in game.Factions.Values)
        {
            if (!faction.TryGetDataBlob<FactionTechDB>(out var tech))
                continue;
            foreach (var (scientist, _) in tech.AllScientists)
            {
                if (scientist.PortraitSeed == 0)
                    scientist.PortraitSeed = FromId(scientist.ID);
            }
        }
    }
}
