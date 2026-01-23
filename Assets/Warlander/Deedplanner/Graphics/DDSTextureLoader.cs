using System;
using UnityEngine;
using Warlander.Deedplanner.Utils;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
{
    public class DDSTextureLoader : ITextureLoader
    {
        public void LoadTexture(string location, bool readable, Action<Texture2D> onLoaded)
        {
            WebUtils.ReadUrlToByteArray(location, data =>
            {
                Texture2D texture = LoadTextureDxt(data);
                onLoaded.Invoke(texture);
            });
        }

        private Texture2D LoadTextureDxt(byte[] ddsBytes)
        {
            if (ddsBytes == null)
            {
                Debug.LogWarning("Unable to load DDS texture. Returning placeholder instead.");
                return Texture2D.whiteTexture;
            }
            
            byte ddsSizeCheck = ddsBytes[4];
            if (ddsSizeCheck != 124)
                throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

            int height = ReadShort(ddsBytes, 12);
            int width = ReadShort(ddsBytes, 16);
            int totalSize = width * height;
            int pitch = ReadInt(ddsBytes, 20);

            const int ddsHeaderSize = 128;
            
            byte[] dxtBytes = new byte[ddsBytes.Length - ddsHeaderSize];
            Buffer.BlockCopy(ddsBytes, ddsHeaderSize, dxtBytes, 0, ddsBytes.Length - ddsHeaderSize);

            TextureFormat textureFormat;
            if (pitch == totalSize)
            {
                textureFormat = TextureFormat.DXT5;
            }
            else
            {
                textureFormat = TextureFormat.DXT1;
            }

            Texture2D dxtTexture = new Texture2D(width, height, textureFormat, false);
            dxtTexture.LoadRawTextureData(dxtBytes);

            Texture2D finalTexture = new Texture2D(dxtTexture.width, dxtTexture.height);
            Color32[] pixelBuffer = dxtTexture.GetPixels32();
            Object.Destroy(dxtTexture);

            int yScanSize = finalTexture.height / 2;
            int xScanSize = finalTexture.width;
            for (int y = 0; y < yScanSize; y++)
            {
                for (int x = 0; x < xScanSize; x++)
                {
                    int originalIndex = y * finalTexture.width + x;
                    int flippedIndex = (finalTexture.height - y - 1) * finalTexture.width + x;
                    Color32 temp = pixelBuffer[flippedIndex];
                    pixelBuffer[flippedIndex] = pixelBuffer[originalIndex];
                    pixelBuffer[originalIndex] = temp;
                }
            }
            finalTexture.SetPixels32(pixelBuffer);
            finalTexture.Apply();
            finalTexture.Compress(true);

            return finalTexture;
        }
        
        private short ReadShort(byte[] data, int index)
        {
            return (short) (data[index + 1] * 256 + data[index]);
        }

        private int ReadInt(byte[] data, int index)
        {
            return data[index + 3] * 256 * 256 * 256 + data[index + 2] * 256 * 256 + data[index + 1] * 256 + data[index];
        }
    }
}
