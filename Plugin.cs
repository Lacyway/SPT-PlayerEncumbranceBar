using BepInEx;
using BepInEx.Logging;
using DrakiaXYZ.VersionChecker;
using WeightBar.Patches;
using System;
using System.IO;
using System.Reflection;
using EFT.UI.Health;

namespace WeightBar
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.WeightBar", "WeightBar", BuildInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public WeightBarComponent weightBarComponent;

        internal void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            Instance = this;
            DontDestroyOnLoad(this);

            // patches
            new HealthParametersShowPatch().Enable();
        }

        public void TryAttachToHealthParametersPanel(HealthParametersPanel inventoryScreen)
        {
            if (weightBarComponent != null)
            {
                return;
            }

            weightBarComponent = WeightBarComponent.AttachToHealthParametersPanel(inventoryScreen);
        }
    }
}
