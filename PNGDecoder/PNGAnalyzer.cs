using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PNGDecoder
{
    class PNGAnalyzer
    {
        byte[] pngData;
        int index;
        bool foundIEND;
        MemoryStream memoryStream;
        byte[] zBuf;
        zlib.ZStream zStream;

        int width;
        int height;
        int bitDepth;
        int colorType;
        int compressMethod;
        int filterMethod;
        int interlaceMethod;

        string errorMessage;

        public PNGAnalyzer(byte[] data)
        {
            pngData = data;
            index = 0;
            foundIEND = false;
            memoryStream = new MemoryStream();
            
            // zlib initialization
            zBuf = new byte[8192];
            zStream = new zlib.ZStream();
            zStream.inflateInit();
            zStream.next_out = zBuf;
            zStream.avail_out = zBuf.Length;
        }

        public bool Analyze()
        {
            bool result = false;

            if (null != pngData)
            {
                if (AnalyzeHeader())
                {
                    if (AnalyzeBody())
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool AnalyzeHeader()
        {
            for (int i = 0; i < PNGUtil.signature.Length; i++)
            {
                if (PNGUtil.signature[i] != pngData[i])
                {
                    errorMessage = "wrong signature";
                    return false;
                }
            }
            index += PNGUtil.signature.Length;
            return true;
        }

        private bool AnalyzeBody()
        {
            while (!foundIEND)
            {
                if (!AnalyzeChunk())
                {
                    return false;
                }
            }

            return true;
        }

        private bool AnalyzeChunk()
        {
            bool result = true;

            int length = 0;
            byte[] typeArray = new byte[4];
            byte[] temp4Byte = new byte[4];
            uint crc = 0;

            // length area
            Array.Copy(pngData, index, temp4Byte, 0, 4);
            length = PNGUtil.GetInt(temp4Byte);
            index += 4;

            // type area
            Array.Copy(pngData, index, typeArray, 0, 4);
            index += 4;

            // data area
            byte[] dataArray = new byte[length];
            Array.Copy(pngData, index, dataArray, 0, length);
            index += length;

            // CRC area
            Array.Copy(pngData, index, temp4Byte, 0, 4);
            index += 4;
            crc = (uint)PNGUtil.GetInt(temp4Byte);

            // CRC check
            if (CRCChecker.CRC(typeArray, dataArray) == crc)
            {
                result = ApplyChunk(typeArray, dataArray);

            }
            else
            {
                // CRC invalid
                errorMessage = "CRC invalid";
                result = false;
            }
            
            return result;
        }

        private bool ApplyChunk(byte[] typeArray, byte[] dataArray)
        {
            bool result = true;
            string typeString = PNGUtil.GetTypeString(typeArray);

            switch (typeString)
            {
                case "IHDR":
                    result = ApplyChunkIHDR(dataArray);
                    break;
                case "PLTE":
                    errorMessage = "can't handle PLTE chunk";
                    result = false;
                    break;
                case "IDAT":
                    result = ApplyChunkIDAT(dataArray);
                    break;
                case "IEND":
                    result = ApplyChunkIEND();
                    foundIEND = result;
                    break;
                case "tRNS":
                case "cHRM":
                case "gAMA":
                case "iCCP":
                case "sBIT":
                case "sRGB":
                case "tEXt":
                case "iTXt":
                case "zTXt":
                case "bKGD":
                case "hIST":
                case "pHYs":
                case "sPLT":
                case "tIME":
                    // chunks to be ignored
                    break;
                default:
                    errorMessage = "unknown chunk type";
                    result = false;
                    break;
            }

            return result;
        }

        private bool ApplyChunkIHDR(byte[] dataArray)
        {
            // verify length
            if (13 != dataArray.Length)
                return false;

            byte[] sizeArray = new byte[4];

            Array.Copy(dataArray, 0, sizeArray, 0, 4);
            width = PNGUtil.GetInt(sizeArray);

            Array.Copy(dataArray, 4, sizeArray, 0, 4);
            height = PNGUtil.GetInt(sizeArray);

            bitDepth = (int)dataArray[8];
            colorType = (int)dataArray[9];
            compressMethod = (int)dataArray[10];
            filterMethod = (int)dataArray[11];
            interlaceMethod = (int)dataArray[12];

            if (bitDepth != 8)
            {
                errorMessage = "can't handle bit depth " + bitDepth;
                return false;
            }
            if (colorType != 2)
            {
                errorMessage = "can't handle color type " + colorType;
                return false;
            }
            if (compressMethod != 0)
            {
                errorMessage = "strange compress method " + compressMethod;
                return false;
            }
            if (filterMethod != 0)
            {
                errorMessage = "strange filter method " + filterMethod;
                return false;
            }
            if (interlaceMethod < 0 || 1 < interlaceMethod)
            {
                errorMessage = "strange interlace method " + interlaceMethod;
                return false;
            }
            return true;
        }

        private bool ApplyChunkIDAT(byte[] dataArray)
        {
            zStream.next_in_index = 0;
            zStream.next_in = dataArray;
            zStream.avail_in = dataArray.Length;

            do
            {
                int inflateErr = zStream.inflate(zlib.zlibConst.Z_SYNC_FLUSH);
                if (inflateErr != zlib.zlibConst.Z_OK &&
                    inflateErr != zlib.zlibConst.Z_STREAM_END)
                {
                    //throw new Exception("inflate error");
                    errorMessage = "inflate error";
                    return false;
                }

                if (zStream.avail_out == 0)
                {
                    memoryStream.Write(zBuf, 0, zBuf.Length);
                    zStream.next_out_index = 0;
                    zBuf.Initialize();
                    zStream.avail_out = zBuf.Length;
                }
            }
            while (zStream.avail_in > 0);

            return true;
        }

        private bool ApplyChunkIEND()
        {
            int inflateErr;

            do {
                //zlib.zlibConst.Z_PARTIAL_FLUSH
                inflateErr = zStream.inflate(zlib.zlibConst.Z_FINISH);

                if (zlib.zlibConst.Z_OK == inflateErr)
                {
                    if (0 == zStream.avail_out)
                    {
                        memoryStream.Write(zBuf, 0, zBuf.Length);
                        zStream.next_out_index = 0;
                        zBuf.Initialize();
                        zStream.avail_out = zBuf.Length;
                    }
                }
                else if (zlib.zlibConst.Z_STREAM_END != inflateErr)
                {
                    errorMessage = "inflate finish error";
                    return false;
                }
            }
            while (zlib.zlibConst.Z_STREAM_END != inflateErr);

            if (zStream.avail_out < zBuf.Length)
            {
                memoryStream.Write(zBuf, 0, zBuf.Length - zStream.avail_out);
            }

            return true;
        }


        // Property
        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public int BitDepth { get { return bitDepth; } }
        public int ColorType { get { return colorType; } }
        //public int CompressMethod { get { return compressMethod; } }
        //public int FilterMethod { get { return filterMethod; } }
        public int InterlaceMethod { get { return interlaceMethod; } }
        
        public byte[] DecompressData { get { return memoryStream.ToArray(); } }
        public string ErrorMessage { get { return errorMessage; } }
    }
}
