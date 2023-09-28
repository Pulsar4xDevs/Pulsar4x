using System.ComponentModel;
using System.Reflection;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescription<TEnum>(this TEnum source)
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            return source.ToString();
        }
    }
}