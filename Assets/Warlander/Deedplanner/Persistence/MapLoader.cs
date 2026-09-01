using Warlander.Deedplanner.Platform.Web;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Persistence.Compression;

namespace Warlander.Deedplanner.Persistence
{
    public class MapLoader
    {
        public static readonly LogCategory Category = new LogCategory("Maps");

        private readonly MapFactory _mapFactory;
        private readonly IByteCompressor _compressor;
        private readonly ICategoryLogger _logger;

        public ICategoryLogger Logger => _logger;

        public MapLoader(MapFactory mapFactory, IByteCompressor compressor, ILoggerSource loggerSource)
        {
            _mapFactory = mapFactory;
            _compressor = compressor;
            _logger = loggerSource.Create(Category);
        }

        public Map LoadMap(string mapString)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(mapString);
            return _mapFactory.LoadFromXml(doc);
        }

        public Map LoadMap(byte[] mapData)
        {
            if (_compressor.IsCompressed(mapData))
            {
                return LoadMap(Encoding.UTF8.GetString(_compressor.Decompress(mapData)));
            }

            return LoadMap(Encoding.UTF8.GetString(mapData));
        }

        public async Task<Map> LoadMapAsync(Uri mapUri)
        {
            byte[] mapData = await WebUtils.ReadUrlToByteArrayAsync(mapUri);

            if (mapData == null)
            {
                _logger.Error("Failed to download map from: " + mapUri);
                return null;
            }

            _logger.Message("Map downloaded, checking if compressed");
            string requestText = Encoding.UTF8.GetString(mapData);

            try
            {
                byte[] requestBytes = Convert.FromBase64String(requestText);
                byte[] decompressedBytes = await _compressor.DecompressAsync(requestBytes);
                requestText = Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length);
                _logger.Message("Compressed map, decompressed");
            }
            catch
            {
                _logger.Message("Not compressed map");
            }

            return LoadMap(requestText);
        }
    }
}
