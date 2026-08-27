using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Settings
{
    public class SettingsFactory
    {
        public static readonly LogCategory Category = new LogCategory("Settings");

        private readonly ICategoryLogger _logger;

        public SettingsFactory(ILoggerSource loggerSource)
        {
            _logger = loggerSource.Create(Category);
        }

        public DPSettings Create()
        {
            DPSettings settings = LoadOrDefault();
            settings.Logger = _logger;
            return settings;
        }

        private DPSettings LoadOrDefault()
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
                _logger.Exception(ex);
                return new DPSettings();
            }
        }
    }
}