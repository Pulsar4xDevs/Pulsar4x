using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using Pulsar4X.Helpers;


//using log4net.Config;
//using log4net;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Class for storing Jump point detections for each faction.
    /// </summary>
    public class JPDetection
    {
        public enum Status
        {
            None,
            Incomplete,
            Complete,
            Count
        }

        /// <summary>
        /// Status of this faction's survey of this starsystem.
        /// </summary>
        public Status _SurveyStatus { get; set; }

        /// <summary>
        /// Which Points have been surveyed?
        /// </summary>
        public BindingList<SurveyPoint> _SurveyedPoints { get; set; }

        /// <summary>
        /// List of detected JPs if status is Incomplete.
        /// </summary>
        public BindingList<JumpPoint> _DetectedJPs { get; set; }

        /// <summary>
        /// These points are marked as claimed for survey purpose so that more than one survey ship doesn't attempt to survey the same point.
        /// </summary>
        public BindingList<SurveyPoint> _GravSurveyInProgress { get; set; }

        /// <summary>
        /// These system bodies are "claimed" by the geo survey craft surveying them, this should prevent multiple craft from surveying the same body.
        /// </summary>
        public BindingList<SystemBody> _GeoSurveyInProgress { get; set; }

        public JPDetection()
        {
            _SurveyStatus = Status.None;
            _SurveyedPoints = new BindingList<SurveyPoint>();
            _DetectedJPs = new BindingList<JumpPoint>();
            _GravSurveyInProgress = new BindingList<SurveyPoint>();
            _GeoSurveyInProgress = new BindingList<SystemBody>();
        }

    }

    public class StarSystem : GameEntity
    {
        public BindingList<Star> Stars { get; set; }

        /// <summary>
        /// Each starsystem has its own list of waypoints. These probably need to be faction specific?
        /// </summary>
        public BindingList<Waypoint> Waypoints { get; set; }

        /// <summary>
        /// Each system has links to other systems.
        /// </summary>
        public BindingList<JumpPoint> JumpPoints { get; set; }

        /// <summary>
        /// A list of TaskGroups currently inside this system.
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroups { get; set; }

        /// <summary>
        /// A list of Populations currently inside this system.
        /// </summary>
        public BindingList<Population> Populations { get; set; }

        /// <summary>
        /// A list of OrdnanceGroups (Missile Groups) currently inside this system.
        /// </summary>
        public BindingList<OrdnanceGroupTN> OrdnanceGroups { get; set; }

        /// <summary>
        /// Global List of all contacts within the system.
        /// </summary>
        public VerboseBindingList<SystemContact> SystemContactList { get; set; }

        /// <summary>
        /// List of faction contact lists. Here is where context starts getting confusing. This is a list of the last time the SystemContactList was pinged.
        /// SystemContactList stores Location and pointers to Pop/TG signatures. These must be arrayed in order from Faction[0] to Faction[Max] Corresponding to
        /// FactionContactLists[0] - [max]
        /// </summary>
        public BindingList<FactionSystemDetection> FactionDetectionLists { get; set; }

        public int SystemIndex;

        /// <summary>
        /// Random generation seed used to generate this system.
        /// </summary>
        private int m_seed;
        public int Seed { get { return m_seed; } }


        /// <summary>
        /// Each starsystem will have 30 suvery points under TN rules.
        /// Also, I'll need to track which faction has surveyed which point. I could do this at the survey point level, but I think it better to have a starsystem list of factions
        /// that have completely surveyed this starsystem, along with a way of handling incomplete surveys.
        /// </summary>
        public BindingList<SurveyPoint> _SurveyPoints { get; set; }

        /// <summary>
        /// store the results of each faction's surveying here. if no faction is present then it obviously has not done any surveying. otherwise complete means every point is mapped,
        /// and incomplete means only those points in JPDetection._DetectedJP are detected.  
        /// </summary>
        public Dictionary<Faction, JPDetection> _SurveyResults { get; set; }

        public StarSystem(string name, int seed)
            : base()
        {
            Name = name;
            Stars = new BindingList<Star>();

            Waypoints = new BindingList<Waypoint>();
            JumpPoints = new BindingList<JumpPoint>();
            SystemContactList = new VerboseBindingList<SystemContact>();
            FactionDetectionLists = new BindingList<FactionSystemDetection>();

            TaskGroups = new BindingList<TaskGroupTN>();
            Populations = new BindingList<Population>();
            OrdnanceGroups = new BindingList<OrdnanceGroupTN>();

            _SurveyPoints = new BindingList<SurveyPoint>();

            _SurveyResults = new Dictionary<Faction, JPDetection>();

            m_seed = seed;

            // Subscribe to change events.
            SystemContactList.ListChanged += SystemContactList_ListChanged;

            // Create the faciton contact information for each faction.
            foreach (Faction f in GameState.Instance.Factions)
            {
                f.AddNewContactList(this);
            }
        }

        /// <summary>
        /// This function adds a waypoint to the system waypoint list, it is called by SystemMap.cs and connects the UI waypoint to the back end waypoints.
        /// </summary>
        /// <param name="X">System Position X in AU</param>
        /// <param name="Y">System Position Y in AU</param>
        public void AddWaypoint(String Title, double X, double Y, int FactionID)
        {
            Waypoint NewWP = new Waypoint(Title, this, X, Y, FactionID);
            Waypoints.Add(NewWP);
        }

        /// <summary>
        /// This function removes a waypoint from the system waypoint list, it is called in SystemMap.cs and connects the UI to the backend.
        /// </summary>
        /// <param name="Remove"></param>
        public void RemoveWaypoint(Waypoint Remove)
        {
            if (Waypoints.Count == 1)
                Waypoints.Clear();
            else
                Waypoints.Remove(Remove);
        }

        private void SystemContactList_ListChanged(object sender, ListChangedEventArgs e)
        {
            BindingList<SystemContact> list = sender as BindingList<SystemContact>;

            switch (e.ListChangedType)
            {
                    ///< @todo Find a better place to update the FactionDetectionLists
                case ListChangedType.ItemAdded:
                    // Update all the faction contact lists with the new contact.
                    for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
                    {
                        FactionDetectionLists[loop].AddContact();
                    }
                    break;
                case ListChangedType.ItemDeleted:
                    // Remove the contact from each of the faction contact lists as well as the System contact list.
                    for (int loop = 0; loop < FactionDetectionLists.Count; loop++)
                    {
                        FactionDetectionLists[loop].RemoveContact(e.NewIndex);
                    }
                    break;
            }
        }

        /// <summary>
        /// Get the PPV level for this faction in this system.
        /// </summary>
        /// <param name="fact">Faction to find PPV for</param>
        /// <returns>PPV value</returns>
        public int GetProtectionLevel(Faction fact)
        {
            int PPV = 0;
            foreach (TaskGroupTN TaskGroup in TaskGroups)
            {
                if (TaskGroup.TaskGroupFaction == fact)
                {
                    foreach (ShipTN Ship in TaskGroup.Ships)
                    {
                        PPV = PPV + Ship.ShipClass.PlanetaryProtectionValue;
                    }
                }
            }

            return PPV;
        }

        /// <summary>
        /// Updates this StarSystem for the new time.
        /// </summary>
        /// <param name="deltaSeconds">Change in seconds since last update.</param>
        public void Update(int deltaSeconds)
        {
            // Update the position of all planets. This should probably be in something like the construction tick in Aurora.
            foreach (Star CurrentStar in Stars)
            {
                CurrentStar.UpdatePosition(deltaSeconds);

                // Since the star moved, update the JumpPoint position.
                foreach (JumpPoint CurrentJumpPoint in JumpPoints)
                {
                    CurrentJumpPoint.UpdatePosition();
                }
            }
        }

        /// <summary>
        /// The cost for Gravitational survey points as per Aurora is: square root ( System Primary's solar masses ) * 400 
        /// </summary>
        /// <returns>Number of survey points that must be generated to survey each gravitational survey point.</returns>
        public int GetSurveyCost()
        {
            return (int)Math.Floor((float)Math.Sqrt(Stars[0].Orbit.MassRelativeToSol) * 400.0f);
        }


        /// <summary>
        /// This should be done after mass has been assigned to Stars[0]. The pattern here is the one in use in TN Aurora. I am using angle from top, for my numbers, but
        /// that is 90 degrees internally.
        /// </summary>
        public void GenerateSurveyPoints()
        {
            double RingValue = Math.Sqrt(Stars[0].Orbit.MassRelativeToSol) * Constants.SensorTN.EarthRingDistance;

            /// <summary>
            /// Ring one is 0,60,120,180,240,300.
            /// </summary>
            for (int surveyPointIterator = 30; surveyPointIterator < 360; surveyPointIterator += 60)
            {
                double dAngle = surveyPointIterator * ((Math.PI) / 180.0); //this would be 2 * PI / 360
                double fX = Math.Cos(dAngle) * RingValue;
                double fY = Math.Sin(dAngle) * RingValue;

                SurveyPoint SP = new SurveyPoint(this, fX, fY);
                _SurveyPoints.Add(SP);
            }

            /// <summary>
            /// Ring two is 15,45,75,105,135,165,195,225,255,285,315,345.
            /// </summary>
            for (int surveyPointIterator = 15; surveyPointIterator < 360; surveyPointIterator += 30)
            {
                double dAngle = surveyPointIterator * ((Math.PI) / 180.0); //this would be 2 * PI / 360
                double fX = Math.Cos(dAngle) * (RingValue * 2);
                double fY = Math.Sin(dAngle) * (RingValue * 2);

                SurveyPoint SP = new SurveyPoint(this, fX, fY);
                _SurveyPoints.Add(SP);
            }

            /// <summary>
            /// Ring three is 0,30,60,90,120,150,180,210,240,270,300,330.
            /// </summary>
            for (int surveyPointIterator = 0; surveyPointIterator < 360; surveyPointIterator += 30)
            {
                double dAngle = surveyPointIterator * ((Math.PI) / 180.0); //this would be 2 * PI / 360
                double fX = Math.Cos(dAngle) * (RingValue * 3);
                double fY = Math.Sin(dAngle) * (RingValue * 3);

                SurveyPoint SP = new SurveyPoint(this, fX, fY);
                _SurveyPoints.Add(SP);
            }
        }


        /// <summary>
        /// Which survey point will X,Y be within the area of? Between 0 to RingValue is ring one, RingValue to 2 * RingValue is ring 2, and 3 * RingValue is ring 3.
        /// </summary>
        /// <param name="X">X position</param>
        /// <param name="Y">Y Position</param>
        /// <returns>Survey Point Index. -1 means that no survey point should correspond to this location. 0-29 mean 1 through 30 in display terms. index is 0 through 29 however.</returns>
        public int GetSurveyPointArea(double X, double Y)
        {
            double RingValue = Math.Sqrt(Stars[0].Orbit.MassRelativeToSol) * Constants.SensorTN.EarthRingDistance;
            double distanceFromPrimary = Math.Sqrt(((X * X) + (Y * Y)));
            double Angle = (Math.Atan((Y / X)) / Constants.Units.Radian);


            /// <summary>
            /// Atan will give the same values for -1,-1 as 1,1, so handle this condition. likewise -1,1 will give 1,1's answer
            /// Atan(1) = 45
            /// Atan(-1) = -45
            /// -1,1(135)   1,1(45)
            /// -1,-1(225), 1,-1(315)
            /// </summary>
            if (X < 0 && Y < 0)
                Angle += 180.0;

            /// <summary>
            /// -1,1
            /// </summary>
            else if (X < 0)
                Angle += 180.0;

            /// <summary>
            /// 1,-1
            /// </summary>
            else if (Y < 0)
                Angle += 360.0;

            int SurveyIndex = -1;

            if (distanceFromPrimary >= (2 * RingValue) && distanceFromPrimary < (3 * RingValue))
            {
                /// <summary>
                /// Ring 3 contains survey indices 19 through 30, but these are addressed starting from zero.
                /// </summary>
                SurveyIndex = 29;
                for (int surveyPointIterator = 330; surveyPointIterator >= 0; surveyPointIterator -= 30)
                {
                    if (surveyPointIterator != 0)
                    {
                        int highAngle = surveyPointIterator + 15;
                        int lowAngle = surveyPointIterator - 15;
                        if (Angle <= highAngle && Angle >= lowAngle)
                        {
                            break;
                        }
                        else
                        {
                            SurveyIndex--;
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// Point zero is a special case here, though this calculation should be unnecessary.
                        /// </summary>
                        if (Angle >= 345.0 || Angle <= 15.0)
                        {
                            break;
                        }
                        else
                        {
                            /// <summary>
                            /// This is an error condition. The entire circle has been progressed through and we didn't get a match between our angle and survey index.
                            /// </summary>
                            SurveyIndex = -1;
                        }
                    }
                }
            }
            else if (distanceFromPrimary >= (RingValue) && distanceFromPrimary < (2 * RingValue))
            {
                /// <summary>
                /// Ring 2 contains survey indices 7 through 18.
                /// </summary>
                SurveyIndex = 17;
                for (int surveyPointIterator = 345; surveyPointIterator >= 15; surveyPointIterator -= 30)
                {
                    int highAngle = surveyPointIterator + 15;
                    int lowAngle = surveyPointIterator - 15;
                    if (Angle <= highAngle && Angle >= lowAngle)
                    {
                        break;
                    }
                    else
                    {
                        SurveyIndex--;
                    }
                }
            }
            else if(distanceFromPrimary < RingValue)
            {
                /// <summary>
                /// Ring 1 contains survey indices 1 through 6, again, subtract 1 to address from the survey point List which starts at index 0.
                /// because there are fewer points, each point covers more total area, 60 degrees here instead of 30.
                /// </summary>
                SurveyIndex = 5;
                for (int surveyPointIterator = 330; surveyPointIterator >= 30; surveyPointIterator -= 60)
                {
                    int highAngle = surveyPointIterator + 30;
                    int lowAngle = surveyPointIterator - 30;
                    if (Angle <= highAngle && Angle >= lowAngle)
                    {
                        break;
                    }
                    else
                    {
                        SurveyIndex--;
                    }
                }
            }

            String Entry = String.Format("GetSurveyPointArea({0},{1}):{2},{3},{4},{5}", X,Y,distanceFromPrimary,Angle,(SurveyIndex+1),RingValue);
            MessageEntry NME = new MessageEntry(MessageEntry.MessageType.Error, null, null, GameState.Instance.GameDateTime, GameState.Instance.CurrentSecond, Entry);
            GameState.Instance.Factions[0].MessageLog.Add(NME);

            return (SurveyIndex+1);
        }
    }
}
