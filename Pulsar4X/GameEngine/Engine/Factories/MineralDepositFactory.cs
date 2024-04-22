using System;
using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Orbital;

namespace Pulsar4X.Engine.Factories;

public class MineralDepositFactory
{
    public static MineralsDB? GenerateRandom(SystemGenSettingsBlueprint settings, List<Mineral> minerals, StarSystem system, SystemBodyInfoDB bodyInfoDB, MassVolumeDB massVolumeDB, bool forceGeneration = false)
    {
        // get the mass ratio for this body to planet:
        double massRatio = massVolumeDB.MassDry / UniversalConstants.Units.EarthMassInKG;
        double genChance = massRatio * system.RNGNextDouble();
        double genChanceThreshold = settings.MineralGenerationChanceByBodyType[bodyInfoDB.BodyType];

        // now lets see if this body has minerals
        if (genChance < genChanceThreshold
            && BodyType.Comet != bodyInfoDB.BodyType  // comets always have minerals.
            && !forceGeneration)
        {
            return null;
        }

        var mineralInfo = new MineralsDB();

        // this body has at least some minerals, lets generate them:
        foreach (var min in minerals)
        {
            // create a MineralDepositInfo
            MineralDeposit mdi = new MineralDeposit();

            // get a genChance:
            double abundance = min.Abundance[bodyInfoDB.BodyType];
            genChance = massRatio * system.RNGNextDouble() * abundance;

            if (genChance >= genChanceThreshold)
            {
                mdi.Accessibility = GeneralMath.Clamp(settings.MinMineralAccessibility + genChance, 0, 1);
                mdi.Amount = (long)Math.Round(settings.MaxMineralAmmountByBodyType[bodyInfoDB.BodyType] * genChance);
                mdi.HalfOriginalAmount = mdi.Amount / 2;

                if (!mineralInfo.Minerals.ContainsKey(min.ID))
                {
                    mineralInfo.Minerals.Add(min.ID, mdi);
                }
            }
        }

        return mineralInfo.Minerals.Count > 0 ? mineralInfo : null;
    }

    public static MineralsDB Generate(Game game, List<(int, double, double)> mineralsToGenerate, BodyType bodyType)
    {
        var mineralsDb = new MineralsDB();

        foreach((int id, double abundance, double accessibility) in mineralsToGenerate)
        {
            var mdi = new MineralDeposit()
            {
                Accessibility = GeneralMath.Clamp(accessibility, 0, 1),
                Amount = (long)Math.Round(game.GalaxyGen.Settings.MaxMineralAmmountByBodyType[bodyType] * abundance),
            };
            mdi.HalfOriginalAmount = mdi.Amount / 2;
            mineralsDb.Minerals.Add(id, mdi);
        }

        return mineralsDb;
    }
}