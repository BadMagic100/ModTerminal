using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModTerminal
{
    public class CommandTable
    {
        private Dictionary<string, Command> commands = new();
        private Dictionary<string, CommandTable> commandGroups = new();
        public string GeneralHelp { get; }

        public string Prefix { get; private set; } = "";

        public HashSet<string> RegisteredCommandAndGroupNames => new(commands.Keys.Concat(commandGroups.Keys));

        public CommandTable(string generalHelp) 
        {
            GeneralHelp = generalHelp;
            RegisterCommand(new Command("help", HelpCommand));
            RegisterCommand(new Command("listcommands", ListCommand));
        }

        public void RegisterCommand(Command command)
        {
            if (commands.ContainsKey(command.Name))
            {
                throw new ArgumentException($"A command with the name {command.Name} is already registered", nameof(command));
            }
            if (commandGroups.ContainsKey(command.Name))
            {
                throw new ArgumentException($"A command group with the name {command.Name} is already registered", nameof(command));
            }
            commands[command.Name] = command;
        }

        public void RegisterGroup(string groupName, CommandTable commandTable)
        {
            if (commands.ContainsKey(groupName))
            {
                throw new ArgumentException($"A command with the name {groupName} is already registered", nameof(groupName));
            }
            if (commandGroups.ContainsKey(groupName))
            {
                throw new ArgumentException($"A command group with the name {groupName} is already registered", nameof(groupName));
            }
            commandTable.Prefix = Prefix + groupName + " ";
            commandGroups[groupName] = commandTable;
        }

        public CommandTable? GetGroup(string name)
        {
            if (commandGroups.TryGetValue(name, out CommandTable group))
            {
                return group;
            }
            return null;
        }

        public Command? GetCommand(string name)
        {
            if (commands.TryGetValue(name, out Command command))
            {
                return command;
            }
            return null;
        }

        [HelpDocumentation("Lists available commands.")]
        private string ListCommand(
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
                b.Append($"{Prefix}{commandName}");
                b.AppendLine();
            }
            return b.ToString();
        }

        [HelpDocumentation("Displays help documentation for the specified command, or with no parameters, "
            + "displays general help documentation.")]
        private string HelpCommand(string? command = null)
        {
            if (command == null)
            {
                return GeneralHelp;
            }

            CommandTable? ct = GetGroup(command);
            if (ct != null)
            {
                return ct.GeneralHelp;
            }

            Command? c = GetCommand(command);
            if (c == null)
            {
                return $"Cannot get help because '{Prefix}{command}' is not known command";
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
                foreach (ParameterInfo param in c.Method.GetParameters().Where(p => typeof(Command) != p.ParameterType))
                {
                    b.Append("  - ");
                    b.Append(param.Name);
                    b.Append(": ");
                    b.Append(param.FriendlyTypeName());
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
    }
}
