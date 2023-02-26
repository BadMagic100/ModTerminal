using Nito.Collections;

namespace ModTerminal.Processing
{
    internal class CommandBuffer
    {
        private const int COMMAND_LIMIT = 500;
        private readonly Deque<string> buf = new();
        private int index = -1;
        private string heldValue = "";

        public bool IsAtBottom => index == -1;
        public bool IsAtTop => index == buf.Count - 1;

        public void Hold(string value)
        {
            heldValue = value;
        }

        public void ResetNavigation()
        {
            heldValue = "";
            index = -1;
        }

        public void Add(string command)
        {
            buf.AddToFront(command);
            heldValue = "";
            if (buf.Count > COMMAND_LIMIT)
            {
                buf.RemoveFromBack();
            }
            index = -1;
        }

        public string GoUp()
        {
            if (!IsAtTop)
            {
                index++;
            }
            return buf[index];
        }

        public string GoDown()
        {
            if (!IsAtBottom)
            {
                index--;
            }
            return IsAtBottom ? heldValue : buf[index];
        }
    }
}
