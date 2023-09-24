using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Atb
{
    public class NavalAcademyAtb : IComponentDesignAttribute
    {
        public int ClassSize { get; internal set; }
        public int TrainingPeriodInMonths { get; internal set; }

        public NavalAcademyAtb(double classSize, double period)
        {
            ClassSize = (int)classSize;
            TrainingPeriodInMonths = (int)period;
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