using Newtonsoft.Json;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;

namespace Pulsar4X.People
{
    public class NavalAcademyAtb : IComponentDesignAttribute
    {
        public int ClassSize { get; internal set; }
        public int TrainingPeriodInMonths { get; internal set; }

        /// <summary>Crew plan phase 2 defaults: an academy trains 200 crew at a time on a 3-month course.</summary>
        public const int DefaultCrewSeats = 200;
        public const int DefaultCrewCourseMonths = 3;

        /// <summary>Crew-course seats, separate from the officer class.</summary>
        public int CrewSeats { get; internal set; } = DefaultCrewSeats;

        /// <summary>Months a crew course runs.</summary>
        public int CrewCourseMonths { get; internal set; } = DefaultCrewCourseMonths;

        [JsonConstructor]
        private NavalAcademyAtb() { }

        public NavalAcademyAtb(double classSize, double period)
        {
            ClassSize = (int)classSize;
            TrainingPeriodInMonths = (int)period;
        }

        public NavalAcademyAtb(double classSize, double period, double crewSeats, double crewMonths)
        {
            ClassSize = (int)classSize;
            TrainingPeriodInMonths = (int)period;
            CrewSeats = (int)crewSeats;
            CrewCourseMonths = (int)crewMonths;
        }

        public NavalAcademyAtb(int classSize, int period)
        {
            ClassSize = classSize;
            TrainingPeriodInMonths = period;
        }

        public NavalAcademyAtb(NavalAcademyAtb db)
        {
            ClassSize = db.ClassSize;
            TrainingPeriodInMonths = db.TrainingPeriodInMonths;
            CrewSeats = db.CrewSeats;
            CrewCourseMonths = db.CrewCourseMonths;
        }

        public string AtbDescription()
        {
            return "Class Size: " + ClassSize.ToString() + "\nTraining Length: " + TrainingPeriodInMonths.ToString() + " months"
                + "\nCrew Seats: " + CrewSeats.ToString() + "\nCrew Course: " + CrewCourseMonths.ToString() + " months";
        }

        public string AtbName()
        {
            return "Naval Academy";
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var row = new NavalAcademy
            {
                ComponentId = componentInstance.ID,
                ClassSize = ClassSize,
                TrainingPeriodInMonths = TrainingPeriodInMonths,
                CrewSeats = CrewSeats,
                CrewCourseMonths = CrewCourseMonths,
                NextCohortId = 1,
                Cohorts = new System.Collections.Generic.List<AcademyCohort>(),
                CourseKind = AcademyCourseKind.Idle,
                OfficerId = -1,
                SpeciesId = -1,
            };

            if (!parentEntity.TryGetDataBlob<NavalAcademyDB>(out var academyDB))
            {
                academyDB = new NavalAcademyDB();
                parentEntity.SetDataBlob(academyDB);
            }
            academyDB.Academies.Add(row);
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.TryGetDataBlob<NavalAcademyDB>(out var academyDB))
                return;

            for (int i = 0; i < academyDB.Academies.Count; i++)
            {
                if (academyDB.Academies[i].ComponentId != componentInstance.ID)
                    continue;
                var row = academyDB.Academies[i];
                NavalAcademyCourse.FoldLegacy(ref row);
                if (row.Cohorts != null)
                {
                    foreach (var cohort in row.Cohorts)
                    {
                        if (cohort.Kind == AcademyCourseKind.Class || cohort.Kind == AcademyCourseKind.Crew)
                            NavalAcademyCourse.ReturnPeople(parentEntity, cohort.PopulationTaken, cohort.SpeciesId);
                    }
                }
                academyDB.Academies.RemoveAt(i);
                break;
            }

            if (academyDB.Academies.Count == 0)
                parentEntity.RemoveDataBlob<NavalAcademyDB>();
        }
    }
}
