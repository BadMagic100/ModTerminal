using ModTerminal.Processing;
using System;
using System.Threading;

namespace ModTerminal.Commands
{
    public class AsyncCommand : Command
    {
        private Thread? workerThread;

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
            Context = new ExecutionContext();
            Context.ProgressChanged += JoinThreadAndReport;
            Context.CancellationRequested += JoinThreadAndCancel;
            Context.Finished += JoinThreadAndFinish;

            workerThread = new(() => Delegate.DynamicInvoke(args));
            workerThread.Start();
            return null;
        }

        private void JoinThreadAndReport(string str)
        {
            Dispatcher.BeginInvoke(() => ReportProgress(str));
        }

        private void JoinThreadAndCancel()
        {
            Dispatcher.BeginInvoke(() =>
            {
                workerThread?.Abort();
                Context?.Report("Operation cancelled.");
                Context?.Finish();
            });
        }

        private void JoinThreadAndFinish()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Finish();
                workerThread = null;
            });
        }
    }
}
