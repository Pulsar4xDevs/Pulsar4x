using System;
using System.ComponentModel;
using System.Reflection;

namespace Pulsar4X.Client
{
    public static class EnumExtensions
    {
        public static string ToDescription<TEnum>(this TEnum source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var sourceStr = source.ToString();
            if (string.IsNullOrEmpty(sourceStr)) throw new NullReferenceException("Somehow ToString returned null?");
            FieldInfo? fi = source.GetType().GetField(sourceStr);

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi!.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            return sourceStr;
        }
    }


    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return String.IsNullOrEmpty(s);
        }

        public static bool IsNotNullOrEmpty(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }
    }
}
