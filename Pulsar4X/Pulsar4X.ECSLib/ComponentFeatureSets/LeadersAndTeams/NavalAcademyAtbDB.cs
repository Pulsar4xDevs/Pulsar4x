using System;

namespace Pulsar4X.ECSLib
{
    public class NavalAcademyAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        public int ClassSize { get; internal set; }

        public NavalAcademyAtbDB(double classSize)
        {
            ClassSize = (int)classSize;
        }

        public NavalAcademyAtbDB(int classSize)
        {
            ClassSize = classSize;
        }

        public NavalAcademyAtbDB(NavalAcademyAtbDB db)
        {
            ClassSize = db.ClassSize;
        }

        public override object Clone()
        {
            return new NavalAcademyAtbDB(this);
        }

        public string AtbDescription()
        {
            return "Class Size: " + ClassSize.ToString();
        }

        public string AtbName()
        {
            return "Naval Academy";
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            DateTime graduationDate = parentEntity.StarSysDateTime + TimeSpan.FromDays(NavalAcademyProcessor.DaysUntilGraduation);
            if (parentEntity.TryGetDatablob<NavalAcademyDB>(out var academyDB))
            {
                academyDB.Academies.Add(new NavalAcademy() {
                    ClassSize = this.ClassSize,
                    GraduationDate = graduationDate
                });
            }
            else
            {
                parentEntity.SetDataBlob(new NavalAcademyDB(ClassSize, graduationDate));
            }
            parentEntity.Manager.ManagerSubpulses.AddEntityInterupt(graduationDate, nameof(NavalAcademyProcessor), parentEntity);
        }
    }
}