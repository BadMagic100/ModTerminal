using System.Linq;

namespace ModTerminal
{
    internal static class BuiltInCommands
    {
        private static void UpdateCharmsEffects()
        {
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
            PlayMakerFSM.BroadcastEvent("CHARM EQUIP CHECK");
        }

        public static string GiveCharm(int id)
        {
            if (id < 1)
            {
                return "Invalid charm ID";
            }
            PlayerData.instance.SetBool(nameof(PlayerData.hasCharm), true);
            PlayerData.instance.SetBool("gotCharm_" + id, true);
            UpdateCharmsEffects();
            return $"Successfully given charm {id}";
        }

        public static string GiveCharms(params int[] ids)
        {
            return string.Join(", ", ids.Select(GiveCharm));
        }
    }
}
