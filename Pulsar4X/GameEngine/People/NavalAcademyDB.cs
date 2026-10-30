using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.People
{
    public enum AcademyCourseKind
    {
        Idle = 0,
        Class = 1,
        Command = 2,
    }

    /// <summary>One run using seats on a mounted academy. A class holds Count recruits. A command course holds one officer.</summary>
    public struct AcademyCohort
    {
        public int CohortId;
        public AcademyCourseKind Kind;
        public int Count;
        public DateTime GraduationDate;
        public int OfficerId;
        public int PopulationTaken;
        public int SpeciesId;
    }

    public struct NavalAcademy
    {
        public int ComponentId;
        public int ClassSize;
        public int TrainingPeriodInMonths;
        public int NextCohortId;
        public List<AcademyCohort> Cohorts;

        // The single course these fields used to store. A save that still has them and no cohorts
        // is folded into one cohort on load, then the fields are cleared.
        public AcademyCourseKind CourseKind;
        public DateTime GraduationDate;
        public int OfficerId;
        public int PopulationTaken;
        public int SpeciesId;
    }

    public class NavalAcademyDB : BaseDataBlob
    {
        public SafeList<NavalAcademy> Academies = new SafeList<NavalAcademy>();

        public NavalAcademyDB() { }

        public NavalAcademyDB(NavalAcademyDB db)
        {
            Academies = new SafeList<NavalAcademy>();
            for (int i = 0; i < db.Academies.Count; i++)
                Academies.Add(Copy(db.Academies[i]));
        }

        public override object Clone()
        {
            return new NavalAcademyDB(this);
        }

        [OnDeserialized]
        private void FoldSavedCourses(StreamingContext context)
        {
            for (int i = 0; i < Academies.Count; i++)
            {
                var row = Academies[i];
                NavalAcademyCourse.FoldLegacy(ref row);
                Academies[i] = row;
            }
        }

        static NavalAcademy Copy(NavalAcademy row)
        {
            NavalAcademyCourse.FoldLegacy(ref row);
            var copy = row;
            copy.Cohorts = new List<AcademyCohort>(row.Cohorts);
            return copy;
        }
    }
}
