﻿using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModTerminal
{
    public class Command
    {
        private static readonly Regex namePattern = new("^[a-z]+$");

        public readonly string Name;

        private Delegate Delegate;
        private readonly bool HasResult;
        public readonly MethodInfo Method;

        public Command(string commandName, Delegate exec)
        {
            if (!namePattern.IsMatch(commandName))
            {
                throw new ArgumentException("Command name must be lowercase letters only", nameof(commandName));
            }
            Name = commandName;

            Delegate = exec;
            Method = exec.Method;
            if (Method.ReturnType == typeof(void))
            {
                HasResult = false;
            }
            else if (Method.ReturnType == typeof(string))
            {
                HasResult = true;
            }
            else
            {
                throw new ArgumentException("Commands must be of type void if they don't yield a result," +
                    " or of type string if they do", nameof(exec));
            }

            ParameterInfo[] parameters = Method.GetParameters();
            foreach(ParameterInfo param in parameters)
            {
                if (param.ParameterType.IsArray && param.Position != parameters.Length - 1)
                {
                    throw new ArgumentException("Array parameters are only legal in the final position (e.g. params arrays)",
                        nameof(exec));
                }
                Type targetType = param.ParameterType.ConversionType();
                if (!targetType.IsEnum && !targetType.IsConvertible())
                {
                    throw new ArgumentException($"{param.Name} is not a convertible type", nameof(exec));
                }
            }
        }

        internal string? Execute(object?[] args)
        {
            object result = Delegate.DynamicInvoke(args);
            if (HasResult)
            {
                return (string)result;
            }
            return null;
        }
    }
}
