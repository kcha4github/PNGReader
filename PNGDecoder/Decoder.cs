using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PNGDecoder
{
    public class Decoder
    {
        byte[] pngData;
        string errorMessage;
        byte[] decompressedData;
        int decompressedIndex = 0;

        int height;
        int width;
        int colorType;
        int channels;
        int passTotal;

        byte[] dataR;
        byte[] dataG;
        byte[] dataB;

        public Decoder(string path)
        {
            if (!File.Exists(path))
                throw new Exception("file not exists");

            FileInfo fileInfo = new FileInfo(path);
            int pngFileSize = (int)fileInfo.Length;

            // prepare buffer
            pngData = new byte[pngFileSize];
            errorMessage = null;
            
            Stream inputStream = File.OpenRead(path);
            BufferedStream bufferedInputStream = new BufferedStream(inputStream);
            bufferedInputStream.Read(pngData, 0, pngFileSize);
            bufferedInputStream.Flush();
            bufferedInputStream.Close();
            inputStream.Close();
        }

        public bool Decode()
        {
            PNGAnalyzer analyzer = new PNGAnalyzer(pngData);
            if (!analyzer.Analyze())
            {
                errorMessage = analyzer.ErrorMessage;
                return false;
            }

            // succeeded to scan data

            // ======================

            // begin to arrange the image data

            height = analyzer.Height;
            width = analyzer.Width;
            dataR = new byte[height * width];
            dataG = new byte[height * width];
            dataB = new byte[height * width];
            colorType = analyzer.ColorType;
            switch (colorType)
            {
                case 2:
                    channels = 3;
                    break;
                default:
                    errorMessage = "Color type can't be handled";
                    return false;
            }

            decompressedData = analyzer.DecompressData;

            passTotal = analyzer.InterlaceMethod == 0 ? 1 : 7;


            for (int passIndex = 0; passIndex < passTotal; passIndex++)
            {
                int heightStart = 0;
                int heightSkip = 1;
                int widthStart = 0;
                int widthSkip = 1;
                int rowLength = width;

                // for interlace, 
                if (passTotal > 1)
                {
                    widthStart = PNGUtil.passStart[passIndex];
                    widthSkip = PNGUtil.passIncrement[passIndex];
                    heightStart = PNGUtil.passYStart[passIndex];
                    heightSkip = PNGUtil.passYIncrement[passIndex];
                    rowLength = PNGUtil.CalculateWidth(width, passIndex);
                }

                // in the case that the pass have no reduced image.
                if (rowLength <= 0)
                    continue;

                int rowBytes = rowLength * channels + 1;
                byte[] rowPrev;
                byte[] rowCurrent = new byte[rowBytes];

                for (int h = heightStart; h < height; h += heightSkip)
                {
                    rowPrev = rowCurrent;
                    rowCurrent = new byte[rowBytes];

                    Array.Copy(decompressedData, decompressedIndex, rowCurrent, 0, rowBytes);

                    switch (rowCurrent[0])
                    {
                        case 0:
                            // NONE
                            break;
                        case 1:
                            // SUB
                            for (int i = 4; i < rowBytes; i += 3)
                            {
                                rowCurrent[i + 0] = (byte)((int)rowCurrent[i + 0] + (int)rowCurrent[i - 3]);
                                rowCurrent[i + 1] = (byte)((int)rowCurrent[i + 1] + (int)rowCurrent[i - 2]);
                                rowCurrent[i + 2] = (byte)((int)rowCurrent[i + 2] + (int)rowCurrent[i - 1]);
                            }
                            break;
                        case 2:
                            // UP
                            for (int i = 1; i < rowBytes; i += 3)
                            {
                                rowCurrent[i + 0] = (byte)((int)rowCurrent[i + 0] + (int)rowPrev[i + 0]);
                                rowCurrent[i + 1] = (byte)((int)rowCurrent[i + 1] + (int)rowPrev[i + 1]);
                                rowCurrent[i + 2] = (byte)((int)rowCurrent[i + 2] + (int)rowPrev[i + 2]);
                            }
                            break;
                        case 3:
                            // AVERAGE
                            rowCurrent[1] = (byte)((int)rowCurrent[1] + ((int)rowPrev[1] / 2));
                            rowCurrent[2] = (byte)((int)rowCurrent[2] + ((int)rowPrev[2] / 2));
                            rowCurrent[3] = (byte)((int)rowCurrent[3] + ((int)rowPrev[3] / 2));
                            for (int i = 4; i < rowBytes; i += 3)
                            {
                                rowCurrent[i + 0] = (byte)((int)rowCurrent[i + 0] + (((int)rowCurrent[i - 3] + (int)rowPrev[i + 0]) / 2));
                                rowCurrent[i + 1] = (byte)((int)rowCurrent[i + 1] + (((int)rowCurrent[i - 2] + (int)rowPrev[i + 1]) / 2));
                                rowCurrent[i + 2] = (byte)((int)rowCurrent[i + 2] + (((int)rowCurrent[i - 1] + (int)rowPrev[i + 2]) / 2));
                            }
                            break;
                        case 4:
                            // PAETH
                            rowCurrent[1] = (byte)((int)rowCurrent[1] + PNGUtil.PaethPredictor(0, (int)rowPrev[1], 0));
                            rowCurrent[2] = (byte)((int)rowCurrent[2] + PNGUtil.PaethPredictor(0, (int)rowPrev[2], 0));
                            rowCurrent[3] = (byte)((int)rowCurrent[3] + PNGUtil.PaethPredictor(0, (int)rowPrev[3], 0));
                            for (int i = 4; i < rowBytes; i += 3)
                            {
                                rowCurrent[i + 0] = (byte)((int)rowCurrent[i + 0] + PNGUtil.PaethPredictor((int)rowCurrent[i - 3], (int)rowPrev[i + 0], (int)rowPrev[i - 3]));
                                rowCurrent[i + 1] = (byte)((int)rowCurrent[i + 1] + PNGUtil.PaethPredictor((int)rowCurrent[i - 2], (int)rowPrev[i + 1], (int)rowPrev[i - 2]));
                                rowCurrent[i + 2] = (byte)((int)rowCurrent[i + 2] + PNGUtil.PaethPredictor((int)rowCurrent[i - 1], (int)rowPrev[i + 2], (int)rowPrev[i - 1]));
                            }
                            break;
                        default:
                            errorMessage = "Unknown filter type";
                            return false;
                    }

                    int t = 1;
                    for (int w = widthStart; w < width; w += widthSkip)
                    {
                        dataR[h * width + w] = rowCurrent[t++];
                        dataG[h * width + w] = rowCurrent[t++];
                        dataB[h * width + w] = rowCurrent[t++];
                    }

                    decompressedIndex += rowBytes;
                }
            }

            return true;
        }


        // property
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        public byte[] R { get { return dataR; } }
        public byte[] G { get { return dataG; } }
        public byte[] B { get { return dataB; } }
        public string ErrorMessage { get { return errorMessage; } }
    }
}
