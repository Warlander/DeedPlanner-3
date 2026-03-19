using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Settings
{
    public class SettingsFactory
    {
        public DPSettings Create()
        {
            if (!PlayerPrefs.HasKey(DPSettings.SettingsKey))
            {
                return new DPSettings();
            }

            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(DPSettings));
                using (TextReader reader = new StringReader(PlayerPrefs.GetString(DPSettings.SettingsKey)))
                {
                    using (XmlReader xmlReader = new XmlTextReader(reader))
                    {
                        DPSettings settings = (DPSettings) xmlSerializer.Deserialize(xmlReader);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return new DPSettings();
            }
        }
    }
}