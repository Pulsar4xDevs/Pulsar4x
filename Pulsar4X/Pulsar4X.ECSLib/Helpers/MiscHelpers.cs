using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class Misc
    {

        public static string StringifyWeight(double amountInKg, string format = "0.###")
        {
            string stringWeight = "0 Kg";
            if (amountInKg > 100000000)
            {
                amountInKg = amountInKg * 0.00000001;
                stringWeight = amountInKg.ToString(format) + " MT";
            }
            else if (amountInKg > 100000)
            {
                amountInKg = amountInKg * 0.00001;
                stringWeight = amountInKg.ToString(format) + " KT";
            }
            else if (amountInKg > 1000)
            {
                amountInKg = amountInKg * 0.001;
                stringWeight = amountInKg.ToString(format) + " T";
            }

            else { stringWeight = amountInKg.ToString(format) + " Kg"; }

            return stringWeight;
        }

        
        public static string StringifyDistance(double length_m,  string format = "0.###")
        {

            string stringDistance = "0 m";
            double abslen = Math.Abs(length_m);
            double len;
            if (abslen > 1.0e12)
            {
                len = length_m * 1.0e-12;
                stringDistance = len.ToString(format) + " GKm";
            }
            else if (abslen > 1.0e9)
            {
                len = length_m * 1.0e-9;
                stringDistance = len.ToString(format) + " MKm";
            }
            else if (abslen > 1.0e6)
            {
                len = length_m * 1.0e-6;
                stringDistance = len.ToString(format) + " KKm";
            }
            else if (abslen > 1.0e3)
            {
                len = length_m * 0.001;
                stringDistance = len.ToString(format) + " Km";
            }
            
            else { stringDistance = length_m.ToString(format) + " m"; }

            return stringDistance;
        }


        public static string StringifyVelocity(double velocity_m, string format = "0.##")
        {
            string stringVelocity = " 0 m/s";
            if (velocity_m > 1.0e9)
            {
                velocity_m = velocity_m * 1.0e-9;
                stringVelocity = velocity_m.ToString(format) + " Gm/s";
            }
            else if (velocity_m > 1.0e6)
            {
                velocity_m = velocity_m * 1.0e-6;
                stringVelocity = velocity_m.ToString(format) + " Mm/s";
            }
            else if (velocity_m > 1.0e3)
            {
                velocity_m = velocity_m * 1.0e-3;
                stringVelocity = velocity_m.ToString(format) + " Km/s";
            }

            else { stringVelocity = velocity_m.ToString(format) + " m/s"; }

            return stringVelocity;
        }


        public static string StringifyThrust(double thrust_n, string format = "0.00")
        {
            string stringThrust = " 0 KN";
            if (thrust_n > 1.0e9)
            {
                thrust_n = thrust_n * 1.0e-9;
                stringThrust = thrust_n.ToString(format) + " GN";
            }
            else if (thrust_n > 1.0e6)
            {
                thrust_n = thrust_n * 1.0e-6;
                stringThrust = thrust_n.ToString(format) + " MN";
            }
            else if (thrust_n > 1.0e3)
            {
                thrust_n = thrust_n * 1.0e-3;
                stringThrust = thrust_n.ToString(format) + " KN";
            }

            else { stringThrust = thrust_n.ToString(format) + " KN"; }

            return stringThrust;
        }

        public static bool HasReqiredItems(Dictionary<Guid, int> stockpile, Dictionary<Guid, int> costs)
        {
            if (costs == null)
                return true;
            return costs.All(kvp => stockpile.ContainsKey(kvp.Key) && (stockpile[kvp.Key] >= kvp.Value));
        }

        public static void UseFromStockpile(Dictionary<Guid, int> stockpile, Dictionary<Guid, int> costs)
        {
            if (costs != null)
            {
                foreach (var kvp in costs)
                {
                    stockpile[kvp.Key] -= kvp.Value;
                }
            }
        }

        /// <summary>
        /// Values the hash.
        /// </summary>
        /// <returns>The hash.</returns>
        /// <param name="obj">Object.</param>
        /// <param name="hash">if mulitiple values need to be hashed, include the previous hash</param>
        public static int ValueHash(object obj, int hash = 17)
        {
            if (obj != null)
            {
                unchecked { hash = hash * 31 + obj.GetHashCode(); }
            }            
            return hash;
        }
        


    }

    public static class DictionaryExtension
    {
        /// <summary>
        /// Adds an int value to a dictionary, adding the key if the key does not exsist.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="toReplace"></param>
        [PublicAPI]
        public static void SafeValueReplace<TKey>(this Dictionary<TKey, int> dict, TKey key, int toReplace)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, toReplace);
            else
            {
                dict[key] = toReplace;
            }
        }

        public static void SafeValueReplace<TKey>(this Dictionary<TKey, long> dict, TKey key, long toReplace)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, toReplace);
            else
            {
                dict[key] = toReplace;
            }
        }

        /// <summary>
        /// Adds an int value to a dictionary, adding the key if the key does not exsist.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="toAdd"></param>
        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, int> dict, TKey key, int toAdd)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, toAdd);
            else
            {
                dict[key] += toAdd;
            }
        }

        /// <summary>
        /// Adds an long value to a dictionary, adding the key if the key does not exsist.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="toAdd"></param>
        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, long> dict, TKey key, long toAdd)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, toAdd);
            else
            {
                dict[key] += toAdd;
            }
        }

        /// <summary>
        /// Adds a float value to a dictionary, adding the key if the key does not exsist.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="toAdd"></param>
        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, float> dict, TKey key, float toAdd)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, toAdd);
            else
            {
                dict[key] += toAdd;
            }
        }
        /// <summary>
        /// Adds a double value to a dictionary, adding the key if the key does not exsist.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="toAdd"></param>
        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, double> dict, TKey key, double toAdd)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, toAdd);
            else
            {
                dict[key] += toAdd;
            }
        }
    }

    public static class ListExtension
    {
        [PublicAPI]
        public static List<Entity> GetEntititiesWithDataBlob<TDataBlob>(this List<Entity> list) where TDataBlob : BaseDataBlob
        {
            var retVal = new List<Entity>();
            foreach (Entity entity in list)
            {
                if (entity.HasDataBlob<TDataBlob>())
                {
                    retVal.Add(entity);
                }
            }

            return retVal;
        }
    }

   

}
