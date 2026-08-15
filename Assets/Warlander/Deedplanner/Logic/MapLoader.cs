using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    public class MapLoader
    {
        private readonly MapFactory _mapFactory;

        public MapLoader(MapFactory mapFactory)
        {
            _mapFactory = mapFactory;
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
                Debug.LogError("Failed to download map from: " + mapUri);
                return null;
            }

            Debug.Log("Map downloaded, checking if compressed");
            string requestText = Encoding.UTF8.GetString(mapData);

            try
            {
                byte[] requestBytes = Convert.FromBase64String(requestText);
                byte[] decompressedBytes = await DecompressGzipAsync(requestBytes);
                requestText = Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length);
                Debug.Log("Compressed map, decompressed");
            }
            catch
            {
                Debug.Log("Not compressed map");
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
