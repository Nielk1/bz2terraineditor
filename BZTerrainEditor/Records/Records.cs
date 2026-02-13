using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZTerrainEditor.Records
{
    //public sealed record HeightmapF32(int W, int H, float[] Data);
    //public sealed record IndexMap4Bit(int W, int H, byte[] Data);      // enforce 0..15
    public sealed record ColorMapRgb24(int W, int H, byte[] Rgb);      // 3 bytes per pixel
    public sealed record AlphaMap8(int W, int H, byte[] A);
    public sealed record FlagsMap<TEnum>(int W, int H, TEnum[] Data) where TEnum : unmanaged, Enum;
}
