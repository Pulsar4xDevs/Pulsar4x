using System;

namespace Pulsar4X.Extensions
{
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

        public static bool IsNullOrWhitespace(this string s)
        {
            return String.IsNullOrWhiteSpace(s);
        }

        public static bool IsNotNullOrWhitespace(this string s)
        {
            return !String.IsNullOrWhiteSpace(s);
        }
    }
}