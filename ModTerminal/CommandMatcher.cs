using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NotNullAttribute = Antlr4.Runtime.Misc.NotNullAttribute;

namespace ModTerminal
{
    internal record AnnotatedSlotInfo(Interval SourceInterval, ParserRuleContext Context, string Value);
    internal record AnnotatedNamedSlotInfo(Interval SourceInterval, ParserRuleContext Context, string Name, string Value) 
        : AnnotatedSlotInfo(SourceInterval, Context, Value);

    public enum InvocationType
    {
        CommandGroup,
        Command,
        Unknown
    }

    internal class CommandMatcher : TerminalCommandBaseListener
    {
        private CommandTable currentTable;
        private Command? currentCommand;
        private object?[] args = new object[0];
        private readonly List<string> fullNameParts = new();
        private readonly List<string> errors = new();
        private readonly List<AnnotatedSlotInfo> orderedSlots = new();
        private readonly List<AnnotatedNamedSlotInfo> namedSlots = new();

        private bool canSearchForCommand = true;
        private InvocationType invocationType = InvocationType.Unknown;

        [MemberNotNullWhen(true, nameof(Command))]
        public bool FoundCommand => currentCommand != null;
        public CommandTable Table => currentTable;
        public Command? Command => currentCommand;

        public string FullRequestedCommandName => string.Join(" ", fullNameParts);
        public InvocationType InvocationType => invocationType;

        public IReadOnlyList<string> CollectedSemanticErrors => errors.AsReadOnly();
        public object?[] CollectedParameters
        {
            get
            {
                object?[] result = new object[args.Length];
                args.CopyTo(result, 0);
                return result;
            }
        }

        public CommandMatcher(CommandTable rootTable)
        {
            this.currentTable = rootTable;
        }

        public override void EnterValue([NotNull] TerminalCommandParser.ValueContext context)
        {
            // values which are direct children of a command are either (sub)command names or ordered params
            if (context.Parent is TerminalCommandParser.CommandContext)
            {
                // if we haven't found a command yet, attempt to do so using valid identifiers.
                if (currentCommand == null && !IsMissingToken(context.ID()) && canSearchForCommand)
                {
                    string name = context.ID().GetText();
                    fullNameParts.Add(name);
                    if (currentTable.GetGroup(name) is CommandTable ct)
                    {
                        currentTable = ct;
                        invocationType = InvocationType.CommandGroup;
                        return;
                    }
                    else if (currentTable.GetCommand(name) is Command c)
                    {
                        currentCommand = c;
                        invocationType = InvocationType.Command;
                        return;
                    }
                    else
                    {
                        // failure - could not find a command or command group as specified by full name.
                        canSearchForCommand = false;
                        invocationType = InvocationType.Command;
                        return;
                    }
                }

                // the current value is not being used as a lookup (either because we already found a command,
                // or because it's not legal to do so); use it instead as an ordered slot value
                string value = ReadValue(context);
                orderedSlots.Add(new AnnotatedSlotInfo(RangeOf(context), context, value));
                if (currentCommand == null)
                {
                    errors.Add($"Ordered parameter '{context.GetText()}' was not expected.");
                }
            }
        }

        public override void EnterNamedParameter([NotNull] TerminalCommandParser.NamedParameterContext context)
        {
            if (!IsMissingToken(context.ID())) 
            {
                string name = context.ID().GetText();
                string value = context.value() == null ? "true" : ReadValue(context.value());
                namedSlots.Add(new AnnotatedNamedSlotInfo(RangeOf(context), context, name, value));

                if (currentCommand == null)
                {
                    errors.Add($"Named parameter '{context.GetText()}' was not expected.");
                }
            }
            else
            {
                // this corresponds to a syntax error, it's used only for completion
                namedSlots.Add(new AnnotatedNamedSlotInfo(RangeOf(context), context, "", ""));
            }
        }

