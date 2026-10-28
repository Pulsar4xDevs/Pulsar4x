using System;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Messaging;
using Pulsar4X.Ships;

namespace Pulsar4X.People;

/// <summary>
/// An academy sits idle until the colony orders one course. The processor graduates that course and does not start another.
/// </summary>
public static class NavalAcademyCourse
{
    public const string LargerClassesTech = "tech-academy-larger-classes";
    public const string InstructionTech = "tech-academy-better-instruction";
    public const string CandidateSearchTech = "tech-academy-candidate-search";

    public static int SchoolMean(int months, int instruction)
        => Math.Clamp(100 + (months - 24) + instruction, 0, 300);

    public static int InstructionOf(Entity colony)
        => TechLevel(colony, InstructionTech) * 12;

    public static int PeoplePerEnsign(Entity colony)
        => Math.Max(100, 1000 - 250 * TechLevel(colony, CandidateSearchTech));

    public static long PopulationOf(ColonyInfoDB info)
    {
        long total = 0;
        foreach (var count in info.Population.Values)
            total += count;
        return total;
    }

    public static int TotalExperience(CommanderDB officer)
    {
        int skills = 0;
        foreach (var value in officer.Skills.Values)
            skills += value;
        return Math.Max(officer.Experience, skills);
    }

    /// <summary>Null when the course may run. Otherwise the sentence the button shows.</summary>
    public static string? CommandRefusal(CommanderDB officer, int mean)
    {
        if (officer.ExperienceCap < mean)
            return null;
        int experience = TotalExperience(officer);
        if (officer.Rank < 4)
            return experience >= 8 ? null : "Experience " + experience + ". This course needs 8.";
        if (officer.Rank <= 5)
            return experience >= 16 ? null : "Experience " + experience + ". This course needs 16.";
        return "Rank is already 6. This course cannot raise it further.";
    }

    public static string? TryTrain(Entity colony, int componentId)
    {
        if (!TryIdle(colony, componentId, out var db, out int index, out var reason))
            return reason;

        if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return "The colony has no population.";

        var row = db.Academies[index];
        long people = PopulationOf(info);
        long need = (long)row.ClassSize * PeoplePerEnsign(colony);
        if (people < need)
            return "The colony has " + people + " people. This class needs " + need + ".";

        row.CourseKind = AcademyCourseKind.Class;
        row.PopulationTaken = row.ClassSize;
        row.SpeciesId = TakePeople(info, row.ClassSize);
        row.OfficerId = -1;
        row.GraduationDate = colony.StarSysDateTime.AddDays(row.TrainingPeriodInMonths * 30);
        db.Academies[index] = row;
        Schedule(colony, row.GraduationDate);
        Publish(colony, personnel: false);
        return null;
    }

    public static string? TryCommand(Entity colony, int componentId, Entity officer)
    {
        if (!TryIdle(colony, componentId, out var db, out int index, out var reason))
            return reason;
        if (!officer.TryGetDataBlob<CommanderDB>(out var commander))
            return "This officer is assigned.";
        if (commander.Type != CommanderTypes.Navy)
            return "Only a navy officer can take a command course.";
        if (IsSeated(officer))
            return "This officer is assigned.";
        if (officer.Manager?.Game != null && IsEnrolled(officer.Manager.Game, officer.Id))
            return "This officer is already on a course.";

        var row = db.Academies[index];
        int mean = SchoolMean(row.TrainingPeriodInMonths, InstructionOf(colony));
        var refusal = CommandRefusal(commander, mean);
        if (refusal != null)
            return refusal;

        row.CourseKind = AcademyCourseKind.Command;
        row.OfficerId = officer.Id;
        row.PopulationTaken = 0;
        row.SpeciesId = -1;
        row.GraduationDate = colony.StarSysDateTime.AddDays(row.TrainingPeriodInMonths * 30);
        db.Academies[index] = row;
        Schedule(colony, row.GraduationDate);
        Publish(colony, personnel: false);
        return null;
    }

