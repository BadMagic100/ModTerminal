using ModTerminal.Processing;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModTerminal.Commands
{
    public class Command
    {
        private static readonly Regex namePattern = new("^[a-z]+$");

        public readonly string Name;

        public event Action<string>? ProgressReported;
        public event Action? Finished;

        protected readonly Delegate Delegate;
        protected readonly bool HasResult;
        public readonly MethodInfo Method;

        public ExecutionContext? Context { get; protected set; }

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
            foreach (ParameterInfo param in parameters)
            {
                if (typeof(Command) == param.ParameterType)
                {
                    if (param.Position == 0)
                    {
                        continue;
                    }
                    throw new ArgumentException("Command parameters are only legal in the first position.");
                }

                if (param.ParameterType.IsArray && param.Position != parameters.Length - 1)
                {
                    throw new ArgumentException("Array parameters are only legal in the final position (e.g. params arrays)",
                        nameof(exec));
                }

                Type targetType = param.ParameterType.ConversionType();
                if (!targetType.IsEnum && !param.IsConvertible())
                {
                    throw new ArgumentException($"{param.Name} is not a convertible type", nameof(exec));
                }
            }
        }

        internal virtual string? Execute(object?[] args)
        {
            Context = new ExecutionContext();
            Context.ProgressChanged += ReportProgress;
            object result = Delegate.DynamicInvoke(args);
            Context.Finish();

            Finish();

            if (HasResult)
            {
                return (string)result;
            }
            return null;
        }

        protected void ReportProgress(string message)
        {
            ProgressReported?.Invoke(message);
        }

        protected void Finish()
        {
            if (Context == null || !Context.IsFinished)
            {
                throw new InvalidOperationException("Cannot finish a command before its execution context has finished!");
            }

            Finished?.Invoke();
            Context = null;
        }
    }
}
