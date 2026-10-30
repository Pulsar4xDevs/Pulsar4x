using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Messaging;
using Pulsar4X.Ships;

namespace Pulsar4X.People;

/// <summary>
/// Class size is how many students can be enrolled at once. Each order starts a cohort with its own
/// graduation date. The processor graduates the cohorts whose date has arrived and leaves the rest.
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

    public static int SeatsUsed(NavalAcademy row)
    {
        if (row.Cohorts == null)
            return 0;
        int used = 0;
        foreach (var cohort in row.Cohorts)
            used += cohort.Kind == AcademyCourseKind.Command ? 1 : cohort.Count;
        return used;
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

    /// <summary>
    /// A save from the single-course academy stores the run on the building. Fold that into one cohort.
    /// </summary>
    public static void FoldLegacy(ref NavalAcademy row)
    {
        row.Cohorts ??= new List<AcademyCohort>();
        if (row.Cohorts.Count == 0 && row.CourseKind != AcademyCourseKind.Idle)
        {
            bool command = row.CourseKind == AcademyCourseKind.Command;
            int count = command
                ? 1
                : (row.PopulationTaken > 0 ? row.PopulationTaken : Math.Max(row.ClassSize, 1));
            row.Cohorts.Add(new AcademyCohort
            {
                CohortId = 1,
                Kind = row.CourseKind,
                Count = count,
                GraduationDate = row.GraduationDate,
                OfficerId = row.OfficerId,
                PopulationTaken = row.PopulationTaken,
                SpeciesId = row.SpeciesId,
            });
            if (row.NextCohortId <= 1)
                row.NextCohortId = 2;
        }
        row.CourseKind = AcademyCourseKind.Idle;
        row.OfficerId = -1;
        row.PopulationTaken = 0;
        row.SpeciesId = -1;
        if (row.NextCohortId <= 0)
            row.NextCohortId = 1;
    }

    public static string? TryTrain(Entity colony, int componentId, int count)
    {
        if (!TryAcademy(colony, componentId, out var db, out int index, out var reason))
            return reason;
        if (count < 1)
            return "A class needs at least one recruit.";
        if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return "The colony has no population.";

        var row = db.Academies[index];
        int free = row.ClassSize - SeatsUsed(row);
        if (count > free)
            return "This class needs " + count + " seats. " + free + " are free.";

        long people = PopulationOf(info);
        long need = (long)count * PeoplePerEnsign(colony);
        if (people < need)
            return "The colony has " + people + " people. This class needs " + need + ".";

        var cohort = new AcademyCohort
        {
            CohortId = TakeId(ref row),
            Kind = AcademyCourseKind.Class,
            Count = count,
            PopulationTaken = count,
            SpeciesId = TakePeople(info, count),
            OfficerId = -1,
            GraduationDate = colony.StarSysDateTime.AddDays(row.TrainingPeriodInMonths * 30),
        };
        row.Cohorts.Add(cohort);
        db.Academies[index] = row;
        Schedule(colony, cohort.GraduationDate);
        Publish(colony, personnel: false);
        return null;
    }

    public static string? TryCommand(Entity colony, int componentId, Entity officer)
    {
        if (!TryAcademy(colony, componentId, out var db, out int index, out var reason))
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
        if (row.ClassSize - SeatsUsed(row) < 1)
            return "This academy has no free seats.";

        var cohort = new AcademyCohort
        {
            CohortId = TakeId(ref row),
            Kind = AcademyCourseKind.Command,
            Count = 1,
            OfficerId = officer.Id,
            PopulationTaken = 0,
            SpeciesId = -1,
            GraduationDate = colony.StarSysDateTime.AddDays(row.TrainingPeriodInMonths * 30),
        };
        row.Cohorts.Add(cohort);
        db.Academies[index] = row;
        Schedule(colony, cohort.GraduationDate);
        Publish(colony, personnel: false);
        return null;
    }

    public static string? TryCancel(Entity colony, int componentId, int cohortId)
    {
        if (!colony.TryGetDataBlob<NavalAcademyDB>(out var db) || !TryIndex(db, componentId, out int index))
            return "No such academy.";
        var row = db.Academies[index];
        FoldLegacy(ref row);
        int found = -1;
        for (int i = 0; i < row.Cohorts.Count; i++)
        {
            if (row.Cohorts[i].CohortId == cohortId)
            {
                found = i;
                break;
            }
        }
        if (found < 0)
        {
            db.Academies[index] = row;
            return "No such course.";
        }

        var cohort = row.Cohorts[found];
        if (cohort.Kind == AcademyCourseKind.Class)
            ReturnPeople(colony, cohort.PopulationTaken, cohort.SpeciesId);
        row.Cohorts.RemoveAt(found);
        db.Academies[index] = row;
        Publish(colony, personnel: false);
        return null;
    }

    public static void ReturnPeople(Entity colony, int taken, int speciesId)
    {
        if (taken <= 0 || speciesId < 0)
            return;
        if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return;
        info.Population.TryGetValue(speciesId, out long have);
        info.Population[speciesId] = have + taken;
    }

    public static void Graduate(Entity colony, DateTime at)
    {
        if (!colony.TryGetDataBlob<NavalAcademyDB>(out var db))
            return;

        bool any = false;
        for (int a = 0; a < db.Academies.Count; a++)
        {
            var row = db.Academies[a];
            FoldLegacy(ref row);
            for (int i = row.Cohorts.Count - 1; i >= 0; i--)
            {
                var cohort = row.Cohorts[i];
                if (cohort.GraduationDate.Date != at.Date)
                    continue;
                if (cohort.Kind == AcademyCourseKind.Class)
                    GraduateClass(colony, row, cohort, at);
                else
                    GraduateCommand(colony, row, cohort, at);
                row.Cohorts.RemoveAt(i);
                any = true;
            }
            db.Academies[a] = row;
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
                var db = colony.GetDataBlob<NavalAcademyDB>();
                for (int i = 0; i < db.Academies.Count; i++)
                {
                    var row = db.Academies[i];
                    FoldLegacy(ref row);
                    db.Academies[i] = row;
                    for (int c = 0; c < row.Cohorts.Count; c++)
                    {
                        var cohort = row.Cohorts[c];
                        if (cohort.Kind == AcademyCourseKind.Command && cohort.OfficerId == officerId)
                            return true;
                    }
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

    static bool TryAcademy(Entity colony, int componentId, out NavalAcademyDB db, out int index, out string? reason)
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
        var row = db.Academies[index];
        FoldLegacy(ref row);
        db.Academies[index] = row;
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

    static int TakeId(ref NavalAcademy row)
    {
        if (row.NextCohortId <= 0)
            row.NextCohortId = 1;
        int id = row.NextCohortId;
        row.NextCohortId++;
        return id;
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

    static void GraduateClass(Entity colony, NavalAcademy row, AcademyCohort cohort, DateTime at)
    {
        if (colony.Manager?.Game == null)
            return;
        var game = colony.Manager.Game;
        var generator = new GaussianRandom();
        int instruction = InstructionOf(colony);
        double mean = 100 + (row.TrainingPeriodInMonths - 24) + instruction;
        for (int i = 0; i < cohort.Count; i++)
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

    static void GraduateCommand(Entity colony, NavalAcademy row, AcademyCohort cohort, DateTime at)
    {
        if (colony.Manager == null || !colony.Manager.TryGetGlobalEntityById(cohort.OfficerId, out var officer))
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
