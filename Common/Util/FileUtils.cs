namespace FrontierSharp.Common.Util {
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;

    using NLog;

    public static class FileUtils {
        private const int DEFAULT_SIZE = 8;

        private static int defaultCounter;

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static Bitmap LoadDefaultImage(out Coord size) {
            var color = ColorUtils.UniqueColor(defaultCounter++);
            var bcolor = new byte[] {
                (byte)(color.R * 255.0f),
                (byte)(color.G * 255.0f),
                (byte)(color.B * 255.0f),
                255 };
            var white = new byte[] { 255, 255, 255, 255 };

            var bitmap = new Bitmap(DEFAULT_SIZE, DEFAULT_SIZE, PixelFormat.Format32bppArgb);
            size = new Coord(DEFAULT_SIZE, DEFAULT_SIZE);
            BitmapData data = null;
            try {
                data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    for (var y = 0; y < bitmap.Height; y++) {
                    var row = data.Scan0 + y * data.Stride;
                    for (var x = 0; x < bitmap.Width; x++) {
                        if ((x + y) % 2 != 0) {
                            Marshal.Copy(white, 0, row + x * 4, 4);
                        } else {
                            Marshal.Copy(bcolor, 0, row + x * 4, 4);
                        }
                    }
                }
            } finally {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }

        public static Bitmap FileImageLoad(string filename, out Coord sizeOut) {
            try {
                var bitmap = new Bitmap(filename);
                sizeOut = new Coord(bitmap.Width, bitmap.Height);
                return bitmap;
            } catch (FileNotFoundException e) {
                Log.Error(e, "Image file {0} not found, loading default image.", filename);
                return LoadDefaultImage(out sizeOut);
            }
        }
    }
}