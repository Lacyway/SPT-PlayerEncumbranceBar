using BepInEx.Configuration;
using System.Collections.Generic;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QUICKMOVETOCONTAINER
namespace WeightBar
{
    internal class Settings
    {
        public const string GeneralSectionTitle = "1. General";

        public static ConfigFile Config;

        public static ConfigEntry<bool> DisplayText;

        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(DisplayText = Config.Bind(
                GeneralSectionTitle,
                "Display Breakpoint Text",
                false,
                new ConfigDescription(
                    "If text for each tick mark breakpoint should be displayed",
                    null,
                    new ConfigurationManagerAttributes { })));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                ConfigurationManagerAttributes attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
