using System.Threading.Tasks;

namespace Warlander.Deedplanner.Persistence.Compression
{
    public interface IByteCompressor
    {
        bool IsCompressed(byte[] data);
        byte[] Compress(byte[] raw);
        byte[] Decompress(byte[] compressed);
        Task<byte[]> DecompressAsync(byte[] compressed);
    }
}
