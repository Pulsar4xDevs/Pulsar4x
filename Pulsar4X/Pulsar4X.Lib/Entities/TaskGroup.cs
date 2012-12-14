using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    public class TaskGroup
    {
        public Guid Id { get; set; }
        public Faction Faction { get; set; }

        public TaskForce TaskForce { get; set; }
        
        public string Name { get; set; }
        public long XSystem { get; set; }
        public long YSystem { get; set; }
        public int CurrentSpeed { get; set; }
        public int MaxSpeed { get; set; }
        public List<ShipTN> Ships { get; set; }

        /// <summary>
        /// List of all active sensors in this taskgroup, and the number of each.
        /// </summary>
        public BindingList<ActiveSensorTN> ActiveSensorQue;
        public BindingList<int> ActiveSensorCount;
        public int ActiveQueCount;

        /// <summary>
        /// The best thermal sensor, and the number present in the fleet and undamaged.
        /// </summary>
        public PassiveSensorTN BestThermal;
        public int BestThermalCount;

        /// <summary>
        /// The best EM sensor, and the number present in the fleet and undamaged.
        /// </summary>
        public PassiveSensorTN BestEM;
        public int BestEMCount;

        /// <summary>
        /// Each sensor stores its own lookup characteristics for at what range a particular signature is detected, but the purpose of the taskgroup 
        /// lookup tables will be to store which sensor in the taskgroup active que is best at detecting a ship with the specified TCS.
        /// </summary>
        public BindingList<int> TaskGroupLookUpST;
        public BindingList<int> TaskGroupLookUpMT;

        /// <summary>
        /// Each of these linked lists stores the index of the ships in the taskgroup, in the order from least to greatest of each respective signature.
        /// </summary>
        public LinkedList<int> ThermalSortList;
        public LinkedList<int> EMSortList;
        public LinkedList<int> ActiveSortList;


    }
}
