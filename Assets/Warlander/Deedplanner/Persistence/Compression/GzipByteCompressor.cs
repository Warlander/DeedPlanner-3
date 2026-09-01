using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Persistence.Compression
{
    public class GzipByteCompressor : IByteCompressor
    {
        public bool IsCompressed(byte[] data)
        {
            return data != null && data.Length > 2 && data[0] == 0x1F && data[1] == 0x8B;
        }

        public byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream stream = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    stream.Write(raw, 0, raw.Length);
                }

                return memory.ToArray();
            }
        }

        public byte[] Decompress(byte[] compressed)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        public async Task<byte[]> DecompressAsync(byte[] compressed)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
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
