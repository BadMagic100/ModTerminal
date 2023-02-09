using System.Collections.Generic;
using System.Linq;

namespace ModTerminal
{
    internal record SlotInfo(string Raw, int Index, string Value, string? Name = null);

    internal record CommandInvocation(string Name, IReadOnlyList<SlotInfo> Slots);

    internal record ScopedCommandInvocation(string FullName, IReadOnlyList<SlotInfo> Slots, CommandTable OwningTable, CommandInvocation FinalInvocation);

    internal static class CommandParser
    {
        public static CommandInvocation ParseCommand(string command)
        {
            string[] fragments = command.Split(' ');
            string commandName = fragments[0];
            List<SlotInfo> slots = new();
            for (int i = 1; i < fragments.Length; i++)
            {
                int index = i - 1;
                string slot = fragments[i].Trim();
                if (slot.Contains("="))
                {
                    string[] namedSlot = slot.Split(new char[] { '=' }, 2);
                    slots.Add(new SlotInfo(slot, index, namedSlot[1], namedSlot[0]));
                }
                else
                {
                    slots.Add(new SlotInfo(slot, index, slot));
                }
            }

            return new CommandInvocation(commandName, slots);
        }

        public static ScopedCommandInvocation ScopeCommandInvocation(CommandTable rootTable, CommandInvocation invocation)
        {
            List<string> fullNameParts = new() { invocation.Name };
            CommandTable table = rootTable;
            CommandTable? nextTable = table.GetGroup(invocation.Name);
            IReadOnlyList<SlotInfo> slots = invocation.Slots;
            // look for command tables until valid subgroups have been exhausted
            while (nextTable != null && slots.Count >= 1)
            {
                string newName = slots[0].Raw;
                fullNameParts.Add(newName);

                slots = slots.Skip(1).Select(x => x with { Index = x.Index - 1 }).ToList();
                table = nextTable;
                nextTable = nextTable.GetGroup(newName);
            }

            return new ScopedCommandInvocation(
                string.Join(" ", fullNameParts), 
                slots, 
                table, 
                new CommandInvocation(fullNameParts[fullNameParts.Count - 1], slots)
            );
        }
    }
}
