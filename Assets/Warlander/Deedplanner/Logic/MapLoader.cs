using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    public class MapLoader
    {
        public static readonly LogCategory Category = new LogCategory("Maps");

        private readonly MapFactory _mapFactory;
        private readonly ICategoryLogger _logger;

        public ICategoryLogger Logger => _logger;

        public MapLoader(MapFactory mapFactory, ILoggerSource loggerSource)
        {
            _mapFactory = mapFactory;
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
            if (mapData.Length > 2 && mapData[0] == 0x1F && mapData[1] == 0x8B)
            {
                return LoadMap(Encoding.UTF8.GetString(DecompressGzip(mapData)));
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
                byte[] decompressedBytes = await DecompressGzipAsync(requestBytes);
                requestText = Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length);
                _logger.Message("Compressed map, decompressed");
            }
            catch
            {
                _logger.Message("Not compressed map");
            }

            return LoadMap(requestText);
        }

        private static byte[] DecompressGzip(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        private async Task<byte[]> DecompressGzipAsync(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    await stream.CopyToAsync(memory);
                    return memory.ToArray();
                }
            }
        }
    }
}
