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
            if (PlayerData.instance.GetBool("gotCharm_" + id))
            {
                return $"Already obtained charm {id}";
            }

            PlayerData.instance.SetBool(nameof(PlayerData.hasCharm), true);
            PlayerData.instance.IncrementInt(nameof(PlayerData.charmsOwned));
            PlayerData.instance.SetBool("gotCharm_" + id, true);
            PlayerData.instance.SetBool("newCharm_" + id, true);
            UpdateCharmsEffects();
            return $"Successfully given charm {id}";
        }

        public static string GiveCharms(params int[] ids)
        {
            return string.Join(", ", ids.Select(GiveCharm));
        }

        public static void GiveEssence(int amount)
        {
            PlayerData.instance.IntAdd(nameof(PlayerData.dreamOrbs), amount);
        }

        public static void GiveGeo(int amount)
        {
            HeroController.instance.AddGeo(amount);
        }
    }
}
