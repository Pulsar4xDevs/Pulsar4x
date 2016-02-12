using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class SurveyProcessor
    {


        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            if (game.EnableMultiThreading)
            {
                // Process Surveying planets from spaceships
                //Parallel.ForEach(systems, SurveyPlanetsFromShip);

                // Process surveying planets from ground team
                //Parallel.ForEach(systems, SurveyPlanetsFromTeams);

                // Process surveying JPSurveyPoints
                Parallel.ForEach(systems, SurveyJPSurveyPoints);
            }


            
        }

        private static void SurveyPlanetsFromShip(StarSystem system)
        {
            throw new NotImplementedException();
        }

        private static void SurveyPlanetsFromTeams(StarSystem system)
        {
            throw new NotImplementedException();
        }

        private static void SurveyJPSurveyPoints(StarSystem system)
        {
            
        }


    }
}
