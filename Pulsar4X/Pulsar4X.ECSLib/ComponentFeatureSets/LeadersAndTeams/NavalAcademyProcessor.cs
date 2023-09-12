using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class NavalAcademyProcessor : IInstanceProcessor
    {
        public static readonly int DaysUntilGraduation = 365;
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if(!entity.TryGetDatablob<NavalAcademyDB>(out var academyDB)) return;

            var academy = academyDB.Academies.Where(a => a.GraduationDate.Date == atDateTime.Date).First();

            // Graduate the class
            for(int i = 0; i < academy.ClassSize; i++)
            {
                // TODO: create a new naval officer
                var blobs = new List<BaseDataBlob>();
                blobs.Add(CommanderFactory.CreateAcademyGraduate());
                Entity.Create(entity.Manager, entity.FactionOwnerID, blobs);
            }

            // Remove the graduated class
            academyDB.Academies.Remove(academy);

            // Setup the next graduating class
            var graduationDate = academy.GraduationDate + TimeSpan.FromDays(DaysUntilGraduation);
            academyDB.Academies.Add(new NavalAcademy() {
                ClassSize = academy.ClassSize,
                GraduationDate = graduationDate
            });

            // Add interrupt for next graduation
            entity.Manager.ManagerSubpulses.AddEntityInterupt(graduationDate, nameof(NavalAcademyProcessor), entity);
        }
    }
}