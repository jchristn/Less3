namespace Less3.Publisher
{
    using System;
    using System.IO;
    using SkiaSharp;

    /// <summary>
    /// Generates every installer format's artwork from the one square logo, so nobody hand-maintains icon sizes.
    /// </summary>
    public static class ImageAssets
    {
        private static readonly SKSamplingOptions _Sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

        /// <summary>
        /// Writes the logo resized to a square PNG.
        /// </summary>
        /// <param name="logo">Source PNG.</param>
        /// <param name="size">Edge length in pixels, 1 to 4096.</param>
        /// <param name="destination">Destination PNG.</param>
        /// <exception cref="ArgumentOutOfRangeException">The size is out of range.</exception>
        /// <exception cref="PublisherException">The logo cannot be read.</exception>
        public static void SquarePng(string logo, int size, string destination)
        {
            if (size < 1 || size > 4096) throw new ArgumentOutOfRangeException(nameof(size), "Size must be 1 to 4096 pixels.");
            using (SKBitmap source = Load(logo))
            using (SKBitmap canvasBitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul)))
            {
                using (SKCanvas canvas = new SKCanvas(canvasBitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    Draw(canvas, source, new SKRect(0, 0, size, size));
                }

                SavePng(canvasBitmap, destination);
            }
        }

        /// <summary>
        /// Writes a 24-bit BMP of the given size: a white background with the logo centered horizontally, filling
        /// <paramref name="logoFraction"/> of the width. Used for Inno Setup's wizard images.
        /// </summary>
        /// <param name="logo">Source PNG.</param>
        /// <param name="width">Width in pixels.</param>
        /// <param name="height">Height in pixels.</param>
        /// <param name="logoFraction">Share of the width the logo fills, 0.1 to 1.</param>
        /// <param name="destination">Destination BMP.</param>
        /// <exception cref="ArgumentOutOfRangeException">A size or the fraction is out of range.</exception>
        /// <exception cref="PublisherException">The logo cannot be read.</exception>
        public static void Banner(string logo, int width, int height, double logoFraction, string destination)
        {
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
            if (logoFraction < 0.1 || logoFraction > 1) throw new ArgumentOutOfRangeException(nameof(logoFraction), "The logo fraction must be 0.1 to 1.");
            using (SKBitmap source = Load(logo))
            using (SKBitmap canvasBitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)))
            {
                using (SKCanvas canvas = new SKCanvas(canvasBitmap))
                {
                    canvas.Clear(SKColors.White);
                    float edge = (float)Math.Min(width * logoFraction, height * logoFraction);
                    float left = (width - edge) / 2;
                    float top = height > width * 1.5 ? height * 0.18f : (height - edge) / 2;
                    Draw(canvas, source, new SKRect(left, top, left + edge, top + edge));
                }

                BmpWriter.Write(canvasBitmap, destination);
            }
        }

        private static SKBitmap Load(string path)
        {
            if (!File.Exists(path)) throw new PublisherException("Logo not found at " + path + ". Set project.logo in publisher.json.");
            SKBitmap? bitmap = SKBitmap.Decode(path);
            if (bitmap == null) throw new PublisherException("Could not decode the logo at " + path + "; it must be a PNG.");
            return bitmap;
        }

        private static void Draw(SKCanvas canvas, SKBitmap source, SKRect destination)
        {
            using (SKImage image = SKImage.FromBitmap(source))
            {
                canvas.DrawImage(image, destination, _Sampling);
            }
        }

        private static void SavePng(SKBitmap bitmap, string destination)
        {
            using (SKImage image = SKImage.FromBitmap(bitmap))
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream stream = File.Create(destination))
            {
                data.SaveTo(stream);
            }
        }
    }
}
