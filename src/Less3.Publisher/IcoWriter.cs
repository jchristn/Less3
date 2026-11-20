namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Writes Windows .ico files from the logo. Each size is stored as an embedded PNG, which Windows Vista and later
    /// read, so the installer and uninstaller icons come from the same artwork as every other format.
    /// </summary>
    public static class IcoWriter
    {
        /// <summary>
        /// Sizes written into the icon, in pixels.
        /// </summary>
        public static IReadOnlyList<int> Sizes { get; } = new[] { 16, 24, 32, 48, 64, 128, 256 };

        /// <summary>
        /// Writes an .ico holding the logo at every size in <see cref="Sizes"/>.
        /// </summary>
        /// <param name="logo">Source PNG.</param>
        /// <param name="destination">Destination .ico.</param>
        /// <exception cref="ArgumentNullException">A path is null or empty.</exception>
        /// <exception cref="PublisherException">The logo cannot be read.</exception>
        public static void Write(string logo, string destination)
        {
            if (String.IsNullOrEmpty(logo)) throw new ArgumentNullException(nameof(logo));
            if (String.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination));

            List<byte[]> images = new List<byte[]>();
            string scratch = Path.Combine(Path.GetTempPath(), "less3-ico-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                foreach (int size in Sizes)
                {
                    ImageAssets.SquarePng(logo, size, scratch);
                    images.Add(File.ReadAllBytes(scratch));
                }
            }
            finally
            {
                if (File.Exists(scratch)) File.Delete(scratch);
            }

            using (FileStream stream = File.Create(destination))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((short)0);
                writer.Write((short)1);
                writer.Write((short)images.Count);

                int offset = 6 + 16 * images.Count;
                for (int i = 0; i < images.Count; i++)
                {
                    int size = Sizes[i];
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((short)1);
                    writer.Write((short)32);
                    writer.Write(images[i].Length);
                    writer.Write(offset);
                    offset += images[i].Length;
                }

                foreach (byte[] image in images) writer.Write(image);
            }
        }
    }
}
