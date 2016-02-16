using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModdingTools.JsonDataEditor
{
    public static class Extensions
    {
        /// <summary>
        /// Determines whether the collection is null or contains no elements.
        /// thanks to http://danielvaughan.org/post/IEnumerable-IsNullOrEmpty.aspx
        /// !!!NOTE Becarefull with this as it'll confuse you if you try use it with a string, 
        /// it'll accept a string and it'll return false when it should return true
        /// </summary>
        /// <typeparam name="T">The IEnumerable type.</typeparam>
        /// <param name="enumerable">The enumerable, which may be null or empty.</param>
        /// <returns>
        ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }
            /* If this is a list, use the Count property. 
             * The Count property is O(1) while IEnumerable.Count() is O(N). */
            var collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                return collection.Count < 1;
            }
            return enumerable.Any();
        }

        /// <summary>
        /// Determines whether the collection is null or contains no elements.
        /// http://danielvaughan.org/post/IEnumerable-IsNullOrEmpty.aspx
        /// </summary>
        /// <typeparam name="T">The IEnumerable type.</typeparam>
        /// <param name="collection">The collection, which may be null or empty.</param>
        /// <returns>
        ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            if (collection == null)
            {
                return true;
            }
            return collection.Count < 1;
        }

        public static object ConvertList(List<object> value, Type type)
        {
            IList list = (IList)Activator.CreateInstance(type);
            foreach (var item in value)
            {
                list.Add(item);
            }
            return list;
        }
    }

}