    public static string? TryCancel(Entity colony, int componentId)
    {
        if (!colony.TryGetDataBlob<NavalAcademyDB>(out var db) || !TryIndex(db, componentId, out int index))
            return "No such academy.";
        var row = db.Academies[index];
        if (row.CourseKind == AcademyCourseKind.Idle)
            return "This academy is idle.";
        if (row.CourseKind == AcademyCourseKind.Class)
            ReturnPeople(colony, row);
        Clear(db, index, row);
        Publish(colony, personnel: false);
        return null;
    }

    public static void ReturnPeople(Entity colony, NavalAcademy row)
    {
        if (row.PopulationTaken <= 0 || row.SpeciesId < 0)
            return;
        if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return;
        info.Population.TryGetValue(row.SpeciesId, out long have);
        info.Population[row.SpeciesId] = have + row.PopulationTaken;
    }

    public static void Graduate(Entity colony, DateTime at)
    {
        if (!colony.TryGetDataBlob<NavalAcademyDB>(out var db))
            return;

        bool any = false;
        for (int i = 0; i < db.Academies.Count; i++)
        {
            var row = db.Academies[i];
            if (row.CourseKind == AcademyCourseKind.Idle || row.GraduationDate.Date != at.Date)
                continue;
            if (row.CourseKind == AcademyCourseKind.Class)
                GraduateClass(colony, row, at);
            else
                GraduateCommand(colony, row, at);
            Clear(db, i, row);
            any = true;
        }

        if (any)
            Publish(colony, personnel: true);
    }

    public static bool IsEnrolled(Game game, int officerId)
    {
        foreach (var system in game.Systems)
        {
            foreach (var colony in system.GetAllEntitiesWithDataBlob<NavalAcademyDB>())
            {
                var academies = colony.GetDataBlob<NavalAcademyDB>().Academies;
                for (int i = 0; i < academies.Count; i++)
                {
                    var row = academies[i];
                    if (row.CourseKind == AcademyCourseKind.Command && row.OfficerId == officerId)
                        return true;
                }
            }
        }
        return false;
    }

    public static bool IsSeated(Entity officer)
    {
        if (officer.TryGetDataBlob<CommanderDB>(out var commander) && commander.AssignedTo >= 0)
            return true;
        var game = officer.Manager?.Game;
        if (game == null)
            return false;
        foreach (var system in game.Systems)
        {
            foreach (var ship in system.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                if (ship.GetDataBlob<ShipInfoDB>().CommanderID == officer.Id)
                    return true;
            }
        }
        return false;
    }

    static bool TryIdle(Entity colony, int componentId, out NavalAcademyDB db, out int index, out string? reason)
    {
        db = null!;
        index = -1;
        if (!colony.HasDataBlob<ColonyInfoDB>())
        {
            reason = "An academy sits on a colony.";
            return false;
        }
        if (!colony.TryGetDataBlob<NavalAcademyDB>(out db) || !TryIndex(db, componentId, out index))
        {
            reason = "No such academy.";
            return false;
        }
        if (db.Academies[index].CourseKind != AcademyCourseKind.Idle)
        {
            reason = "This academy is already running a course.";
            return false;
        }
        reason = null;
        return true;
    }

