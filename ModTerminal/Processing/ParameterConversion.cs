using ModTerminal.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModTerminal.Processing
{
    internal static class ParameterConversion
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

        private static Attribute? GetConverterAttribute(this ParameterInfo p)
        {
            return p.GetCustomAttributes(inherit: false)
                .OfType<Attribute>()
                .Where(a => a.GetType().IsGenericType)
                .Where(a => a.GetType().GetGenericTypeDefinition() == typeof(ParameterConverterAttribute<>))
                .FirstOrDefault();
        }

        public static bool IsConvertible(this ParameterInfo p)
        {
            if (p.GetConverterAttribute() != null)
            {
                return true;
            }
            return p.ParameterType.ConversionType().IsConvertible();
        }

        public static bool IsConvertible(this Type t) => convertibleTypes.Contains(t);

        public static bool TryConvertValue(this ParameterInfo p, string value, out object? result)
        {
            result = null;

            Type targetType = p.ParameterType.ConversionType();
            Attribute? converterAttr = p.GetConverterAttribute();
            if (converterAttr != null)
            {
                Type converterType = converterAttr.GetType().GetGenericArguments().First();
                IValueConverter? converter = Activator.CreateInstance(converterType) as IValueConverter;
                if (converter == null)
                {
                    return false;
                }
                try
                {
                    // assume that the converter can always produce a valid value for the param - if it can't,
                    // that's a modder bug not a user error, so any crash that happens should be caught before release
                    // and graceful failure is not needed.
                    result = converter.Convert(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }


            try
            {
                if (targetType.IsEnum)
                {
                    object slotValue = Enum.Parse(targetType, value);
                    if (Enum.IsDefined(targetType, slotValue))
                    {
                        result = slotValue;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    result = Convert.ChangeType(value, targetType);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static Type ConversionType(this Type t)
        {
            if (t.IsArray)
            {
                return t.GetElementType().ConversionType();
            }
            return Nullable.GetUnderlyingType(t) ?? t;
        }

        public static string FriendlyTypeName(this Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType().FriendlyTypeName() + "[]";
            }
            if (type.IsEnum)
            {
                return "one of " + string.Join(", ", Enum.GetNames(type));
            }

            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return nullableType.FriendlyTypeName();
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
