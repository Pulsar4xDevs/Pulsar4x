using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
namespace Pulsar4X.Entities
{
    public class Orders : GameEntity
    {
        /// <summary>
        /// What order type is this
        /// </summary>
        private Constants.ShipTN.OrderType TypeOf { get; set; }
        public Constants.ShipTN.OrderType typeOf
        {
            get { return TypeOf; }
        }

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
        private Planet Body { get; set; }
        public Planet body
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
        /// Constructor for TaskGroup related orders
        /// </summary>
        /// <param name="TypeOrder">Type</param>
        /// <param name="SecondaryOrder">Any secondary order specification such as installation type.</param>
        /// <param name="TertiaryOrder"> Any Tertiary order such as limits.</param>
        /// <param name="Delay">Delay in seconds before performing this order.</param>
        /// <param name="TaskGroupOrder">The TaskGroup in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, TaskGroupTN TaskGroupOrder)
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
        /// <param name="PlanetOrder">The Planet in question.</param>
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, Planet PlanetOrder)
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
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, Population PopOrder)
        {
            TypeOf = TypeOrder;
            Target = PopOrder.Planet;
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
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, JumpPoint JPOrder)
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
        public Orders(Constants.ShipTN.OrderType TypeOrder, int SecondaryOrder, int TertiaryOrder, int Delay, Waypoint WPOrder)
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
    }


}
