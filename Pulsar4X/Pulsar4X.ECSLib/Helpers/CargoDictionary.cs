using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class CargoDictionary : Dictionary<Guid, int>
    {

        /// <summary>
        /// Adds a value to the dictionary, if the item does not exsist, it will get added to the dictionary.
        /// </summary>
        /// <param name="item">the guid of the item to add</param>
        /// <param name="value">the amount of the item to add</param>
        internal void AddValue(Guid item, int value)
        {
            if (!base.ContainsKey(item))
                base.Add(item, value);
            else
                base[item] += value;
        }

        /// <summary>
        /// Will remove the item from the dictionary if subtracting the value causes the dictionary value to be 0.
        /// </summary>
        /// <param name="item">the guid of the item to subtract</param>
        /// <param name="value">the amount of the item to subtract</param>
        /// <returns>the amount succesfully taken from the dictionary(will not remove more than what the dictionary contains)</returns>
        internal int SubtractValue(Guid item, int value)
        {
            int returnValue = 0;
            if (base.ContainsKey(item))
            {
                if (base[item] >= value)
                {
                    base[item] -= value;
                    returnValue = value;
                }
                else
                {
                    returnValue = base[item];
                    base.Remove(item);
                }
            }
            return returnValue; 
        }




    }
}
