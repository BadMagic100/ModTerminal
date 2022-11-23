using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Linq;
using UnityEngine.UI;

namespace ModTerminal
{
    internal class TerminalUI
    {
        private const int MAX_LINES = 10;
        private static readonly TextureLoader textureLoader = new(typeof(TerminalUI).Assembly, "ModTerminal.Resources");

        private static void SetEnabledHeroActions(bool enabled)
        {
            var inputHandler = InputHandler.Instance;
            if (inputHandler == null)
            {
                return;
            }

            var heroActions = inputHandler.inputActions;
            if (heroActions == null)
            {
                return;
            }

            // Disable all input actions for the hero, except for the pause actions
            // which we use to listen for closing the chat again
            heroActions.left.Enabled = enabled;
            heroActions.right.Enabled = enabled;
            heroActions.up.Enabled = enabled;
            heroActions.down.Enabled = enabled;
            heroActions.menuSubmit.Enabled = enabled;
            heroActions.menuCancel.Enabled = enabled;
            heroActions.rs_up.Enabled = enabled;
            heroActions.rs_down.Enabled = enabled;
            heroActions.rs_left.Enabled = enabled;
            heroActions.rs_right.Enabled = enabled;
            heroActions.jump.Enabled = enabled;
            heroActions.evade.Enabled = enabled;
            heroActions.dash.Enabled = enabled;
            heroActions.superDash.Enabled = enabled;
            heroActions.dreamNail.Enabled = enabled;
            heroActions.attack.Enabled = enabled;
            heroActions.cast.Enabled = enabled;
            heroActions.focus.Enabled = enabled;
            heroActions.quickMap.Enabled = enabled;
            heroActions.quickCast.Enabled = enabled;
            heroActions.textSpeedup.Enabled = enabled;
            heroActions.skipCutscene.Enabled = enabled;
            heroActions.openInventory.Enabled = enabled;
            heroActions.paneRight.Enabled = enabled;
            heroActions.paneLeft.Enabled = enabled;
        }

        private static TerminalUI? _instance = null;
        public static TerminalUI Instance => _instance ??= new();

        private readonly LayoutRoot layout;
        private readonly TextInput input;
        private readonly TextObject output;
        private bool isActive = false;
        private bool isEnabled = false;

        private bool? heldHotkeySetting;
        private bool? heldLockKeybind;

        private TerminalUI()
        {
            layout = new(true, "ModTerminal UI");
            layout.VisibilityCondition = () => isActive;

            Panel p = new(layout, textureLoader.GetTexture("Background.png").ToSprite(), "ModTerminal Background")
            {
                Borders = new Padding(5),
                Padding = new Padding(10, 50),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            };

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

            p.Child = new StackLayout(layout, "ModTerminal Element Stack")
            {
                Spacing = 5,
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
                CommandInvocation invocation = CommandParser.ParseCommand(text);
                Command? command = CommandTable.GetCommand(invocation.Name);
                if (command == null)
                {
                    Write($"Error: {invocation.Name} is not a known command");
                }
                else
                {
                    try
                    {
                        string? result = command.Execute(invocation.Slots);
                        if (result != null)
                        {
                            Write(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModTerminalMod.Instance.LogError(ex);
                        Write($"Unexpected error executing {invocation.Name}");
                    }
                }
            }
            sender.GameObject.GetComponent<InputField>().ActivateInputField();
            sender.GetSelectable().Select();
        }

        public void Clear()
        {
            input.Text = "";
            output.Text = "";
        }

        public void ClearInput()
        {
            input.Text = "";
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
                heldLockKeybind = DebugMod.DebugMod.KeyBindLock;
                DebugMod.DebugMod.KeyBindLock = true;
                SetEnabledHeroActions(false);
            }
        }

        public void Hide()
        {
            if (isEnabled)
            {
                ClearInput();
                isActive = false;
                if (heldHotkeySetting != null)
                {
                    BenchwarpInterop.Hotkeys = heldHotkeySetting.Value;
                    heldHotkeySetting = null;
                }
                if (heldLockKeybind != null)
                {
                    DebugMod.DebugMod.KeyBindLock = heldLockKeybind.Value;
                    heldLockKeybind = null;
                }
                SetEnabledHeroActions(true);
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
