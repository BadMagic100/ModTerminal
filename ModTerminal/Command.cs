using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModTerminal
{
    public class Command
    {
        private static readonly Regex namePattern = new("^[a-z]+$");

        public readonly string Name;
        public readonly MethodInfo Method;

        internal readonly bool HasResult;

        public Command(string commandName, Delegate exec)
        {
            if (!namePattern.IsMatch(commandName))
            {
                throw new ArgumentException("Command name must be lowercase letters only", nameof(commandName));
            }
            Name = commandName;

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
        }

        internal string Execute(IEnumerable<SlotInfo> slots)
        {
            // todo - actual implementation
            return "Executed command " + Name;
        }
    }
}
