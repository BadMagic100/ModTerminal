using ModTerminal.Processing;
using System;
using System.Threading;

namespace ModTerminal.Commands
{
    public class AsyncCommand : Command
    {
        public AsyncCommand(string commandName, Delegate exec) : base(commandName, exec)
        {
            if (Method.ReturnType != typeof(void))
            {
                throw new ArgumentException("Async commands should be of type void, and report progress"
                    + " and completion through the execution context if needed.", nameof(exec));
            }
        }

        internal override string? Execute(object?[] args)
        {
            ExecutionContext = new ExecutionContext();
            ExecutionContext.ProgressChanged += JoinThreadAndReport;
            ExecutionContext.Finished += JoinThreadAndFinish;

            Thread worker = new(() => Delegate.DynamicInvoke(args));
            worker.Start();
            return null;
        }

        private void JoinThreadAndReport(string str)
        {
            Dispatcher.BeginInvoke(() => ReportProgress(str));
        }

        private void JoinThreadAndFinish()
        {
            Dispatcher.BeginInvoke(Finish);
        }
    }
}
