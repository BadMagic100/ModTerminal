using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using ModTerminal.Commands;
using ModTerminal.Processing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

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

        private Command? runningCommand;

        private bool? heldHotkeySetting;
        private bool? heldLockKeybind;

        private readonly CommandBuffer commandBuffer = new();
        private StreamWriter? fileLogger;

        private Dictionary<string, string> aliasTable = new();

        private TerminalUI()
        {
            layout = new(true, "ModTerminal UI");
            layout.VisibilityCondition = () => isActive;

            layout.ListenForHotkey(KeyCode.C, OnInterruptHotkey, ModifierKeys.Ctrl);

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
                CursorToEnd();
            }
        }

        private void OnArrowDown()
        {
            if (isActive)
            {
                input.Text = commandBuffer.GoDown();
                CursorToEnd();
            }
        }

        private void CursorToEnd()
        {
            InputField _if = input.GameObject.GetComponent<InputField>();
            _if.StartCoroutine(MoveCursor());

            IEnumerator MoveCursor()
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                _if.caretPosition = input.Text.Length;
                _if.ForceLabelUpdate();
            }
        }

        private void OnInterruptHotkey()
        {
            if (runningCommand != null && runningCommand.Context != null && !runningCommand.Context.IsFinished)
            {
                runningCommand.Context.RequestCancellation();
            }
        }


        private void OnCommand(TextInput sender, string text)
        {
            sender.Text = "";
            if (!string.IsNullOrWhiteSpace(text.Trim()))
            {
                commandBuffer.Add(text);
                Write("> " + text.Trim());

                LoggingErrorReporter errorReporter = new();
                CommandMatcher matcher = new(ModTerminalMod.Instance.PrimaryCommandTable);

                KeyValuePair<string, string> alias = aliasTable.FirstOrDefault(a => text.TrimStart().StartsWith(a.Key));
                string transformedText = text;
                if (alias.Key != null && alias.Value != null)
                {
                    transformedText = Regex.Replace(text, @"^\s*" + Regex.Escape(alias.Key), alias.Value);
                }

                AntlrInputStream str = new(transformedText);
                TerminalCommandLexer lexer = new(str);
                lexer.AddErrorListener(errorReporter);
                CommonTokenStream tokens = new(lexer);
                TerminalCommandParser parser = new(tokens);
                parser.AddErrorListener(errorReporter);
                ParseTreeWalker walker = new();
                walker.Walk(matcher, parser.command());

                // if there were any syntactic or semantic errors, we won't be able to invoke
                if (errorReporter.CollectedSyntaxErrors.Any())
                {
                    Write("Syntax error:");
                    foreach (string error in errorReporter.CollectedSyntaxErrors)
                    {
                        Write("    " + error);
                    }
                    sender.SelectAndActivate();
                }
                else if (!matcher.FoundCommand 
                    || matcher.CollectedSemanticErrors.Any())
                {
                    if (matcher.InvocationType == InvocationType.CommandGroup && !matcher.CollectedSemanticErrors.Any())
                    {
                        CommandTable group = matcher.Table;
                        Write(group.GeneralHelp);
                        IEnumerable<string> subcommands = group.RegisteredCommandAndGroupNames
                            .OrderBy(x => x, new ValuesLastComparer<string>("help", "listcommands"))
                            .ThenBy(x => x);
                        Write($"Available commands: {string.Join(", ", subcommands)}");
                    }
                    else if (matcher.InvocationType == InvocationType.Command && !matcher.FoundCommand)
                    {
                        Write($"No such command '{matcher.FullRequestedCommandName}'. Use the 'help' and 'listcommands' commands to get started.");
                    }
                    else
                    {
                        foreach (string error in matcher.CollectedSemanticErrors)
                        {
                            Write("  - " + error);
                        }
                    }
                    sender.SelectAndActivate();
                }
                else
                {
                    try
                    {
                        void Unhook()
                        {
                            matcher.Command.ProgressReported -= Write;
                            matcher.Command.Finished -= Unhook;
                            sender.SelectAndActivate();
                            runningCommand = null;
                        }

                        runningCommand = matcher.Command;
                        runningCommand.ProgressReported += Write;
                        runningCommand.Finished += Unhook;
                        sender.Deactivate();
                        string? result = runningCommand.Execute(matcher.CollectedParameters);
                        if (result != null)
                        {
                            Write(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModTerminalMod.Instance.LogError(ex);
                        Write($"Unexpected error executing {matcher.FullRequestedCommandName}");
                        sender.SelectAndActivate();
                    }
                }
            }
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
            fileLogger?.WriteLine(content);

            string next = output.Text + '\n' + content.TrimEnd();
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
            if (isEnabled && runningCommand == null)
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

        [HelpDocumentation("Adds or overwrites a Linux-style command alias.")]
        public string SetAlias(
            [HelpDocumentation("The new alias. Must be a single word.")] string alias,
            [HelpDocumentation("The command fragment to replace the alias with.")] string commandFragment
        )
        {
            if (" \r\n\t".Any(alias.Contains))
            {
                return "Aliases may not contain any whitespace.";
            }

            aliasTable[alias] = commandFragment;
            return $"alias {alias}={commandFragment}";
        }

        [HelpDocumentation("Starts logging terminal content to a file.")]
        public string? StartLogging(
            Command self,
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
                self.Context?.Report($"Starting logging to {path}");

                FileStream fs = new(path, mode, FileAccess.Write, FileShare.ReadWrite);
                fileLogger = new(fs, Encoding.UTF8) { AutoFlush = true };
                return null;
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
