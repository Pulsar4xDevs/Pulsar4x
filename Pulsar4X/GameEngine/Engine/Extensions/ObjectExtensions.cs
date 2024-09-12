namespace Pulsar4X.Extensions
{
    public static class ObjectExtensions
    {
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
}