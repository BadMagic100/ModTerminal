using Modding;
using System;

namespace ModTerminal
{
    internal static class BenchwarpInterop
    {
        private static bool UnsafeHotkeys
        {
            get => Benchwarp.Benchwarp.GS.EnableHotkeys;
            set => Benchwarp.Benchwarp.GS.EnableHotkeys = value;
        }

        public static bool Hotkeys
        {
            get => CheckBenchwarpAndThen(() => UnsafeHotkeys);
            set => CheckBenchwarpAndThen(() => UnsafeHotkeys = value);
        }

        private static void CheckBenchwarpAndThen(Action a)
        {
            if (ModHooks.GetMod("Benchwarp") is Mod)
            {
                a();
            }
        }

        private static T? CheckBenchwarpAndThen<T>(Func<T> f, T? defaultValue = default)
        {
            if (ModHooks.GetMod("Benchwarp") is Mod)
            {
                return f();
            }
            return defaultValue;
        }
    }
}
