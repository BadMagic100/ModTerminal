using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModTerminal
{
    public static class CommandTable
    {
        private static Dictionary<string, Command> commands = new();

        public static void RegisterCommand(Command command)
        {
            if (commands.ContainsKey(command.Name))
            {
                throw new ArgumentException($"A command with the name {command.Name} is already registered", nameof(command));
            }
            commands[command.Name] = command;
        }

        public static Command? GetCommand(string name)
        {
            if (commands.TryGetValue(name, out Command command))
            {
                return command;
            }
            return null;
        }

        internal static string ListCommand(uint page = 0)
        {
            const int PAGE_SIZE = 5;
            int first = (int)page * PAGE_SIZE + 1;
            int last = Math.Min((int)(page + 1) * PAGE_SIZE, commands.Count);
            int count = commands.Count;
            if (first > count)
            {
                return "No more commands available";
            }

            StringBuilder b = new($"Showing commands {first}-{last} of {count}:\n");
            foreach (string commandName in commands.Keys.OrderBy(k => k).Skip(first - 1).Take(PAGE_SIZE)) 
            {
                b.Append("  - ");
                b.Append(commandName);
                b.AppendLine();
            }
            return b.ToString();
        }

        internal static string HelpCommand(string commandName)
        {
            Command? command = GetCommand(commandName);
            if (command == null)
            {
                return $"Cannot get help because {commandName} is not known command";
            }
            StringBuilder b = new();
            ParameterInfo[] parameters = command.Method.GetParameters();
            if (parameters.Length == 0)
            {
                b.AppendLine("This command does not take any parameters");
            }
            else
            {
                b.AppendLine("Parameters:");
                foreach (ParameterInfo param in command.Method.GetParameters())
                {
                    b.Append("  - ");
                    b.Append(param.Name);
                    b.Append(": ");
                    b.Append(GetFriendlyTypeName(param));
                    b.AppendLine();
                }
            }
            return b.ToString();
        }

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

        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsArray)
            {
                return GetFriendlyTypeName(type.GetElementType()) + "[]";
            }
            if (type.IsEnum)
            {
                return "One of: " + string.Join(", ", Enum.GetNames(type));
            }

            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return GetFriendlyTypeName(nullableType);
            }

            if (typeNameLookup.TryGetValue(type, out string name))
            {
                return name;
            }
            return type.Name;
        }

        private static string GetFriendlyTypeName(ParameterInfo p)
        {
            if (p.HasDefaultValue)
            {
                StringBuilder b = new(GetFriendlyTypeName(p.ParameterType));
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
                return GetFriendlyTypeName(p.ParameterType);
            }
        }
    }
}
