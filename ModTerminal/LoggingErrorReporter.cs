using Antlr4.Runtime;
using System.Collections.Generic;
using System.IO;

namespace ModTerminal
{
    internal class LoggingErrorReporter : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
    {
        private List<string> collectedSyntaxErrors = new();

        public IReadOnlyList<string> CollectedSyntaxErrors => collectedSyntaxErrors.AsReadOnly();

        public void SyntaxError(TextWriter output, 
            IRecognizer recognizer, IToken offendingSymbol, 
            int line, int charPositionInLine, string msg, 
            RecognitionException e
        )
        {
            Collect(msg, charPositionInLine);
        }

        public void SyntaxError(TextWriter output,
            IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg,
            RecognitionException e
        )
        {
            Collect(msg, charPositionInLine);
        }

        private void Collect(string msg, int charPositionInLine)
        {
            string error = $"{msg} (position {charPositionInLine})";
            ModTerminalMod.Instance.LogWarn(error);
            collectedSyntaxErrors.Add(error);
        }
    }
}
