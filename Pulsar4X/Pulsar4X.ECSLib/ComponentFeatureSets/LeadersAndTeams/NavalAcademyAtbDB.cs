using System;

namespace Pulsar4X.ECSLib
{
    public class NavalAcademyAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        public int ClassSize { get; internal set; }
        public int TrainingPeriodInMonths { get; internal set; }

        public NavalAcademyAtbDB(double classSize, double period)
        {
            ClassSize = (int)classSize;
            TrainingPeriodInMonths = (int)period;
        }

        public NavalAcademyAtbDB(int classSize, int period)
        {
            ClassSize = classSize;
            TrainingPeriodInMonths = period;
        }

        public NavalAcademyAtbDB(NavalAcademyAtbDB db)
        {
            ClassSize = db.ClassSize;
            TrainingPeriodInMonths = db.TrainingPeriodInMonths;
        }

        public override object Clone()
        {
            return new NavalAcademyAtbDB(this);
        }

        public string AtbDescription()
        {
            return "Class Size: " + ClassSize.ToString() + "\nTraining Length: " + TrainingPeriodInMonths.ToString() + " months";
        }

        public string AtbName()
        {
            return "Naval Academy";
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            DateTime graduationDate = parentEntity.StarSysDateTime + TimeSpan.FromDays(TrainingPeriodInMonths * 30);
            if (parentEntity.TryGetDatablob<NavalAcademyDB>(out var academyDB))
            {
                academyDB.Academies.Add(new NavalAcademy() {
                    ClassSize = this.ClassSize,
                    GraduationDate = graduationDate,
                    TrainingPeriodInMonths = this.TrainingPeriodInMonths
                });
            }
            else
            {
                parentEntity.SetDataBlob(new NavalAcademyDB(ClassSize, graduationDate, TrainingPeriodInMonths));
            }
            parentEntity.Manager.ManagerSubpulses.AddEntityInterupt(graduationDate, nameof(NavalAcademyProcessor), parentEntity);
        }
    }
}