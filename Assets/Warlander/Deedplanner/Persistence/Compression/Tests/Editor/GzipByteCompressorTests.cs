using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Warlander.Deedplanner.Persistence.Compression;

namespace Warlander.Deedplanner.Tests
{
    public class GzipByteCompressorTests
    {
        private GzipByteCompressor _compressor;

        [SetUp]
        public void SetUp()
        {
            _compressor = new GzipByteCompressor();
        }

        [Test]
        public void CompressDecompress_RoundtripsUtf8Text()
        {
            byte[] raw = Encoding.UTF8.GetBytes("<map><tile height=\"5\"/></map>");

            byte[] compressed = _compressor.Compress(raw);
            byte[] result = _compressor.Decompress(compressed);

            CollectionAssert.AreEqual(raw, result);
        }

        [Test]
        public void CompressDecompress_EmptyInput_Roundtrips()
        {
            byte[] compressed = _compressor.Compress(Array.Empty<byte>());

            CollectionAssert.AreEqual(Array.Empty<byte>(), _compressor.Decompress(compressed));
        }

        [Test]
        public async Task DecompressAsync_RoundtripsCompressedData()
        {
            byte[] raw = Encoding.UTF8.GetBytes("some map payload");
            byte[] compressed = _compressor.Compress(raw);

            byte[] result = await _compressor.DecompressAsync(compressed);

            CollectionAssert.AreEqual(raw, result);
        }

        [Test]
        public void IsCompressed_OwnOutput_ReturnsTrue()
        {
            byte[] compressed = _compressor.Compress(Encoding.UTF8.GetBytes("payload"));

            Assert.IsTrue(_compressor.IsCompressed(compressed));
        }

        [Test]
        public void IsCompressed_PlainUtf8_ReturnsFalse()
        {
            Assert.IsFalse(_compressor.IsCompressed(Encoding.UTF8.GetBytes("<map/>")));
        }

        [Test]
        public void IsCompressed_NullOrShort_ReturnsFalse()
        {
            Assert.IsFalse(_compressor.IsCompressed(null));
            Assert.IsFalse(_compressor.IsCompressed(new byte[] { 0x1F, 0x8B }));
        }

        [Test]
        public void Decompress_NotGzip_Throws()
        {
            Assert.Catch<IOException>(() => _compressor.Decompress(Encoding.UTF8.GetBytes("not gzip at all")));
        }
    }
}
