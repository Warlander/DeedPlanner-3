using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner
{
    public class Properties
    {

        private static readonly bool Mobile = Application.isMobilePlatform;
        private static readonly bool Web = Application.platform == RuntimePlatform.WebGLPlayer;

        public static readonly string HomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string ProgramDirectory = Path.Combine(HomeDirectory, ".DeedPlanner3");
        public static readonly string PropertiesFile = Path.Combine(ProgramDirectory, "Properties.xml");

        public static Properties Instance { get; private set; }

        static Properties()
        {
            Instance = LoadProperties();
        }

        private static Properties LoadProperties()
        {
            if (!File.Exists(PropertiesFile))
            {
                return new Properties();
            }

            FileStream input = new FileStream(PropertiesFile, FileMode.Open, FileAccess.Read);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Properties));
            Properties properties = (Properties) xmlSerializer.Deserialize(input);
            input.Close();

            return properties;
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
            DirectoryInfo directory = Directory.CreateDirectory(ProgramDirectory);
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            FileStream output = new FileStream(PropertiesFile, FileMode.Create, FileAccess.Write);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Properties));
            xmlSerializer.Serialize(output, this);
            output.Close();

            if (Saved != null)
            {
                Saved();
            }
        }

    }
}
