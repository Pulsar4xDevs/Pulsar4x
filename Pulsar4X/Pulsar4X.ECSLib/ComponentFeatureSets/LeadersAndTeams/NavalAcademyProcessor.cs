using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class NavalAcademyProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if(!entity.TryGetDatablob<NavalAcademyDB>(out var academyDB)) return;

            var academy = academyDB.Academies.Where(a => a.GraduationDate.Date == atDateTime.Date).First();
            var generator = new GaussianRandom();

            // Graduate the class
            for(int i = 0; i < academy.ClassSize; i++)
            {
                var commanderDB = CommanderFactory.CreateAcademyGraduate();

                // Set the officers commission date and rank date
                commanderDB.CommissionedOn = entity.StarSysDateTime.Date;
                commanderDB.RankedOn = entity.StarSysDateTime.Date;

                // Generate an experience cap for the graduate on a bell curve from 0-200
                commanderDB.ExperienceCap = generator.NextBellCurve(StaticRefLib.Game.RNG, 0, 200, 100, 33.333);

                // Only give starting experience to graduates with some potential
                if(commanderDB.ExperienceCap > 30)
                {
                    // The starting experience for graduates ranges from 0-30
                    // Setup the mean and standard deviation such that low training times give low starting experience
                    // and high training times bell curve around the high end of the range
                    double mu = 1 + (academy.TrainingPeriodInMonths / 48.0) * 27;
                    double sigma = mu / 6.0;
                    commanderDB.Experience = generator.NextBellCurve(StaticRefLib.Game.RNG, 0, Math.Min(commanderDB.ExperienceCap, 30), mu, sigma);
                }
                else
                {
                    commanderDB.Experience = 0;
                }

                var blobs = new List<BaseDataBlob>();
                var nameDB = new NameDB(commanderDB.ToString(), entity.FactionOwnerID, commanderDB.ToString());
                blobs.Add(commanderDB);
                blobs.Add(nameDB);
                Entity.Create(entity.Manager, entity.FactionOwnerID, blobs);
            }

            // Remove the graduated class
            academyDB.Academies.Remove(academy);

            // Setup the next graduating class
            var graduationDate = academy.GraduationDate + TimeSpan.FromDays(academy.TrainingPeriodInMonths * 30);
            academyDB.Academies.Add(new NavalAcademy() {
                ClassSize = academy.ClassSize,
                GraduationDate = graduationDate,
                TrainingPeriodInMonths = academy.TrainingPeriodInMonths
            });

            // Add interrupt for next graduation
            entity.Manager.ManagerSubpulses.AddEntityInterupt(graduationDate, nameof(NavalAcademyProcessor), entity);
        }
    }
}