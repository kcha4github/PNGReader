using System;
using System.Collections.Generic;
using System.Text;

namespace PNGDecoder
{
    class CRCChecker
    {
        static uint[] crcTable;
        static bool crcTableComputed = false;

        static CRCChecker()
        {
            crcTable = new uint[256];
            makeCRCTable();
        }

        static void makeCRCTable()
        {
            uint c;

            for (int n = 0; n < 256; n++)
            {
                c = (uint)n;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 0x01) == 0x01)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c = c >> 1;
                }

                crcTable[n] = c;
            }
            crcTableComputed = true;
        }

        static uint updateCRC(uint crc, byte[] buf)
        {
            uint c = crc;

            if (!crcTableComputed)
                makeCRCTable();

            for (int n = 0; n < buf.Length; n++)
            {
                c = crcTable[(c ^ buf[n]) & 0xff] ^ (c >> 8);
            }

            return c;
        }

        static uint updateCRC(uint crc, byte[] buf, int start, int len)
        {
            uint c = crc;

            if (!crcTableComputed)
                makeCRCTable();

            for (int n = 0; n < len; n++)
            {
                c = crcTable[(c ^ buf[start + n]) & 0xff] ^ (c >> 8);
            }

            return c;
        }

        static public uint CRC(byte[] buf)
        {
            return updateCRC(0xffffffff, buf) ^ 0xffffffff;
        }

        static public uint CRC(byte[] type, byte[] buf)
        {
            uint midCRC = updateCRC(0xffffffff, type);
            return updateCRC(midCRC, buf) ^ 0xffffffff;
        }

        static public uint CRC(byte[] type, byte[] buf, int start, int len)
        {
            uint midCRC;
            midCRC = updateCRC(0xffffffff, type);
            midCRC = updateCRC(midCRC, buf, start, len);
            return midCRC ^ 0xffffffff;
        }
    }
}
