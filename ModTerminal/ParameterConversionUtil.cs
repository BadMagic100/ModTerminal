using System;
using System.Collections.Generic;

namespace ModTerminal
{
    internal static class ParameterConversionUtil
    {

        private static readonly HashSet<Type> convertibleTypes = new()
        {
            typeof(char),
            typeof(string),
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime)
        };

        public static bool IsConvertible(this Type t) => convertibleTypes.Contains(t);

        public static Type ConversionType(this Type t)
        {
            if (t.IsArray)
            {
                return ConversionType(t.GetElementType());
            }
            return Nullable.GetUnderlyingType(t) ?? t;
        }
    }
}
