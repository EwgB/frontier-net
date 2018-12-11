﻿namespace FrontierSharp.Entropy {
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;

    using Common;
    using Common.Util;

    using NLog;

    internal class EntropyImpl : IEntropy {
        private const int BLUR_RADIUS = 3;
        private const string ENTROPY_FILE = "entropy.raw";
        private const string TEXTURES_NOISE256 = "textures/noise256.bmp";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private bool loaded;
        private Coord size;
        private float[] emap;

        public float GetEntropy(float x, float y) {
            var cellX = (int) x;
            var cellY = (int) y;

            var dx = (x - cellX);
            var dy = (y - cellY);

            var y0 = GetEntropy(cellX, cellY);
            var y1 = GetEntropy(cellX + 1, cellY);
            var y2 = GetEntropy(cellX, cellY + 1);
            var y3 = GetEntropy(cellX + 1, cellY + 1);

            float a, b, c;

            if (dx < dy) {
                c = y2 - y0;
                b = y3 - y2;
                a = y0;
            } else {
                c = y3 - y1;
                b = y1 - y0;
                a = y0;
            }

            return (a + b * dx + c * dy);
        }

        public float GetEntropy(int x, int y) {
            if (!loaded)
                LoadEntropy();
            if (emap == null || x < 0 || y < 0)
                return 0;
            return emap[(x % size.X) + (y % size.Y) * size.X];
        }

        private void LoadEntropy() {
            try {
                using (var reader = new BinaryReader(File.OpenRead(ENTROPY_FILE))) {
                    var sizeX = reader.ReadInt32();
                    var sizeY = reader.ReadInt32();
                    size = new Coord(sizeX, sizeY);

                    // Convert to float array
                    var bytes = reader.ReadBytes(sizeX * sizeY * sizeof(float));
                    emap = new float[bytes.Length / sizeof(float)];
                    Buffer.BlockCopy(emap, 0, bytes, 0, bytes.Length);

                    loaded = true;
                }
            } catch (FileNotFoundException) {
                CreateEntropy(TEXTURES_NOISE256);
            }
        }

        private void CreateEntropy(string filename) {
            if (string.IsNullOrEmpty(filename))
                return;

            Bitmap bitmap = null;
            try {
                bitmap = FileUtils.FileImageLoad(filename, out size);
            } catch (FileNotFoundException) {
                Log.Debug("[CreateEntropy] file {0} not found. This is not an error.", filename);
            }
            
            BitmapData bitmapData = null;
            try {
                bitmapData = bitmap?.LockBits(
                    rect: new Rectangle(0, 0, size.X, size.Y),
                    flags: ImageLockMode.ReadOnly,
                    format: bitmap.PixelFormat);
                if (bitmapData == null)
                    return;
                
                var ptr = bitmapData.Scan0;
                var bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                var rgbaValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbaValues, 0, bytes);

                var elements = size.X * size.Y;
                emap = new float[elements];
                for (var y = 0; y < size.Y; y++) {
                    for (var x = 0; x < size.X; x++) {
                        var offsetX = x / (float) size.X;
                        var offsetY = y / (float) size.Y;
                        var scanX = (int) (offsetX * size.X);
                        var scanY = (int) (offsetY * size.Y);
                        var red = rgbaValues[(scanX + scanY * size.X) * 4];
                        emap[x + y * size.X] = (float) red / 255;
                    }
                }
            } finally {
                bitmap?.UnlockBits(bitmapData);
            }


            ErodeEntropy();

            try {
                using (var writer = new BinaryWriter(File.Open(ENTROPY_FILE, FileMode.Create))) {
                    writer.Write(size.X);
                    writer.Write(size.Y);

                    // Convert to byte array
                    var bytes = new byte[emap.Length * sizeof(float)];
                    Buffer.BlockCopy(emap, 0, bytes, 0, bytes.Length);
                    writer.Write(bytes);
                }
            } catch (Exception e) {
                Log.Debug("[CreateEntropy] Error creating file {0}: {1}", ENTROPY_FILE, e.Message);
            }

            loaded = true;
        }

        private int EntropyIndex(Coord n) => EntropyIndex(n.X, n.Y);

        private int EntropyIndex(int x, int y) =>
            Math.Abs(x) % size.X + Math.Abs(y) % size.Y * size.X;

        private void ErodeEntropy() {
            var buffer = new float[size.X * size.Y];
            Buffer.BlockCopy(
                src: emap, srcOffset: 0,
                dst: buffer, dstOffset: 0,
                count: sizeof(float) * size.X * size.Y);

            float low;
            float high;
            var index = 0;
            //Pass over the entire map, dropping a "raindrop" on each point. Trace
            //a path downhill until the drop hits bottom. Subtract elevation
            //along the way.  Makes natural hells from handmade ones. Super effective.
            for (var pass = 0; pass < 3; pass++) {
                for (var y = 0; y < size.Y; y++) {
                    for (var x = 0; x < size.X; x++) {
                        low = high = buffer[x + y * size.X];
                        var current = new Coord(x, y);
                        Coord highIndex;
                        var lowIndex = highIndex = current;
                        while (true) {
                            //look for neighbors lower than this point
                            for (var nX = current.X - 1; nX <= current.X + 1; nX++) {
                                for (var nY = current.Y - 1; nY <= current.Y + 1; nY++) {
                                    index = EntropyIndex(nX, nY);
                                    if (emap[index] >= high) {
                                        high = emap[index];
                                        highIndex = new Coord(nX, nY);
                                    }

                                    if (emap[index] <= low) {
                                        low = emap[index];
                                        lowIndex = new Coord(nX, nY);
                                    }
                                }
                            }

                            //Search done.  

                            //Sanity checks
                            lowIndex = new Coord(
                                (lowIndex.X + (lowIndex.X < 0 ? size.X : 0)) % size.X,
                                (lowIndex.Y + (lowIndex.Y < 0 ? size.Y : 0)) % size.Y);

                            //If we didn't move, then we're at the lowest point
                            if (lowIndex == current)
                                break;
                            index = EntropyIndex(current);

                            //If we're at the highest point around, we're on a spike.
                            //File that sucker down.
                            if (highIndex == current)
                                buffer[index] *= 0.95f;

                            //Erode this point a tiny bit, and move down.
                            buffer[index] *= 0.97f;
                            current = lowIndex;
                        }
                    }
                }

                Buffer.BlockCopy(
                    src: buffer, srcOffset: 0,
                    dst: emap, dstOffset: 0,
                    count: sizeof(float) * size.X * size.Y);
            }


            //Blur the elevations a bit to round off little spikes and divots.
            for (var y = 0; y < size.Y; y++) {
                for (var x = 0; x < size.X; x++) {
                    var val = 0.0f;
                    var count = 0;
                    for (var nX = -BLUR_RADIUS; nX <= BLUR_RADIUS; nX++) {
                        for (var nY = -BLUR_RADIUS; nY <= BLUR_RADIUS; nY++) {
                            var currentX = ((x + nX) + size.X) % size.X;
                            var currentY = ((y + nY) + size.Y) % size.Y;
                            index = EntropyIndex(currentX, currentY);
                            val += buffer[index];
                            count++;
                        }
                    }

                    val /= count;
                    emap[index] = (emap[index] + val) / 2.0f;
                    emap[index] = val;
                }
            }

            //re-normalize the map
            high = 0;
            low = 999999;
            for (var y = 0; y < size.Y; y++) {
                for (var x = 0; x < size.X; x++) {
                    index = EntropyIndex(x, y);
                    high = Math.Max(emap[index], high);
                    low = Math.Min(emap[index], low);
                }
            }

            high = high - low;
            for (var y = 0; y < size.Y; y++) {
                for (var x = 0; x < size.X; x++) {
                    index = EntropyIndex(x, y);
                    emap[index] -= low;
                    emap[index] /= high;
                }
            }
        }
    }
}

/*
#define INDEX(x,y)        ((x % size.X) + (y % size.Y) * size.X)

 */