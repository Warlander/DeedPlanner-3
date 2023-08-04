using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Settings
{
    /// <summary>
    /// Settings should never be modified directly - use "Modify" method instead.
    /// </summary>
    public class DPSettings
    {
        public const string SettingsKey = "properties";

        public event Action Modified;

        public float FppMouseSensitivity = 0.5f;
        public float FppKeyboardRotationSensitivity = 60f;
        public float FppMovementSpeed = 16f;
        public float FppShiftModifier = 5f;
        public float FppControlModifier = 0.2f;

        public float TopMovementSpeed = 16f;

        public float IsoMovementSpeed = 16f;

        public float HeightDragSensitivity = 0.5f;
        public bool HeightRespectOriginalSlopes = true;

        public bool WallAutomaticReverse = true;
        public bool WallReverse = false;

        public bool DecorationSnapToGrid = false;
        public bool DecorationRotationSnapping = false;
        public string DecorationRotationSensitivity = "1";

        public int GuiScale = 10;

        public WaterQuality WaterQuality = WaterDefaultQuality;

        private static WaterQuality WaterDefaultQuality
        {
            get
            {
                if (SystemInfo.deviceType == DeviceType.Desktop && Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    return WaterQuality.High;
                }
                else
                {
                    return WaterQuality.Simple;
                }
            }
        }

        public void Modify(Action<DPSettings> modifyCallback, bool autoSave = true)
        {
            modifyCallback.Invoke(this);
            Modified?.Invoke();
            if (autoSave)
            {
                Save();
            }
        }

        public void Save()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;

            StringBuilder builder = new StringBuilder();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DPSettings));
            using (TextWriter writer = new StringWriter(builder))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
                {
                    xmlSerializer.Serialize(xmlWriter, this);
                }
            }
            PlayerPrefs.SetString(SettingsKey, builder.ToString());
            PlayerPrefs.Save();

            Debug.Log("Properties saved");
        }
    }
}
