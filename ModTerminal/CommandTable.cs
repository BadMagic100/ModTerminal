using System;
using System.Collections.Generic;
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

        internal static string HelpCommand(string commandName)
        {
            Command? command = GetCommand(commandName);
            if (command == null)
            {
                return $"Cannot get help because {commandName} is not known command";
            }
            StringBuilder b = new("Slots:\n");
            foreach (ParameterInfo param in command.Method.GetParameters())
            {
                b.Append("  - ");
                b.Append(param.Name);
                b.Append(": ");
                b.Append(param.ParameterType.Name);
                b.AppendLine();
            }
            return b.ToString();
        }
    }
}
