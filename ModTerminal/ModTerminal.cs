using DebugMod;
using Modding;
using ModTerminal.Commands;
using ModTerminal.Processing;
using System;
using System.Collections;
using System.Threading;

namespace ModTerminal
{
    internal class DebugHooks
    {
        [BindableMethod(name = "Toggle Terminal", category = "Misc", allowLock = false)]
        public static void ToggleTerminal()
        {
            TerminalUI.Instance.Toggle();
        }
    }

    internal class Test
    {
        public static void DoCommand(Command self)
        {
            if (self.ExecutionContext == null)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(5000);
                self.ExecutionContext.Report("Working...");
            }
            self.ExecutionContext.Report("Done");
            self.ExecutionContext.Finish();
        }
    }

    public class ModTerminalMod : Mod
    {
        private static ModTerminalMod? _instance;

        public static ModTerminalMod Instance
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

        public readonly CommandTable PrimaryCommandTable = new(
            "Use 'listcommands' to see available commands, and 'help <command>' to see help for a given command. "
                    + "Commands may take any number of parameters as specified by their help documentation. Command parameters "
                    + "can be specified in order, or by specifying the parameters in 'name=value' syntax. Ordered parameters cannot "
                    + "be used after a named parameter has been used. Some parameters take a variable amount of parameters. For "
                    + "these commands, the last parameter is listed as an array in the help documentation and you can provide any "
                    + "number of values, including zero, by adding additional ordered parameters. Named parameters cannot be used "
                    + "for these commands.");

        public ModTerminalMod() : base("ModTerminal")
        {
            _instance = this;
        }

        public override void Initialize()
        {
            Log("Initializing");

            Dispatcher.Setup();

            // if debug doesn't exist, let this die and don't hook anything else
            DebugMod.DebugMod.AddToKeyBindList(typeof(DebugHooks));

            On.HeroController.Awake += OnEnteredFile;
            On.QuitToMenu.Start += OnExitedFile;
            On.GameCompletionScreen.Start += OnGotEnding;

            PrimaryCommandTable.RegisterCommand(new AsyncCommand("test", Test.DoCommand));

            PrimaryCommandTable.RegisterCommand(new Command("clear", TerminalUI.Instance.Clear));
            PrimaryCommandTable.RegisterCommand(new Command("exit", TerminalUI.Instance.Hide));
            PrimaryCommandTable.RegisterCommand(new Command("startlog", TerminalUI.Instance.StartLogging));
            PrimaryCommandTable.RegisterCommand(new Command("stoplog", TerminalUI.Instance.StopLogging));
            PrimaryCommandTable.RegisterCommand(new Command("givecharm", BuiltInCommands.GiveCharm));
            PrimaryCommandTable.RegisterCommand(new Command("givecharms", BuiltInCommands.GiveCharms));
            PrimaryCommandTable.RegisterCommand(new Command("givegeo", BuiltInCommands.GiveGeo));
            PrimaryCommandTable.RegisterCommand(new Command("giverelic", BuiltInCommands.GiveRelic));
            PrimaryCommandTable.RegisterCommand(new Command("giveessence", BuiltInCommands.GiveEssence));
            PrimaryCommandTable.RegisterCommand(new Command("getpd", BuiltInCommands.GetPlayerData));
            PrimaryCommandTable.RegisterCommand(new Command("setpd", BuiltInCommands.SetPlayerData));
            PrimaryCommandTable.RegisterCommand(new Command("setpdvector", BuiltInCommands.SetPlayerDataVector3));

            CommandTable giveCommands = new("Commands which give the player items or resources.");
            giveCommands.RegisterCommand(new Command("charm", BuiltInCommands.GiveCharm));
            giveCommands.RegisterCommand(new Command("charms", BuiltInCommands.GiveCharms));
            giveCommands.RegisterCommand(new Command("geo", BuiltInCommands.GiveGeo));
            giveCommands.RegisterCommand(new Command("relic", BuiltInCommands.GiveRelic));
            giveCommands.RegisterCommand(new Command("essence", BuiltInCommands.GiveEssence));
            PrimaryCommandTable.RegisterGroup("give", giveCommands);

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
            TerminalUI.Instance.Clear();
            TerminalUI.Instance.Hide();
            TerminalUI.Instance.Disable();
            return temp;
        }

        private void OnGotEnding(On.GameCompletionScreen.orig_Start orig, GameCompletionScreen self)
        {
            TerminalUI.Instance.Clear();
            TerminalUI.Instance.Hide();
            TerminalUI.Instance.Disable();
            orig(self);
        }
    }
}
