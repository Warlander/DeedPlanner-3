using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;
using Warlander.Deedplanner.Ui;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    /// <summary>
    /// One-time migration of legacy persistence into the unified settings store:
    /// the DPSettings XML blob ("properties") and the Input System binding overrides
    /// JSON ("inputSettings"). Legacy keys are deleted only after the new store
    /// confirms the values.
    /// </summary>
    public static class LegacySettingsMigration
    {
        public const string LegacyPropertiesKey = "properties";
        public const string LegacyInputSettingsKey = "inputSettings";
        public const string BindingOverridesKey = "keybindOverrides";

        public static void Migrate(ISettingsStore store)
        {
            Migrate(store, LegacyPropertiesKey, LegacyInputSettingsKey);
        }

        public static void Migrate(ISettingsStore store, string propertiesKey, string inputSettingsKey)
        {
            MigrateProperties(store, propertiesKey);
            MigrateBindingOverrides(store, inputSettingsKey);
        }

        public static Dictionary<string, string> TranslatePropertiesXml(string xml)
        {
            TryTranslatePropertiesXml(xml, out Dictionary<string, string> result);
            return result;
        }

        private static bool TryTranslatePropertiesXml(string xml, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>();
            XElement root;
            try
            {
                root = XElement.Parse(xml);
            }
            catch (Exception)
            {
                return false;
            }

            TranslateFloat(root, "FppMouseSensitivity", "fppMouseSensitivity", result);
            TranslateFloat(root, "FppKeyboardRotationSensitivity", "fppKeyboardRotationSensitivity", result);
            TranslateFloat(root, "FppMovementSpeed", "fppMovementSpeed", result);
            TranslateFloat(root, "TopMovementSpeed", "topMovementSpeed", result);
            TranslateFloat(root, "IsoMovementSpeed", "isoMovementSpeed", result);
            TranslateFloat(root, "FppShiftModifier", "shiftSpeedModifier", result);
            TranslateFloat(root, "FppControlModifier", "controlSpeedModifier", result);
            TranslateFloat(root, "HeightDragSensitivity", "heightDragSensitivity", result);
            TranslateBool(root, "HeightRespectOriginalSlopes", "heightRespectOriginalSlopes", result);
            TranslateBool(root, "WallAutomaticReverse", "wallAutomaticReverse", result);
            TranslateBool(root, "WallReverse", "wallReverse", result);
            TranslateBool(root, "DecorationSnapToGrid", "decorationSnapToGrid", result);
            TranslateBool(root, "DecorationRotationSnapping", "decorationRotationSnapping", result);
            TranslateFloat(root, "DecorationRotationSensitivity", "decorationRotationSensitivity", result);
            TranslateInt(root, "GuiScale", "guiScale", result);
            TranslateWaterQuality(root, result);
            TranslateBool(root, "CompassVisibility", "compassVisibility", result);

            return true;
        }

        private static void MigrateProperties(ISettingsStore store, string propertiesKey)
        {
            if (!PlayerPrefs.HasKey(propertiesKey))
            {
                return;
            }
            if (!TryTranslatePropertiesXml(PlayerPrefs.GetString(propertiesKey),
                    out Dictionary<string, string> translated) || translated.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in translated)
            {
                if (!store.TryLoad(pair.Key, out _))
                {
                    store.Save(pair.Key, pair.Value);
                }
            }

            bool confirmed = true;
            foreach (KeyValuePair<string, string> pair in translated)
            {
                confirmed &= store.TryLoad(pair.Key, out _);
            }
            if (confirmed)
            {
                PlayerPrefs.DeleteKey(propertiesKey);
                PlayerPrefs.Save();
            }
        }

        private static void MigrateBindingOverrides(ISettingsStore store, string inputSettingsKey)
        {
            if (!PlayerPrefs.HasKey(inputSettingsKey))
            {
                return;
            }
            if (!store.TryLoad(BindingOverridesKey, out _))
            {
                store.Save(BindingOverridesKey, PlayerPrefs.GetString(inputSettingsKey));
            }
        }

        private static void TranslateFloat(XElement root, string elementName, string newKey, Dictionary<string, string> result)
        {
            string raw = GetElementValue(root, elementName);
            if (raw != null && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                result[newKey] = value.ToString("R", CultureInfo.InvariantCulture);
            }
        }

        private static void TranslateInt(XElement root, string elementName, string newKey, Dictionary<string, string> result)
        {
            string raw = GetElementValue(root, elementName);
            if (raw != null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                result[newKey] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static void TranslateBool(XElement root, string elementName, string newKey, Dictionary<string, string> result)
        {
            string raw = GetElementValue(root, elementName);
            if (bool.TryParse(raw, out bool value))
            {
                result[newKey] = value ? "true" : "false";
            }
        }

        private static void TranslateWaterQuality(XElement root, Dictionary<string, string> result)
        {
            string raw = GetElementValue(root, "WaterQuality");
            if (raw != null && Enum.TryParse(raw, true, out WaterQuality quality))
            {
                result["waterQuality"] = quality.ToString();
            }
        }

        private static string GetElementValue(XElement root, string elementName)
        {
            return root.Element(elementName)?.Value;
        }
    }
}
