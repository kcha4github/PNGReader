using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace PNGReader
{
    public partial class Form1 : Form
    {
        Bitmap bmp;

        public Form1()
        {
            InitializeComponent();
        }

        private void OpenPNGFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Bitmap bmp = null;

            try {
                // Decoding HERE!
                PNGDecoder.Decoder decoder = new PNGDecoder.Decoder(path);
                if (decoder.Decode())
                {
                    int width = decoder.Width;
                    int height = decoder.Height;

                    bmp = new Bitmap(width, height);

                    byte[] dataR = decoder.R;
                    byte[] dataG = decoder.G;
                    byte[] dataB = decoder.B;
                    int index = 0;

                    for (int h = 0; h < height; h++)
                    {
                        for (int w = 0; w < width; w++)
                        {
                            bmp.SetPixel(w, h, Color.FromArgb(dataR[index], dataG[index], dataB[index]));
                            index++;
                        }
                    }
                }
                else
                {
                    string errorMessage = decoder.ErrorMessage;
                    MessageBox.Show(errorMessage);
                }

                this.bmp = bmp;
            }
            catch
            {
                return;
            }
        }

        private void contextMenuStrip1_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Hide();
            if (DialogResult.OK != openFileDialog1.ShowDialog())
                return;

            OpenPNGFile(openFileDialog1.FileName);
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Empty);

            if (null == bmp)
                return;

            e.Graphics.DrawImage(bmp, 0, 0);
        }
    }
}