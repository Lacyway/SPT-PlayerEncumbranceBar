using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace PlayerEncumbranceBar.Utils
{
    // this static helper class solely exists to try to remove GClassXXXX and assorted other references
    public static class GameUtils
    {
        // reflection
        private static Type _profileInterface = typeof(ISession).GetInterfaces().First(i =>
            {
                var properties = i.GetProperties();
                return properties.Length == 2 &&
                       properties.Any(p => p.Name == "Profile");
            });
        private static PropertyInfo _sessionProfileProperty = AccessTools.Property(_profileInterface, "Profile");
        // private static FieldInfo _skillManagerStrengthBuffEliteField = AccessTools.Field(typeof(SkillManager), "StrengthBuffElite");
        private static FieldInfo _inventoryTotalWeightEliteSkillField = AccessTools.Field(typeof(Inventory), "TotalWeightEliteSkill");
        private static FieldInfo _inventoryTotalWeightField = AccessTools.Field(typeof(Inventory), "TotalWeight");
        private static PropertyInfo _floatWrapperValueProperty = AccessTools.Property(_inventoryTotalWeightField.FieldType, "Value");

        // properties
        public static ISession Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
        public static Profile SessionProfile => _sessionProfileProperty.GetValue(Session) as Profile;
        public static Inventory Inventory => SessionProfile.Inventory;
        public static SkillManager Skills => SessionProfile.Skills;

        public static float GetPlayerCurrentWeight()
        {
            var profile = SessionProfile;
            var inventory = profile.Inventory;
            var skills = profile.Skills;

            var totalWeightEliteSkillWrapper = _inventoryTotalWeightEliteSkillField.GetValue(inventory);
            var totalWeightEliteSkill = (float)_floatWrapperValueProperty.GetValue(totalWeightEliteSkillWrapper);

            var totalWeightWrapper = _inventoryTotalWeightField.GetValue(inventory);
            var totalWeight = (float)_floatWrapperValueProperty.GetValue(totalWeightWrapper);

            return skills.StrengthBuffElite ? totalWeightEliteSkill : totalWeight;
        }

        public static RectTransform GetRectTransform(this GameObject gameObject)
        {
            return gameObject.transform as RectTransform;
        }

        public static RectTransform GetRectTransform(this Component component)
        {
            return component.transform as RectTransform;
        }

        public static void ResetTransform(this GameObject gameObject)
        {
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
		    gameObject.transform.localScale = Vector3.one;
        }
    }
}
