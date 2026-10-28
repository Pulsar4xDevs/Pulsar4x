using System;
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

    public struct NavalAcademy
    {
        public int ComponentId;
        public int ClassSize;
        public int TrainingPeriodInMonths;
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
            Academies = new SafeList<NavalAcademy>(db.Academies);
        }

        public override object Clone()
        {
            return new NavalAcademyDB(this);
        }
    }
}
