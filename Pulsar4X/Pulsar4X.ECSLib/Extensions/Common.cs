using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public static class Common
    {
        #region Helpful String Extensions
        public static bool IsNullOrEmpty(this string s)
        {
            return String.IsNullOrEmpty(s);
        }

        public static bool IsNotNullOrEmpty(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }

        public static bool IsNullOrWhitespace(this string s)
        {
            return String.IsNullOrWhiteSpace(s);
        }

        public static bool IsNotNullOrWhitespace(this string s)
        {
            return !String.IsNullOrWhiteSpace(s);
        }
        #endregion

        #region Helpful Generic Enum Extensions
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
        #endregion
    }
}
