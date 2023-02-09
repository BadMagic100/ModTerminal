using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModTerminal
{
    internal class TerminalUI
    {
        private const int MAX_LINES = 20;
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

        private CommandBuffer commandBuffer = new();
        private StreamWriter? fileLogger;

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
            ArrowKeyWatcher arrowWatcher = layout.Canvas.AddComponent<ArrowKeyWatcher>();
            arrowWatcher.selectableToWatch = input.GetSelectable();
            arrowWatcher.OnUp += OnArrowUp;
            arrowWatcher.OnDown += OnArrowDown;

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

        private void OnArrowUp()
        {
            if (isActive)
            {
                if (commandBuffer.IsAtBottom)
                {
                    commandBuffer.Hold(input.Text);
                }
                input.Text = commandBuffer.GoUp();
            }
        }

        private void OnArrowDown()
        {
            if (isActive)
            {
                input.Text = commandBuffer.GoDown();
            }
        }

        private void OnCommand(TextInput sender, string text)
        {
            sender.Text = "";
            text = text.Trim();
            commandBuffer.Add(text);
            if (!string.IsNullOrWhiteSpace(text))
            {
                Write("> " + text);
                CommandInvocation invocation = CommandParser.ParseCommand(text);
                ScopedCommandInvocation scopedInvocation = CommandParser.ScopeCommandInvocation(
                    ModTerminalMod.Instance.PrimaryCommandTable, invocation);
                Command? command = scopedInvocation.OwningTable.GetCommand(scopedInvocation.FinalInvocation.Name);
                if (command == null)
                {
                    // check if there a command group with the correct name, display help and subcommands
                    CommandTable? group = scopedInvocation.OwningTable.GetGroup(scopedInvocation.FinalInvocation.Name);
                    if (group == null)
                    {
                        // no matching command or group
                        Write($"Error: {scopedInvocation.FullName} is not a known command");
                    }
                    else
                    {
                        // no matching command, but matching group
                        Write(group.GeneralHelp);
                        Write($"Available commands: {string.Join(", ", group.RegisteredCommandAndGroupNames.OrderBy(x => x))}");
                    }
                }
                else
                {
                    try
                    {
                        string? result = command.Execute(scopedInvocation.FinalInvocation.Slots);
                        if (result != null)
                        {
                            Write(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModTerminalMod.Instance.LogError(ex);
                        Write($"Unexpected error executing {scopedInvocation.FullName}");
                    }
                }
            }
            sender.SelectAndActivate();
        }

        [HelpDocumentation("Clears the terminal.")]
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
            if (fileLogger != null)
            {
                fileLogger.WriteLine(content);
            }

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
                input.SelectAndActivate();
                heldHotkeySetting = BenchwarpInterop.Hotkeys;
                BenchwarpInterop.Hotkeys = false;
                heldLockKeybind = DebugMod.DebugMod.KeyBindLock;
                DebugMod.DebugMod.KeyBindLock = true;
                SetEnabledHeroActions(false);
            }
        }

        [HelpDocumentation("Closes the terminal.")]
        public void Hide()
        {
            if (isEnabled)
            {
                ClearInput();
                commandBuffer.ResetNavigation();
                input.Deactivate();
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

        [HelpDocumentation("Starts logging terminal content to a file.")]
        public string StartLogging(
            string fileName, 
            bool append = false
        )
        {
            if (fileLogger == null)
            {
                string path = Path.Combine(Application.persistentDataPath, fileName);
                string dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);
                FileMode mode = append ? FileMode.Append : FileMode.Create;
                FileStream fs = new(path, mode, FileAccess.Write, FileShare.ReadWrite);
                fileLogger = new(fs, Encoding.UTF8) { AutoFlush = true };
                return $"Started logging to {path}";
            }
            return "Logging is already in progress.";
        }

        [HelpDocumentation("Stops logging terminal content to a file.")]
        public string StopLogging()
        {
            if (fileLogger != null)
            {
                fileLogger.Close();
                fileLogger = null;
                return "Logging was stopped successfully.";
            }
            return "Logging was never started.";
        }
    }
}
