using System;
using System.IO;
using System.IO.Compression;

namespace CatMailExpanded.Core
{
    /// <summary>
    /// Minimal PNG decoder (8-bit greyscale/RGB/RGBA/grey+alpha, non-interlaced).
    /// Needed because Unity 6's ImageConversion.LoadImage takes a ReadOnlySpan which the
    /// current Il2CppInterop cannot marshal. Output is RGBA32, bottom row first (Unity order).
    /// </summary>
    internal static class PngDecoder
    {
        public static byte[] Decode(byte[] png, out int width, out int height)
        {
            using var br = new BinaryReader(new MemoryStream(png));
            br.ReadBytes(8);
            width = height = 0;
            int bitDepth = 0, colorType = 0, interlace = 0;
            var idat = new MemoryStream();
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                int len = ReadBE(br);
                string type = new string(br.ReadChars(4));
                byte[] data = br.ReadBytes(len);
                br.ReadUInt32(); // crc
                if (type == "IHDR")
                {
                    width = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
                    height = (data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7];
                    bitDepth = data[8];
                    colorType = data[9];
                    interlace = data[12];
                }
                else if (type == "IDAT") idat.Write(data, 0, data.Length);
                else if (type == "IEND") break;
            }
            if (bitDepth != 8 || interlace != 0) throw new NotSupportedException($"PNG bitDepth={bitDepth} interlace={interlace}");
            int channels = colorType switch { 0 => 1, 2 => 3, 4 => 2, 6 => 4, _ => throw new NotSupportedException("PNG colorType " + colorType) };
            idat.Position = 0;
            byte[] raw;
            using (var z = new ZLibStream(idat, CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                z.CopyTo(outMs);
                raw = outMs.ToArray();
            }
            int stride = width * channels;
            var cur = new byte[stride];
            var prev = new byte[stride];
            var rgba = new byte[width * height * 4];
            int p = 0;
            for (int y = 0; y < height; y++)
            {
                byte filter = raw[p++];
                Buffer.BlockCopy(raw, p, cur, 0, stride);
                p += stride;
                for (int i = 0; i < stride; i++)
                {
                    int a = i >= channels ? cur[i - channels] : 0;
                    int b = prev[i];
                    int c = i >= channels ? prev[i - channels] : 0;
                    switch (filter)
                    {
                        case 1: cur[i] = (byte)(cur[i] + a); break;
                        case 2: cur[i] = (byte)(cur[i] + b); break;
                        case 3: cur[i] = (byte)(cur[i] + ((a + b) >> 1)); break;
                        case 4: cur[i] = (byte)(cur[i] + Paeth(a, b, c)); break;
                    }
                }
                int row = (height - 1 - y) * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int o = row + x * 4, s = x * channels;
                    switch (channels)
                    {
                        case 1: rgba[o] = rgba[o + 1] = rgba[o + 2] = cur[s]; rgba[o + 3] = 255; break;
                        case 2: rgba[o] = rgba[o + 1] = rgba[o + 2] = cur[s]; rgba[o + 3] = cur[s + 1]; break;
                        case 3: rgba[o] = cur[s]; rgba[o + 1] = cur[s + 1]; rgba[o + 2] = cur[s + 2]; rgba[o + 3] = 255; break;
                        default: rgba[o] = cur[s]; rgba[o + 1] = cur[s + 1]; rgba[o + 2] = cur[s + 2]; rgba[o + 3] = cur[s + 3]; break;
                    }
                }
                (cur, prev) = (prev, cur);
            }
            return rgba;
        }

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        static int ReadBE(BinaryReader br)
        {
            var b = br.ReadBytes(4);
            return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        }
    }
}
