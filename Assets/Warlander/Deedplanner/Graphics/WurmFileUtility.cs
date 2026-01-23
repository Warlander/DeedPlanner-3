using System.IO;
using System.Text;

namespace Warlander.Deedplanner.Graphics
{
    public static class WurmFileUtility
    {
        public static string ReadString(BinaryReader source)
        {
            int size = source.ReadInt32();
            byte[] bytes = source.ReadBytes(size);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
