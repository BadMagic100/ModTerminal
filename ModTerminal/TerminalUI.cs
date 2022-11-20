using MagicUI.Core;
using MagicUI.Elements;
using System.Linq;

namespace ModTerminal
{
    internal class TerminalUI
    {
        private const int MAX_LINES = 10;

        private static TerminalUI? _instance = null;
        public static TerminalUI Instance => _instance ??= new();

        private readonly LayoutRoot layout;
        private readonly TextInput input;
        private readonly TextObject output;
        private bool isActive = false;
        private bool isEnabled = false;

        private bool? heldHotkeySetting;

        private TerminalUI()
        {
            layout = new(true, "ModTerminal UI");
            layout.VisibilityCondition = () => isActive;

            input = new(layout, "ModTerminal Command Entry")
            {
                Font = UI.Perpetua,
                FontSize = 20,
                MinWidth = UI.Screen.width / 3
            };
            input.TextEditFinished += OnCommand;

            output = new(layout, "ModTerminal Console")
            {
                Font = UI.Perpetua,
                FontSize = 20,
                MaxWidth = UI.Screen.width / 3
            };

            new StackLayout(layout, "ModTerminal Element Stack")
            {
                Padding = new(10, 50),
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Children =
                {
                    output,
                    input
                }
            };
        }

        private void OnCommand(TextInput sender, string text)
        {
            sender.Text = "";
            text = text.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                Write("> " + text);
                // todo: do commands
            }
        }

        public void Clear()
        {
            input.Text = "";
            output.Text = "";
        }

        private void Write(string content)
        {
            string next = output.Text + '\n' + content.Trim();
            string[] lines = next.Split('\n');
            int diff = lines.Length - MAX_LINES;
            if (diff > 0)
            {
                output.Text = string.Join("\n", lines.Skip(diff));
            }
            else
            {
                output.Text = next;
            }
        }

        public void Show()
        {
            if (isEnabled)
            {
                isActive = true;
                layout.ForceInteractivityRefresh();
                input.GetSelectable().Select();
                heldHotkeySetting = BenchwarpInterop.Hotkeys;
                BenchwarpInterop.Hotkeys = false;
            }
        }

        public void Hide()
        {
            if (isEnabled)
            {
                isActive = false;
                if (heldHotkeySetting != null)
                {
                    BenchwarpInterop.Hotkeys = heldHotkeySetting.Value;
                    heldHotkeySetting = null;
                }
            }
        }

        public void Toggle()
        {
            if (isEnabled)
            {
                if (isActive)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
        }

        public void Enable()
        {
            isEnabled = true;
        }

        public void Disable()
        {
            Hide();
            isEnabled = false;
        }
    }
}
