using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModTerminal
{
    public class Command
    {
        private static readonly Regex namePattern = new("^[a-z]+$");
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

        public readonly string Name;

        private Delegate Delegate;
        internal readonly MethodInfo Method;
        private readonly bool HasResult;

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
                if (param.ParameterType.IsArray && !convertibleTypes.Contains(param.ParameterType.GetElementType()))
                {
                    throw new ArgumentException($"{param.Name} is not a convertible type", nameof(exec));
                }
                if (!param.ParameterType.IsArray && !convertibleTypes.Contains(param.ParameterType))
                {
                    throw new ArgumentException($"{param.Name} is not a convertible type", nameof(exec));
                }
            }
        }

        internal string? Execute(IReadOnlyList<SlotInfo> slots)
        {
            ParameterInfo[] parameters = Method.GetParameters();
            Dictionary<string, ParameterInfo> paramLookup = parameters.ToDictionary(p => p.Name);
            // validate slots
            bool encounteredNamedArg = false;
            foreach (SlotInfo slot in slots)
            {
                if (slot.Name == null && encounteredNamedArg)
                {
                    return $"Error at {slot.Value}: all indexed parameters must come before named parameters";
                }
                if (slot.Name != null)
                {
                    encounteredNamedArg = true;
                }
            }

            if (parameters.Last().ParameterType.IsArray)
            {
                if (encounteredNamedArg)
                {
                    return "Error: named arguments cannot be used with variable-length parameters";
                }
                if (slots.Count < parameters.Length - 1)
                {
                    return $"Error: not enough parameters for {Name}. Use the 'help {Name}' command to see the correct parameters";
                }
            }
            else
            {
                if (slots.Count < parameters.Length)
                {
                    return $"Error: not enough parameters for {Name}. Use the 'help {Name}' command to see the correct parameters";
                }
                if (slots.Count > parameters.Length)
                {
                    return $"Error: too many parameters for {Name}. Use the 'help {Name}' command to see the correct parameters";
                }
            }

            // match slots to actual params
            object[] args = new object[parameters.Length];
            foreach (SlotInfo slot in slots)
            {
                ParameterInfo param = slot.Name == null
                    ? parameters[Math.Min(parameters.Length - 1, slot.Index)]
                    : paramLookup[slot.Name];
                Type targetType = param.ParameterType.IsArray ? param.ParameterType.GetElementType() : param.ParameterType;
                object slotValue;
                try
                {
                    slotValue = Convert.ChangeType(slot.Value, targetType);
                }
                catch
                {
                    return $"Error: could not convert '{slot.Value}' to the correct type for {param.Name}. "
                        + $"Use the 'help {Name}' command to see the correct parameters";
                }

                int argIndex = param.Position;
                if (param.ParameterType.IsArray)
                {
                    if (args[argIndex] == null)
                    {
                        args[argIndex] = Array.CreateInstance(targetType, slots.Count - parameters.Length + 1);
                        ModTerminalMod.Instance.LogDebug($"Length: {slots.Count - parameters.Length + 1}");
                    }
                    Array arr = (Array)args[argIndex];
                    int pos = slot.Index - argIndex;
                    ModTerminalMod.Instance.LogDebug($"{slot.Name} at {slot.Index} - pos: {pos}");
                    arr.SetValue(slotValue, pos);
                }
                else
                {
                    args[argIndex] = slotValue;
                }
            }

            object result = Delegate.DynamicInvoke(args);
            if (HasResult)
            {
                return (string)result;
            }
            return null;
        }
    }
}
