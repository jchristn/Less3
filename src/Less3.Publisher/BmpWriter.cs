namespace Less3.Publisher
{
    using System;
    using System.IO;
    using SkiaSharp;

    /// <summary>
    /// Writes 24-bit uncompressed BMP files, the format Inno Setup's wizard images use. Skia decodes and draws but
    /// does not encode BMP.
    /// </summary>
    public static class BmpWriter
    {
        /// <summary>
        /// Writes a bitmap as a 24-bit BMP. Transparent pixels are blended over white.
        /// </summary>
        /// <param name="bitmap">Bitmap to write.</param>
        /// <param name="path">Destination file.</param>
        /// <exception cref="ArgumentNullException">The bitmap is null.</exception>
        public static void Write(SKBitmap bitmap, string path)
        {
            ArgumentNullException.ThrowIfNull(bitmap);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int rowBytes = (width * 3 + 3) & ~3;
            int imageBytes = rowBytes * height;
            const int headerBytes = 14 + 40;

            using (FileStream stream = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write(headerBytes + imageBytes);
                writer.Write(0);
                writer.Write(headerBytes);

                writer.Write(40);
                writer.Write(width);
                writer.Write(height);
                writer.Write((short)1);
                writer.Write((short)24);
                writer.Write(0);
                writer.Write(imageBytes);
                writer.Write(2835);
                writer.Write(2835);
                writer.Write(0);
                writer.Write(0);

                byte[] row = new byte[rowBytes];
                for (int y = height - 1; y >= 0; y--)
                {
                    Array.Clear(row);
                    for (int x = 0; x < width; x++)
                    {
                        SKColor color = bitmap.GetPixel(x, y);
                        double alpha = color.Alpha / 255.0;
                        row[x * 3] = Blend(color.Blue, alpha);
                        row[x * 3 + 1] = Blend(color.Green, alpha);
                        row[x * 3 + 2] = Blend(color.Red, alpha);
                    }

                    writer.Write(row);
                }
            }
        }

        private static byte Blend(byte channel, double alpha)
        {
            return (byte)Math.Round(channel * alpha + 255 * (1 - alpha));
        }
    }
}
