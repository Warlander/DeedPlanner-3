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

        public float TopMovementSpeed = 16f;

        public float IsoMovementSpeed = 16f;

        public float HeightDragSensitivity = 0.5f;
        public bool HeightRespectOriginalSlopes = true;

        public bool WallAutomaticReverse = true;
        public bool WallReverse = false;

        public float DecorationRotationSensitivity = 1f;
        public bool DecorationSnapToGrid = false;
        public bool DecorationRotationSnapping = false;

        public int GuiScale = 10;

        public WaterQuality WaterQuality;

        private WaterQuality WaterDefaultQuality {
            get {
                if (Mobile || Web)
                {
                    return WaterQuality.Simple;
                }
                else
                {
                    return WaterQuality.High;
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
            PlayerPrefs.Save();

            Debug.Log("Properties saved");

            Saved?.Invoke();
        }
    }
}
