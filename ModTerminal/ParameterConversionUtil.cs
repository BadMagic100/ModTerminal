using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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

        private static readonly Dictionary<Type, string> typeNameLookup = new()
        {
            [typeof(char)] = "char",
            [typeof(string)] = "string",
            [typeof(bool)] = "bool",
            [typeof(byte)] = "byte",
            [typeof(sbyte)] = "sbyte",
            [typeof(short)] = "short",
            [typeof(ushort)] = "ushort",
            [typeof(int)] = "int",
            [typeof(uint)] = "uint",
            [typeof(long)] = "long",
            [typeof(ulong)] = "ulong",
            [typeof(float)] = "float",
            [typeof(double)] = "double",
            [typeof(decimal)] = "decimal",
            [typeof(DateTime)] = "datetime"
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

        public static string FriendlyTypeName(this Type type)
        {
            if (type.IsArray)
            {
                return FriendlyTypeName(type.GetElementType()) + "[]";
            }
            if (type.IsEnum)
            {
                return "one of " + string.Join(", ", Enum.GetNames(type));
            }

            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return FriendlyTypeName(nullableType);
            }

            if (typeNameLookup.TryGetValue(type, out string name))
            {
                return name;
            }
            return type.Name;
        }

        public static string FriendlyTypeName(this ParameterInfo p)
        {
            if (p.HasDefaultValue)
            {
                StringBuilder b = new(p.ParameterType.FriendlyTypeName());
                b.Append(" (optional");
                if (p.DefaultValue != null)
                {
                    b.Append(", default ");
                    b.Append(p.DefaultValue.ToString());
                }
                b.Append(")");
                return b.ToString();
            }
            else
            {
                return p.ParameterType.FriendlyTypeName();
            }
        }
    }
}
