using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
namespace Pulsar4X.Entities
{
    public class Order
    {
        /// <summary>
        /// What order type is this
        /// </summary>
        private Constants.ShipTN.OrderType TypeOf { get; set; }
        public Constants.ShipTN.OrderType typeOf
        {
            get { return TypeOf; }
        }
        public List<Constants.ShipTN.OrderType> EnablesTypeOf()
        {
            List<Constants.ShipTN.OrderType> enabledType = new List<Constants.ShipTN.OrderType>();
            switch (this.TypeOf)
            {
                case Constants.ShipTN.OrderType.Absorb://add list of possible orders target fleet enables.
                    TaskGroupTN targettg = (TaskGroupTN)Target;
                    enabledType = targettg.LegalOrdersTG();
                    break;
                case Constants.ShipTN.OrderType.LoadAllMinerals:
                case Constants.ShipTN.OrderType.LoadMineral:
                case Constants.ShipTN.OrderType.LoadMineralWhenX:
                case Constants.ShipTN.OrderType.LoadOrUnloadMineralsToReserve:
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadAllMinerals);
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadMineral);
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadAll);
                    enabledType.Add(Constants.ShipTN.OrderType.LoadOrUnloadMineralsToReserve);
                    break;
                case Constants.ShipTN.OrderType.LoadColonists:
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadColonists);
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadAll);
                    break;
                case Constants.ShipTN.OrderType.LoadInstallation:
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadInstallation);
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadAll);
                    break;
                case Constants.ShipTN.OrderType.LoadShipComponent:
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadShipComponent);
                    enabledType.Add(Constants.ShipTN.OrderType.UnloadAll);
                    break;
                case Constants.ShipTN.OrderType.TractorSpecifiedShip:
                case Constants.ShipTN.OrderType.TractorSpecifiedShipyard:
                    enabledType.Add(Constants.ShipTN.OrderType.ReleaseAt);
                    break;
            }

            return enabledType;
        }

        public string Name;

        /// <summary>
        /// What entity are those orders pointed at?
        /// </summary>
        private StarSystemEntity Target { get; set; }
        public StarSystemEntity target
        {
            get { return Target; }
        }

        /// <summary>
        /// If SSE is a TG then this is that task group.
        /// </summary>
        private TaskGroupTN TaskGroup { get; set; }
        public TaskGroupTN taskGroup
        {
            get { return TaskGroup; }
        }

        /// <summary>
        /// SSE might be a planet, if so it is here.
        /// </summary>
        private SystemBody Body { get; set; }
        public SystemBody body
        {
            get { return Body; }
        }
        /// <summary>
        /// If SSE is a population then this is that population.
        /// </summary>
        private Population Pop { get; set; }
        public Population pop
        {
            get { return Pop; }
        }

        /// <summary>
        /// IF SSE should be a jumppoint then said Jumppoint is stored here.
        /// </summary>
        private JumpPoint JumpPoint { get; set; }
        public JumpPoint jumpPoint
        {
            get { return JumpPoint; }

        }

        /// <summary>
        /// storage for waypoint targeted orders.
        /// </summary>
        private Waypoint WayPoint { get; set; }
        public Waypoint wayPoint
        {
            get { return WayPoint; }
        }

        /// <summary>
        /// Storage for survey point targeted orders.
        /// </summary>
        private SurveyPoint SurveyPointOrder { get; set; }
        public SurveyPoint surveyPointOrder
        {
            get { return SurveyPointOrder; }
        }

        /// <summary>
        /// Installation/Ship Component/Troops/Ship as part of taskgroup identifier for load/tractor orders.
        /// </summary>
        private int Secondary { get; set; }
        public int secondary
        {
            get { return Secondary; }
        }

        /// <summary>
        /// Order limits. how much to load and unload for example.
        /// </summary>
        private int Tertiary { get; set; }
        public int tertiary
        {
            get { return Tertiary; }
        }

        /// <summary>
        /// How long should the taskgroup wait before performing this order?
        /// </summary>
        private int OrderDelay { get; set; }
        public int orderDelay
        {
            get { return OrderDelay; }
            set { OrderDelay = value; }
        }

        /// <summary>
        /// Time required to perform any non movement related portions of this order.
        /// </summary>
        private int OrderTimeRequirement { get; set; }
        public int orderTimeRequirement
        {
            get { return OrderTimeRequirement; }
            set { OrderTimeRequirement = value; }
        }

        /// <summary>
        /// Orders to specific ship contacts.
        /// </summary>
        private ShipTN ShipOrder { get; set; }
        public ShipTN shipOrder
        {
            get { return ShipOrder; }
            set { ShipOrder = value; }
        }

        /// <summary>
        /// Constructor for TaskGroup related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits.</param>
        /// <param name="Delay">Delay in seconds before performing this order.</param>
        /// <param name="TaskGroupOrder">The TaskGroup in question.</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, TaskGroupTN TaskGroupOrder)
        {
            TypeOf = TypeOrder;
            Target = TaskGroupOrder.Contact;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            TaskGroup = TaskGroupOrder;
            OrderDelay = Delay;

            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + TaskGroupOrder.Name.ToString();
        }

        /// <summary>
        /// Constructor for planet related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits.</param>
        /// <param name="Delay">Delay in seconds before performing this order.</param>
        /// <param name="PlanetOrder">The SystemBody in question.</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, SystemBody PlanetOrder)
        {
            TypeOf = TypeOrder;
            Target = PlanetOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            Body = PlanetOrder;
            OrderDelay = Delay;

            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + PlanetOrder.Name.ToString();
        }

        /// <summary>
        /// Constructor for Population related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits.</param>
        /// <param name="Delay">Delay in seconds before performing this order.</param>
        /// <param name="PopOrder">The Population in question.</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, Population PopOrder)
        {
            TypeOf = TypeOrder;
            Target = PopOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            Pop = PopOrder;
            OrderDelay = Delay;


            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + PopOrder.Name.ToString();
        }

        /// <summary>
        /// Constructor for Jump point related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits.</param>
        /// <param name="Delay">Delay in seconds before performing this order.</param>
        /// <param name="JPOrder">The Jump Point in question.</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, JumpPoint JPOrder)
        {
            TypeOf = TypeOrder;
            Target = JPOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            JumpPoint = JPOrder;
            OrderDelay = Delay;


            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + JPOrder.Name.ToString();
        }

        /// <summary>
        /// Constructor for way point related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits.</param>
        /// <param name="Delay">Delay in seconds before performing this order.</param>
        /// <param name="WPOrder">The Way Point in question.</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, Waypoint WPOrder)
        {
            TypeOf = TypeOrder;
            Target = WPOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            WayPoint = WPOrder;
            OrderDelay = Delay;


            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + WPOrder.Name.ToString();
        }

        /// <summary>
        /// Constructor for detected contact related order.
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Secondary</param>
        /// <param name="TertiaryOrder">Tertiary</param>
        /// <param name="Delay">Order delay</param>
        /// <param name="ShipsOrder">Ship target of order</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, ShipTN ShipsOrder)
        {
            TypeOf = TypeOrder;
            Target = ShipsOrder.ShipsTaskGroup.Contact;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            ShipOrder = ShipsOrder;
            TaskGroup = ShipsOrder.ShipsTaskGroup;
            OrderDelay = Delay;

            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + ShipOrder.Name.ToString();
        }

        /// <summary>
        /// Constructor for SurveyPoint
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Secondary</param>
        /// <param name="TertiaryOrder">Tertiary</param>
        /// <param name="Delay">Order delay</param>
        /// <param name="ShipsOrder">Ship target of order</param>
        public Order(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, SurveyPoint SPOrder)
        {
            TypeOf = TypeOrder;
            Target = SPOrder;
            Secondary = SecondaryOrder;
            Tertiary = TertiaryOrder;
            SurveyPointOrder = SPOrder;
            OrderDelay = Delay;

            OrderTimeRequirement = -1;

            Name = TypeOrder.ToString() + " " + SurveyPointOrder.Name.ToString();
        }
    }


}
