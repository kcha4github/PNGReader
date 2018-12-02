using System;
using System.Collections.Generic;
using System.Text;

namespace PNGDecoder
{
    class PNG
    {
        public const byte PNG_COLOR_TYPE_RGB = 2;

        public const byte PNG_COMPRESSION_METHOD_BASE = 0;

        public const byte PNG_INTERLACE_METHOD_BASE = 0;
        public const byte PNG_INTERLACE_NONE = 0;
        public const byte PNG_INTERLACE_ADAM7 = 1;
        public const byte PNG_INTERLACE_LAST = 2;


        public const byte PNG_FILTER_METHOD_BASE = 0;

        public const byte FILTER_VALUE_NONE = 0;
        public const byte FILTER_VALUE_SUB = 1;
        public const byte FILTER_VALUE_UP = 2;
        public const byte FILTER_VALUE_AVG = 3;
        public const byte FILTER_VALUE_PAETH = 4;
        public const byte FILTER_VALUE_LAST = 5;
    }

    class Chunk
    {
        int length;
        string type;

        public Chunk(int length, string type)
        {
            this.length = length;
            this.type = type;
        }

        public override string ToString()
        {
            return string.Format("Length:{0} ChunkType:{1}", length, type);
        }

        public int Length { get { return length; } }
        public string Type { get { return type; } }
    }

    class ChunkOrderChecker { }

}
