using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner
{
    public class Properties
    {

        private const string PropertiesKey = "properties";
        
        public static readonly bool Mobile = Application.isMobilePlatform;
        public static readonly bool Web = Application.platform == RuntimePlatform.WebGLPlayer;

        public static Properties Instance { get; private set; }

        static Properties()
        {
            Instance = LoadProperties();
        }

        private static Properties LoadProperties()
        {
            if (!PlayerPrefs.HasKey(PropertiesKey))
            {
                return new Properties();
            }
            
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Properties));
            using (TextReader reader = new StringReader(PlayerPrefs.GetString(PropertiesKey)))
            {
                return (Properties) xmlSerializer.Deserialize(reader);
            }
        }

        public event GenericEventArgs Saved;

        public int FieldOfView = 60;

        public float FppMouseSensitivity = 0.5f;
        public float FppKeyboardRotationSensitivity = 60f;
        public float FppMovementSpeed = 16f;
        public float FppShiftModifier = 5f;
        public float FppControlModifier = 0.2f;

        public float TopMouseSensitivity = 0.2f;
        public float TopMovementSpeed = 16f;

        public float IsoMouseSensitivity = 0.2f;
        public float IsoMovementSpeed = 16f;

        public WaterQuality WaterQuality;

        private WaterQuality WaterDefaultQuality {
            get {
                if (Mobile || Web)
                {
                    return WaterQuality.SIMPLE;
                }
                else
                {
                    return WaterQuality.HIGH;
                }
            }
        }

        private Properties()
        {
            WaterQuality = WaterDefaultQuality;
        }

        public void SaveProperties()
        {
            StringBuilder builder = new StringBuilder();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Properties));
            using (TextWriter writer = new StringWriter(builder))
            {
                xmlSerializer.Serialize(writer, this);
            }
            PlayerPrefs.SetString(PropertiesKey, builder.ToString());
            
            Saved?.Invoke();
        }

    }
}
