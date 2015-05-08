using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class StarSystemFactory
    {
        public static StarSystem CreateSystem(string name, int seed = -1)
        {
            // create new RNG with Seed.
            if (seed == -1)
            {
                seed = GalaxyFactory.SeedRNG.Next();
            }

            StarSystem newSystem = new StarSystem(name, seed);

            int numStars = newSystem.RNG.Next(1, 5);
            List<Entity> stars = StarFactory.CreateStarsForSystem(newSystem, numStars);

            foreach (Entity star in stars)
            {
                SystemBodyFactory.GenerateSystemBodiesForStar(newSystem, star);
            }

            // < @todo generate JumpPoints
            //JumpPointFactory.GenerateJumpPoints(newSystem, numJumpPoints);

            Game.Instance.StarSystems.Add(newSystem);
            return newSystem;
        }
    }
}
