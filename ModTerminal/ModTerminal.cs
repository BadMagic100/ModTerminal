using DebugMod;
using Modding;
using System;
using System.Collections;

namespace ModTerminal
{
    internal class DebugHooks
    {
        [BindableMethod(name = "Toggle Terminal", category = "Misc")]
        public static void ToggleTerminal()
        {
            TerminalUI.Instance.Toggle();
        }
    }

    public class ModTerminalMod : Mod
    {
        private static ModTerminalMod? _instance;

        internal static ModTerminalMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(ModTerminalMod)} was never constructed");
                }
                return _instance;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public ModTerminalMod() : base("ModTerminal")
        {
            _instance = this;
        }

        public override void Initialize()
        {
            Log("Initializing");

            On.HeroController.Awake += OnEnteredFile;
            On.QuitToMenu.Start += OnExitedFile;
            On.GameCompletionScreen.Start += OnGotEnding;

            DebugMod.DebugMod.AddToKeyBindList(typeof(DebugHooks));

            CommandTable.RegisterCommand(new Command("help", CommandTable.HelpCommand));
            CommandTable.RegisterCommand(new Command("givecharm", BuiltInCommands.GiveCharm));
            CommandTable.RegisterCommand(new Command("givecharms", BuiltInCommands.GiveCharms));
            CommandTable.RegisterCommand(new Command("givegeo", BuiltInCommands.GiveGeo));
            CommandTable.RegisterCommand(new Command("giveessence", BuiltInCommands.GiveEssence));

            Log("Initialized");
        }

        private void OnEnteredFile(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);
            TerminalUI.Instance.Enable();
        }

        private IEnumerator OnExitedFile(On.QuitToMenu.orig_Start orig, QuitToMenu self)
        {
            IEnumerator temp = orig(self);
            TerminalUI.Instance.Hide();
            TerminalUI.Instance.Clear();
            TerminalUI.Instance.Disable();
            return temp;
        }

        private void OnGotEnding(On.GameCompletionScreen.orig_Start orig, GameCompletionScreen self)
        {
            TerminalUI.Instance.Hide();
            TerminalUI.Instance.Clear();
            TerminalUI.Instance.Disable();
            orig(self);
        }
    }
}
