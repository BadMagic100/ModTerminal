using System.Collections.Generic;

namespace ModTerminal
{
    internal record SlotInfo(int Index, string Value, string? Name = null);

    internal record CommandInvocation(string Name, IReadOnlyList<SlotInfo> Slots);

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
                    slots.Add(new SlotInfo(index, namedSlot[1], namedSlot[0]));
                }
                else
                {
                    slots.Add(new SlotInfo(index, slot));
                }
            }

            return new CommandInvocation(commandName, slots);
        }
    }
}
