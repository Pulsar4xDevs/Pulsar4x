using System;
using System.ComponentModel;
using System.Reflection;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescription<TEnum>(this TEnum source)
        {
            if(source == null) throw new ArgumentNullException("source cannot be null");
            var sourceStr = source.ToString();
            if(string.IsNullOrEmpty(sourceStr)) throw new NullReferenceException("Somehow ToString returned null?");
            FieldInfo? fi = source.GetType().GetField(sourceStr);

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            return sourceStr;
        }
    }
}