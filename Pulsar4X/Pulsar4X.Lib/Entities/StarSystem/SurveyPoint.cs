using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

#if LOG4NET_ENABLED
using log4net;
#endif

namespace Pulsar4X.Entities
{
    public class SurveyPoint : OrbitingEntity
    {
        /// <summary>
        /// List of factions that have surveyed this point.
        /// </summary>
        private BindingList<Faction> _GravSurveyList;
        public BindingList<Faction> _gravSurveyList
        {
            get { return _GravSurveyList; }
        }

        /// <summary>
        /// List of jump points in this survey points survey area, and will be revealed when this survey point is surveyed. by a surveyor.
        /// </summary>
        private BindingList<JumpPoint> _JPList;
        public BindingList<JumpPoint> _jPList
        {
            get { return _JPList; }
        }

        /// <summary>
        /// Constructor for survey points. Survey points will occur in three rings around a system primary for primary only JPs. As with survey cost, the mass of the solar system will be
        /// square rooted, and then multiplied to a constant value to determine the distance of the point from the primary. the angle offset will be determined from the logic for 
        /// making circles. Ring one is 0,60,120,180,240,300. Ring two is 15,45,75,105,135,165,195,225,255,285,315,345. Ring three is 0,30,60,90,120,150,180,210,240,270,300,330.
        /// secondary JP surveying is yet to be worked out.
        /// </summary>
        /// <param name="SystemOfPoint">System this point is in</param>
        /// <param name="xPosition">X Position in AU of this survey point from the system primary(0.0,0.0)</param>
        /// <param name="yPosition">Y Position in AU of this survey point from the system primary</param>
        public SurveyPoint(StarSystem SystemOfPoint, double xPosition, double yPosition)
        {
            Position.System = SystemOfPoint;
            Position.X = xPosition;
            Position.Y = yPosition;

            SSEntity = StarSystemEntityType.SurveyPoint;

            /// <sumamry>
            /// Unused Orbiting Entity data here.
            /// </summary>
            Parent = SystemOfPoint.Stars[0];
            SupportsPopulations = false;
            Radius = 0.0;

            _GravSurveyList = new BindingList<Faction>();
            _JPList = new BindingList<JumpPoint>();

            Name = "Survey Location #" + (SystemOfPoint._SurveyPoints.Count() + 1).ToString();
        }

        /// <summary>
        /// What legal orders will be associated with this star system entity? in the code that determines available orders the order in question, in this case Grav Survey, must be present here
        /// and in the taskgroup's orders list in order for it to be a valid order. Here it is present by default, assuming the point is unsurveyed, the taskgroup will only have the order if it has a
        /// grav survey sensor.
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        public override List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.AddRange(_legalOrders);
            if (this._GravSurveyList.Contains(faction) == false)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.GravSurvey);
            }
            return legalOrders;
        }
    }
}
