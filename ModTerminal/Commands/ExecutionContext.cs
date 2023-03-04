using System;
using System.Diagnostics.CodeAnalysis;

namespace ModTerminal.Commands
{
    public class ExecutionContext : IProgress<string>
    {
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        [MemberNotNullWhen(true, nameof(EndTime))]
        public bool IsFinished { get; private set; }

        public event Action<string>? ProgressChanged;

        public event Action? Finished;

        public event Action? CancellationRequested;

        public ExecutionContext()
        {
            StartTime = DateTime.UtcNow;
        }

        public void Finish()
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("Cannot finish an already-finished execution");
            }
            EndTime = DateTime.UtcNow;
            IsFinished = true;
            Finished?.Invoke();
        }

        public void Report(string value)
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("Cannot report progress on a finished execution");
            }
            ProgressChanged?.Invoke(value);
        }

        public void RequestCancellation()
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("Cannot cancel a completed execution context");
            }
            CancellationRequested?.Invoke();
        }
    }
}
