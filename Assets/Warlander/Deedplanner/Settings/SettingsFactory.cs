using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Settings
{
    public class SettingsFactory : IFactory<DPSettings>
    {
        public DPSettings Create()
        {
            if (!PlayerPrefs.HasKey(DPSettings.SettingsKey))
            {
                return new DPSettings();
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DPSettings));
            using (TextReader reader = new StringReader(PlayerPrefs.GetString(DPSettings.SettingsKey)))
            {
                return (DPSettings) xmlSerializer.Deserialize(reader);
            }
        }
    }
}