        public override void ExitCommand([NotNull] TerminalCommandParser.CommandContext context)
        {
            if (currentCommand == null)
            {
                args = new object[0];
                return;
            }

            // attempt to build a parameter array for the command, and collect errors.
            ParameterInfo[] parameters = currentCommand.Method.GetParameters();
            Dictionary<string, ParameterInfo> paramLookup = parameters.ToDictionary(p => p.Name);
            List<ParameterInfo> unsetParameters = parameters.ToList();
            args = new object[parameters.Length];

            if (parameters.Length > 0 && typeof(Command) == parameters[0].ParameterType)
            {
                args[0] = currentCommand;
                unsetParameters.Remove(parameters[0]);
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }
            }

            foreach (IGrouping<string, AnnotatedNamedSlotInfo> namedSlotGroup in namedSlots.GroupBy(s => s.Name))
            {
                string name = namedSlotGroup.Key;
                if (paramLookup.TryGetValue(name, out ParameterInfo target))
                {
                    Type targetType = target.ParameterType.ConversionType();
                    object?[] vals = namedSlotGroup.Select(v =>
                    {
                        if (!target.TryConvertValue(v.Value, out object? result))
                        {
                            errors.Add($"Could not convert '{v}' to type {targetType.Name} for argument {target.Name}");
                        }
                        return result;
                    }).ToArray();

                    if (target.ParameterType.IsArray)
                    {
                        Array arr = Array.CreateInstance(targetType, vals.Length);
                        vals.CopyTo(arr, 0);
                        args[target.Position] = arr;
                    }
                    else if (vals.Length == 1)
                    {
                        args[target.Position] = vals[0];
                    }
                    else
                    {
                        errors.Add($"Mutilple values provided for non-array argument {name}");
                    }
                    unsetParameters.Remove(target);
                }
                else
                {
                    errors.Add($"One or more values provided for nonexistant argument {name}");
                }
            }

            for (int i = 0; i < orderedSlots.Count; i++)
            {
                if (unsetParameters.Count == 0)
                {
                    errors.Add($"An unexpected ordered parameter '{orderedSlots[i].Value}' was provided with no corresponding actual parameter.");
                    continue;
                }

                ParameterInfo target = unsetParameters[0];
                Type targetType = target.ParameterType.ConversionType();
                if (target.ParameterType.IsArray)
                {
                    List<object?> vals = new();
                    while (i < orderedSlots.Count)
                    {
                        if (!target.TryConvertValue(orderedSlots[i].Value, out object? result))
                        {
                            errors.Add($"Could not convert '{orderedSlots[i].Value}' to type {targetType.Name} for argument {target.Name}");
                        }
                        vals.Add(result);
                        i++;
                    }
                    Array arr = Array.CreateInstance(targetType, vals.Count);
                    vals.ToArray().CopyTo(arr, 0);
                    args[target.Position] = arr;
                }
                else
                {
                    // do conversion and apply
                    if (!target.TryConvertValue(orderedSlots[i].Value, out object? result))
                    {
                        errors.Add($"Could not convert '{orderedSlots[i].Value}' to type {targetType.Name} for argument {target.Name}");
                    }
                    args[target.Position] = result;
                }
                unsetParameters.Remove(target);
            }

            foreach (ParameterInfo p in unsetParameters.Where(p => !p.HasDefaultValue))
            {
                errors.Add($"No value was provided for argument {p.Name} with no default value");
            }
        }

        private string ReadValue(TerminalCommandParser.ValueContext context)
        {
            string value = context.GetText();
            if (context.LITERAL() != null)
            {
                value = Regex.Unescape(value[1..^1]);
            }
            return value;
        }

        private bool IsMissingToken(ITerminalNode token)
        {
            return token == null || token.SourceInterval.a < 0 && token.SourceInterval.b < 0;
        }

        private Interval RangeOf(ParserRuleContext context)
        {
            return new Interval(context.Start.StartIndex, context.Stop.StopIndex);
        }
    }
}