    static bool TryIndex(NavalAcademyDB db, int componentId, out int index)
    {
        for (int i = 0; i < db.Academies.Count; i++)
        {
            if (db.Academies[i].ComponentId == componentId)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    static void Clear(NavalAcademyDB db, int index, NavalAcademy row)
    {
        row.CourseKind = AcademyCourseKind.Idle;
        row.OfficerId = -1;
        row.PopulationTaken = 0;
        row.SpeciesId = -1;
        db.Academies[index] = row;
    }

    static int TakePeople(ColonyInfoDB info, int count)
    {
        int species = -1;
        int left = count;
        while (left > 0)
        {
            int bestKey = -1;
            long best = 0;
            foreach (var pair in info.Population)
            {
                if (pair.Value > best)
                {
                    best = pair.Value;
                    bestKey = pair.Key;
                }
            }
            if (bestKey < 0 || best <= 0)
                break;
            long take = Math.Min(best, left);
            long remain = best - take;
            if (remain <= 0)
                info.Population.Remove(bestKey);
            else
                info.Population[bestKey] = remain;
            if (species < 0)
                species = bestKey;
            left -= (int)take;
        }
        return species;
    }

    static void Schedule(Entity colony, DateTime when)
    {
        if (colony.Manager == null)
            return;
        if (when < colony.StarSysDateTime)
            when = colony.StarSysDateTime;
        colony.Manager.ManagerSubpulses.AddEntityInterupt(when, nameof(NavalAcademyProcessor), colony);
    }

    static void GraduateClass(Entity colony, NavalAcademy row, DateTime at)
    {
        if (colony.Manager?.Game == null)
            return;
        var game = colony.Manager.Game;
        var generator = new GaussianRandom();
        int instruction = InstructionOf(colony);
        double mean = 100 + (row.TrainingPeriodInMonths - 24) + instruction;
        for (int i = 0; i < row.ClassSize; i++)
        {
            var commander = CommanderFactory.CreateAcademyGraduate(game);
            commander.CommissionedOn = at;
            commander.RankedOn = at;
            commander.ExperienceCap = generator.NextBellCurve(game.RNG, 0, 300, mean, 33.333);
            if (commander.ExperienceCap > 30)
            {
                double mu = 1 + (row.TrainingPeriodInMonths / 48.0) * 27;
                double sigma = mu / 6.0;
                commander.Experience = generator.NextBellCurve(
                    game.RNG, 0, Math.Min(commander.ExperienceCap, 30), mu, sigma);
            }
            else
            {
                commander.Experience = 0;
            }
            CommanderFactory.Create(colony.Manager, colony.FactionOwnerID, commander);
        }
    }

    static void GraduateCommand(Entity colony, NavalAcademy row, DateTime at)
    {
        if (colony.Manager == null || !colony.Manager.TryGetGlobalEntityById(row.OfficerId, out var officer))
            return;
        if (!officer.TryGetDataBlob<CommanderDB>(out var commander))
            return;
        int mean = SchoolMean(row.TrainingPeriodInMonths, InstructionOf(colony));
        if (CommandRefusal(commander, mean) != null)
            return;
        if (commander.ExperienceCap < mean)
        {
            commander.ExperienceCap = mean;
            return;
        }

        int rank = commander.Rank < 4 ? 4 : 6;
        commander.Rank = rank;
        commander.RankedOn = at;
        string name = string.IsNullOrWhiteSpace(commander.Name) ? "An officer" : commander.Name;
        EventManager.Instance.Publish(Event.Create(
            EventType.OfficerPromoted,
            at,
            name + " promoted to rank " + rank,
            colony.FactionOwnerID,
            colony.Manager.ManagerID,
            officer.Id));
    }

    static int TechLevel(Entity colony, string techId)
    {
        var game = colony.Manager?.Game;
        if (game == null || !game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
            return 0;
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return 0;
        return info.Data.Techs.ContainsKey(techId) ? info.Data.Techs[techId].Level : 0;
    }

    static void Publish(Entity colony, bool personnel)
    {
        if (colony.Manager == null)
            return;
        MessagePublisher.Instance.Publish(Message.Create(
            MessageTypes.EntityChanged,
            colony.Id,
            colony.Manager.ManagerID,
            colony.FactionOwnerID));
        if (!personnel)
            return;
        MessagePublisher.Instance.Publish(Message.Create(
            MessageTypes.PersonnelChanged,
            colony.Id,
            colony.Manager.ManagerID,
            colony.FactionOwnerID));
    }
}
