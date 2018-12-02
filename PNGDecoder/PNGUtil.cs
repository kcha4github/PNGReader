using System;
using System.Collections.Generic;
using System.Text;

namespace PNGDecoder
{
    class PNGUtil
    {
        public static readonly byte[] signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        public static readonly byte[] chunkNameIHDR = new byte[] { 73, 72, 68, 82 };
        public static readonly byte[] chunkNameIDAT = new byte[] { 73, 68, 65, 84 };
        public static readonly byte[] chunkNameIEND = new byte[] { 73, 69, 78, 68 };

        public static readonly int[] passStart = new int[7] { 0, 4, 0, 2, 0, 1, 0 };
        public static readonly int[] passIncrement = new int[7] { 8, 8, 4, 4, 2, 2, 1 };
        public static readonly int[] passYStart = new int[7] { 0, 0, 4, 0, 2, 0, 1 };
        public static readonly int[] passYIncrement = new int[7] { 8, 8, 8, 4, 4, 2, 2 };

        // afterword, CREATE utility class and move to it
        public static void GetBigEndian32(byte[] output, uint input)
        {
            output[0] = (byte)((input >> 24) & 0x000000ff);
            output[1] = (byte)((input >> 16) & 0x000000ff);
            output[2] = (byte)((input >> 8) & 0x000000ff);
            output[3] = (byte)(input & 0x000000ff);
        }

        public static int GetInt(byte[] bNum)
        {
            int retValue = 0;
            foreach (byte b in bNum)
            {
                retValue = (retValue << 8) | (int)b;
            }
            return retValue;
        }

        public static int CalculateWidth(int width, int pass)
        {
            int result;
            result = (width + passIncrement[pass] - 1 - passStart[pass]) /
                passIncrement[pass];

            return result;
        }

        public static int PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if ((pa <= pb) && (pa <= pc))
                return a;

            if (pb <= pc)
                return b;

            return c;
        }

        public static string GetTypeString(byte[] typeArray)
        {
            return Encoding.GetEncoding(65001).GetString(typeArray);
        }

    }
}
