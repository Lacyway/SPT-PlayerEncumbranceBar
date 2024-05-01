﻿using BepInEx;
using BepInEx.Logging;
using DrakiaXYZ.VersionChecker;
using WeightBar.Patches;
using System;
using System.IO;
using System.Reflection;
using EFT.UI.Health;
using EFT.HealthSystem;

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

            Settings.Init(Config);
            Config.SettingChanged += (_, _) => weightBarComponent.OnSettingChanged();

            Instance = this;
            DontDestroyOnLoad(this);

            // patches
            new HealthParametersShowPatch().Enable();
        }

        public void TryAttachToHealthParametersPanel(HealthParametersPanel healthParametersScreen, HealthParameterPanel weightPanel, IHealthController healthController)
        {
            if (weightBarComponent != null)
            {
                return;
            }

            weightBarComponent = WeightBarComponent.AttachToHealthParametersPanel(healthParametersScreen, weightPanel, healthController);
        }
    }
}
