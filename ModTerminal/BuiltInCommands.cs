using System.Linq;

namespace ModTerminal
{
    public enum PDType
    {
        Int, Float, Bool, String, Vector
    }

    internal static class BuiltInCommands
    {

        private static void UpdateCharmsEffects()
        {
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
            PlayMakerFSM.BroadcastEvent("CHARM EQUIP CHECK");
        }

        [HelpDocumentation("Gives the player a single charm by its internal ID.")]
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

        [HelpDocumentation("Gives the player multiple charms by their internal IDs.")]
        public static string GiveCharms(params int[] ids)
        {
            return string.Join(", ", ids.Select(GiveCharm));
        }

        [HelpDocumentation("Gives the player the specified amount of essence.")]
        public static void GiveEssence(int amount)
        {
            PlayerData.instance.IntAdd(nameof(PlayerData.dreamOrbs), amount);
        }

        [HelpDocumentation("Gives the player the specified amount of geo.")]
        public static void GiveGeo(int amount)
        {
            if (amount < 0)
            {
                HeroController.instance.TakeGeo(-amount);
            }
            else
            {
                HeroController.instance.AddGeo(amount);
            }
        }

        [HelpDocumentation("Displays the PlayerData variable of the given type and name.")]
        public static string GetPlayerData(PDType type, string name)
        {
            switch (type)
            {
                case PDType.Int:
                    return PlayerData.instance.GetInt(name).ToString();
                case PDType.Float:
                    return PlayerData.instance.GetFloat(name).ToString();
                case PDType.Bool:
                    return PlayerData.instance.GetBool(name).ToString();
                case PDType.String:
                    return PlayerData.instance.GetString(name);
                case PDType.Vector:
                    return PlayerData.instance.GetVector3(name).ToString();
                default:
                    return "Invalid type";
            }
        }

        [HelpDocumentation(
            "Sets the PlayerData variable of the given name and type. If multiple type values are"
            + " set, the first one will be used in the order displayed here.")]
        public static string SetPlayerData(string name, int? @int = null, bool? @bool = null, string? @string = null, float? @float = null)
        {
            if (@int != null)
            {
                PlayerData.instance.SetInt(name, @int.Value);
            }
            else if (@bool != null)
            {
                PlayerData.instance.SetBool(name, @bool.Value);
            }
            else if (@string != null)
            {
                PlayerData.instance.SetString(name, @string);
            }
            else if (@float != null)
            {
                PlayerData.instance.SetFloat(name, @float.Value);
            }
            else
            {
                return "No value provided";
            }
            return $"Successfully set {name}";
        }

        [HelpDocumentation("Sets the PlayerData vector of the given name.")]
        public static string SetPlayerDataVector3(string name, float x, float y, float z = 0f)
        {
            PlayerData.instance.SetVector3(name, new UnityEngine.Vector3(x, y, z));
            return $"Successfully set {name}";
        }
    }
}
