using System;

namespace Pulsar4X.ECSLib
{
    public struct NavalAcademy
    {
        public int ClassSize;
        public DateTime GraduationDate;
        public int TrainingPeriodInMonths;
    }

    public class NavalAcademyDB : BaseDataBlob
    {
        public SafeList<NavalAcademy> Academies = new SafeList<NavalAcademy>();

        public NavalAcademyDB() { }
        public NavalAcademyDB(int classSize, DateTime graduationDate, int trainingPeriod)
        {
            Academies.Add(new NavalAcademy(){
                ClassSize = classSize,
                GraduationDate = graduationDate,
                TrainingPeriodInMonths = trainingPeriod
            });
        }

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