using Aki.Reflection.Patching;
using EFT.UI.Health;
using HarmonyLib;
using System.Reflection;

namespace WeightBar.Patches
{
    internal class HealthParametersShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HealthParametersPanel), nameof(HealthParametersPanel.Show));
        }

        [PatchPostfix]
        public static void PatchPostfix(HealthParametersPanel __instance)
        {
            Plugin.Instance.TryAttachToHealthParametersPanel(__instance);
        }
    }
}
