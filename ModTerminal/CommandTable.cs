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

        [HelpDocumentation("Lists available commands.")]
        internal static string ListCommand(
            [HelpDocumentation("The zero-indexed page number to start on.")] uint page = 0
            )
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

        [HelpDocumentation("Displays help documentation for the specified command, or with no parameters, "
            + "displays general help documentation for the mod.")]
        internal static string HelpCommand(string? command = null)
        {
            if (command == null)
            {
                return "Use 'listcommands' to see available commands, and 'help <command>' to see help for a given command. "
                    + "Commands may take any number of parameters as specified by their help documentation. Command parameters "
                    + "can be specified in order, or by specifying the parameters in 'name=value' syntax. Ordered parameters cannot "
                    + "be used after a named parameter has been used. Some parameters take a variable amount of parameters. For "
                    + "these commands, the last parameter is listed as an array in the help documentation and you can provide any "
                    + "number of values, including zero, by adding additional ordered parameters. Named parameters cannot be used "
                    + "for these commands.";
            }

            Command? c = GetCommand(command);
            if (c == null)
            {
                return $"Cannot get help because {command} is not known command";
            }
            StringBuilder b = new();
            HelpDocumentationAttribute commandDoc = c.Method.GetCustomAttribute<HelpDocumentationAttribute>();
            if (commandDoc != null)
            {
                b.AppendLine(commandDoc.Docs);
            }

            ParameterInfo[] parameters = c.Method.GetParameters();
            if (parameters.Length == 0)
            {
                b.AppendLine("This command does not take any parameters");
            }
            else
            {
                b.AppendLine("Parameters:");
                foreach (ParameterInfo param in c.Method.GetParameters())
                {
                    b.Append("  - ");
                    b.Append(param.Name);
                    b.Append(": ");
                    b.Append(GetFriendlyTypeName(param));
                    HelpDocumentationAttribute? paramDoc = param.GetCustomAttribute<HelpDocumentationAttribute>();
                    if (paramDoc != null)
                    {
                        b.Append(". ");
                        b.Append(paramDoc.Docs);
                    }
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
                return "one of " + string.Join(", ", Enum.GetNames(type));
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
